using UnityEngine;
using Cinemachine;
using System.Collections;

public class LevelAreaTrigger : MonoBehaviour
{
    [Header("Camera Control")]
    public CinemachineVirtualCamera levelCamera;
    public float slowBlendTime = 2.5f;
    public float fastBlendTime = 0.5f;

    [Header("Butterfly")]
    public ButterflyController butterfly;
    public Transform[] guidePath;

    [Header("Level Elements")]
    public SwitchController endSwitch;
    
    private bool hasEntered = false;
    private bool isSequenceRunning = false;
    private CinemachineBrain mainCameraBrain;
    private PlayerController player;

    private void Start()
    {
        if (Camera.main != null) mainCameraBrain = Camera.main.GetComponent<CinemachineBrain>();
        
        if (butterfly != null)
        {
            butterfly.OnButterflyClicked += OnButterflyInteracted;
        }
    }

    private void OnDestroy()
    {
        if (butterfly != null)
        {
            butterfly.OnButterflyClicked -= OnButterflyInteracted;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !hasEntered)
        {
            hasEntered = true;
            
            if (player == null) player = collision.GetComponent<PlayerController>();

            if (levelCamera != null && mainCameraBrain != null)
            {
                mainCameraBrain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.EaseInOut;
                mainCameraBrain.m_DefaultBlend.m_Time = slowBlendTime;
                levelCamera.Priority = 20;
            }

            if (butterfly != null && !isSequenceRunning)
            {
                butterfly.StartPulse();
            }
        }
    }

    private void OnButterflyInteracted()
    {
        if (!hasEntered || isSequenceRunning) return;

        StartCoroutine(RunPuzzleSequence());
    }

    private IEnumerator RunPuzzleSequence()
    {
        isSequenceRunning = true;

        if (player != null) player.isInputLocked = true;

        butterfly.StopPulse();
        bool isButterflyArrived = false;
        butterfly.StartPath(guidePath, () => { isButterflyArrived = true; });

        yield return new WaitUntil(() => isButterflyArrived);

        bool bigPulseDone = false;
        butterfly.PlayBigPulse(() => { bigPulseDone = true; });

        yield return new WaitUntil(() => bigPulseDone);

        bool switchPulled = false;
        if (endSwitch != null)
        {
            endSwitch.ActivateSwitch(() => { switchPulled = true; });
            yield return new WaitUntil(() => switchPulled);
        }

        bool isPreviewDone = false;
        LevelSequenceManager.Instance.PlayPreviewSequence(() => { isPreviewDone = true; });

        yield return new WaitUntil(() => isPreviewDone);

        butterfly.ResumeFollow();
        if (player != null) player.isInputLocked = false;
        
        isSequenceRunning = false;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && hasEntered)
        {
            hasEntered = false;

            if (butterfly != null)
            {
                butterfly.ResumeFollow();
                butterfly.StopPulse();
            }

            if (endSwitch != null) endSwitch.ResetSwitch();

            if (levelCamera != null && mainCameraBrain != null)
            {
                mainCameraBrain.m_DefaultBlend.m_Time = fastBlendTime;
                levelCamera.Priority = 0;
            }
        }
    }

    public void OnLevelCleared()
    {
        if (butterfly != null)
        {
            butterfly.ResumeFollow();
        }
    }
}
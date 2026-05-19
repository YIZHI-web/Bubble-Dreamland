using UnityEngine;
using UnityEngine.Playables;

public class WinTimelineTrigger : MonoBehaviour
{
    [Header("Win Timeline Settings")]
    [SerializeField] private PlayableDirector winPlayableDirector;
    [SerializeField] private string nextLevelSceneName;
    [SerializeField] private bool loadNextLevelOnTimelineEnd = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            TriggerWinTimeline();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            TriggerWinTimeline();
        }
    }

    public void TriggerWinTimeline()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        if (winPlayableDirector != null)
        {
            winPlayableDirector.time = 0;
            winPlayableDirector.Play();

            if (loadNextLevelOnTimelineEnd && !string.IsNullOrEmpty(nextLevelSceneName))
            {
                winPlayableDirector.stopped += OnWinTimelineEnd;
            }
        }
        else
        {
            Debug.LogWarning("WinTimelineTrigger: winPlayableDirector is null!");
            OnTimelineCompleted();
        }
    }

    private void OnWinTimelineEnd(PlayableDirector director)
    {
        if (director != null)
        {
            director.stopped -= OnWinTimelineEnd;
        }
        OnTimelineCompleted();
    }

    private void OnTimelineCompleted()
    {
        if (!string.IsNullOrEmpty(nextLevelSceneName))
        {
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.TransitionToScene(nextLevelSceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextLevelSceneName);
            }
        }
    }

    public void SetPlayableDirector(PlayableDirector director)
    {
        winPlayableDirector = director;
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
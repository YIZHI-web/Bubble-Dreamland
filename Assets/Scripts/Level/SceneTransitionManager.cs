using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Transition Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Color fadeColor = Color.black;

    [Header("Main Menu Settings")]
    [SerializeField] private string mainMenuSceneName = "主界面";
    [SerializeField] private string[] managersToDestroyOnMainMenu = new string[] { "Dialogue Manager" };

    private Canvas transitionCanvas;
    private Image fadeImage;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        
        GameObject canvasObj = new GameObject("TransitionCanvas");
        DontDestroyOnLoad(canvasObj);
        SetupTransitionCanvas(canvasObj);
    }

    private void SetupTransitionCanvas(GameObject canvasObj)
    {
        transitionCanvas = canvasObj.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 9999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.referencePixelsPerUnit = 100;

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform);
        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = fadeColor;
        fadeImage.raycastTarget = false;

        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        fadeImage.enabled = false;
    }

    public void TransitionToScene(string sceneName)
    {
        if (isTransitioning || string.IsNullOrEmpty(sceneName))
            return;

        isTransitioning = true;
        fadeImage.enabled = true;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(fadeImage.DOFade(1f, fadeDuration));
        sequence.AppendCallback(() =>
        {
            if (sceneName == mainMenuSceneName)
            {
                DestroyManagers();
            }
            SceneManager.LoadScene(sceneName);
        });
        sequence.AppendInterval(0.1f);
        sequence.Append(fadeImage.DOFade(0f, fadeDuration));
        sequence.OnComplete(() =>
        {
            fadeImage.enabled = false;
            isTransitioning = false;
        });
    }

    public void TransitionToSceneAsync(string sceneName)
    {
        if (isTransitioning || string.IsNullOrEmpty(sceneName))
            return;

        isTransitioning = true;
        fadeImage.enabled = true;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(fadeImage.DOFade(1f, fadeDuration));
        sequence.AppendCallback(() =>
        {
            if (sceneName == mainMenuSceneName)
            {
                DestroyManagers();
            }
            SceneManager.LoadSceneAsync(sceneName);
        });
        sequence.AppendInterval(0.1f);
        sequence.Append(fadeImage.DOFade(0f, fadeDuration));
        sequence.OnComplete(() =>
        {
            fadeImage.enabled = false;
            isTransitioning = false;
        });
    }

    private void DestroyManagers()
    {
        foreach (string managerName in managersToDestroyOnMainMenu)
        {
            GameObject manager = GameObject.Find(managerName);
            if (manager != null)
            {
                Destroy(manager);
            }
        }
    }
}
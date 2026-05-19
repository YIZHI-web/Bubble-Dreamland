using UnityEngine;

public class MainMenuButton : MonoBehaviour
{
    public void ReturnToMainMenu()
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene("主界面");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("主界面");
        }
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Put this on an empty "MenuManager" object in the MainMenu scene and hook the buttons' OnClick events to the methods below.
public class MainMenu : MonoBehaviour
{
    [Tooltip("Exactly the name of your game scene (and it must be in Build Profiles / Build Settings)")]
    public string gameSceneName = "Map_Hosp1";

    [Header("Panels")]
    public GameObject mainPanel;          // Play / Options / Quit
    public GameObject difficultyPanel;    // Easy / Normal / Difficult / Back
    public GameObject optionsPanel;       // volume slider / Back

    [Header("Fade to black (optional)")]
    public CanvasGroup fadeOverlay;       // full-screen black Image with a CanvasGroup, alpha 0
    public float fadeTime = 1f;

    bool loading;

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;      // the game locks the cursor; the menu needs it free
        Cursor.visible = true;

        ShowOnly(mainPanel);
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
        }
    }

    void ShowOnly(GameObject panel)
    {
        if (mainPanel != null) mainPanel.SetActive(mainPanel == panel);
        if (difficultyPanel != null) difficultyPanel.SetActive(difficultyPanel == panel);
        if (optionsPanel != null) optionsPanel.SetActive(optionsPanel == panel);
    }

    // ---- panel buttons ----
    public void PlayGame() { OpenDifficulty(); }           // the Play button
    public void OpenDifficulty() { ShowOnly(difficultyPanel); }
    public void OpenOptions() { ShowOnly(optionsPanel); }
    public void BackToMain() { ShowOnly(mainPanel); }
    public void CloseOptions() { ShowOnly(mainPanel); }

    // ---- difficulty buttons ----
    public void PlayEasy() { StartGame(Difficulty.Easy); }
    public void PlayNormal() { StartGame(Difficulty.Normal); }
    public void PlayDifficult() { StartGame(Difficulty.Difficult); }

    public void StartGame(Difficulty difficulty)
    {
        if (loading) return;
        GameSettings.Selected = difficulty;
        StartCoroutine(LoadGame());
    }

    IEnumerator LoadGame()
    {
        loading = true;

        if (fadeOverlay != null)
        {
            fadeOverlay.blocksRaycasts = true;
            for (float t = 0f; t < fadeTime; t += Time.deltaTime)
            {
                fadeOverlay.alpha = t / fadeTime;
                yield return null;
            }
            fadeOverlay.alpha = 1f;
        }

        SceneManager.LoadSceneAsync(gameSceneName);
    }

    // hook to a Slider (min 0, max 1) with "Dynamic float"
    public void SetVolume(float value)
    {
        AudioListener.volume = value;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

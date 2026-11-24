using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Put this on an empty "GameMenus" object in the GAME scene (not in the main menu scene).
// - Press Esc (or P) to pause: Resume / Restart / Main Menu / Quit, plus a volume slider.
// - When the player dies, a "YOU DIED" screen appears with Try Again / Main Menu.
// It draws its own screens, so there is nothing else to set up.
public class GameMenus : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Exactly the name of your main menu scene (it must be in Build Profiles / Build Settings)")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Keys")]
    public KeyCode pauseKey = KeyCode.Escape;
    public KeyCode pauseKeyAlt = KeyCode.P;      // Esc also releases the mouse in the Editor, so P works too

    [Header("Death screen")]
    public float deathScreenDelay = 1.5f;        // seconds after dying before the screen starts to appear
    public float deathFadeTime = 1.0f;

    [Header("Look")]
    public Color accentColor = new Color(0.75f, 0.08f, 0.06f, 1f);

    [Header("Victory screen (when every monster is dead)")]
    public bool showVictoryScreen = true;
    public float victoryScreenDelay = 3f;        // lets the monster's death animation play first
    public float victoryFadeTime = 1.5f;
    public Color victoryColor = new Color(0.85f, 0.92f, 1f, 1f);

    [Header("Canvas UI (optional) - filled in for you by  Tools > Horror Game > Build Pause Menu UI")]
    [Tooltip("If any of these are assigned, the Canvas screens are used instead of the built-in simple ones")]
    public GameObject pausePanel;
    public CanvasGroup deathPanel;       // needs a CanvasGroup so it can fade in
    public CanvasGroup victoryPanel;     // needs a CanvasGroup so it can fade in
    public CanvasGroup fadeOverlay;      // full-screen black, used when changing scene
    public Slider volumeSlider;          // optional
    public TMP_Text victorySubtitle;     // optional: text under "YOU SURVIVED"

    PlayerHealth health;
    FPSPlayer fps;
    bool paused, loading;
    bool prevLook, prevMove;
    float deathTimer, screenFade;
    MonsterHealth[] monsters;
    bool victory, victoryActive, victoryDismissed;
    float victoryTimer;

    GUIStyle titleStyle, buttonStyle, labelStyle, subtitleStyle;
    Texture2D texNormal, texHover, texActive;

    void Start()
    {
        health = FindFirstObjectByType<PlayerHealth>();
        fps = FindFirstObjectByType<FPSPlayer>();
        monsters = FindObjectsByType<MonsterHealth>(FindObjectsSortMode.None);   // only monsters that are switched on

        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
        RefreshCanvas();
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    void Update()
    {
        if (loading) return;

        if (health != null && health.isDead)
        {
            if (paused) SetPaused(false);
            if (deathTimer == 0f) ShowCursor(true);
            deathTimer += Time.unscaledDeltaTime;
            return;
        }

        UpdateVictory();

        if (!victoryActive && (Input.GetKeyDown(pauseKey) || Input.GetKeyDown(pauseKeyAlt)))
            SetPaused(!paused);
    }

    // ------------------------------------------------------------------ victory

    void UpdateVictory()
    {
        if (!showVictoryScreen || victoryDismissed || monsters == null || monsters.Length == 0) return;

        if (!victory && AllMonstersDead())
        {
            victory = true;
            victoryTimer = 0f;
        }

        if (victory)
        {
            victoryTimer += Time.unscaledDeltaTime;
            if (!victoryActive && victoryTimer >= victoryScreenDelay) BeginVictoryScreen();
        }
    }

    bool AllMonstersDead()
    {
        foreach (MonsterHealth m in monsters)
        {
            if (m != null && !m.IsDead) return false;
        }
        return true;
    }

    void BeginVictoryScreen()
    {
        victoryActive = true;
        Time.timeScale = 0f;                 // freeze the game so a click on a button can't fire the shotgun

        if (fps != null)
        {
            prevLook = fps.lookEnabled;
            prevMove = fps.movementEnabled;
            fps.lookEnabled = false;
            fps.movementEnabled = false;
        }
        ShowCursor(true);
    }

    public void KeepExploring()
    {
        victoryActive = false;
        victoryDismissed = true;
        Time.timeScale = 1f;

        if (fps != null)
        {
            fps.lookEnabled = prevLook;
            fps.movementEnabled = prevMove;
        }
        ShowCursor(false);
    }

    // ------------------------------------------------------------------ pause

    void SetPaused(bool value)
    {
        paused = value;
        Time.timeScale = value ? 0f : 1f;
        AudioListener.pause = value;

        if (fps != null)
        {
            if (value)
            {
                prevLook = fps.lookEnabled;
                prevMove = fps.movementEnabled;
                fps.lookEnabled = false;       // otherwise the camera keeps turning with the mouse
                fps.movementEnabled = false;
            }
            else
            {
                fps.lookEnabled = prevLook;
                fps.movementEnabled = prevMove;
            }
        }
        ShowCursor(value);
    }

    static void ShowCursor(bool show)
    {
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = show;
    }

    // ------------------------------------------------------------------ changing scene

    public void RestartLevel() { if (!loading) StartCoroutine(Load(SceneManager.GetActiveScene().name)); }
    public void GoToMainMenu() { if (!loading) StartCoroutine(Load(mainMenuSceneName)); }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator Load(string sceneName)
    {
        loading = true;
        const float fadeTime = 0.6f;
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)   // unscaled: works while the game is paused
        {
            screenFade = t / fadeTime;
            yield return null;
        }
        screenFade = 1f;

        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(sceneName);
    }

    // ------------------------------------------------------------------ drawing

    void OnGUI()
    {
        if (UseCanvas) return;           // the Canvas screens are in use

        GUI.depth = -1000;                 // on top of the HUD
        EnsureStyles();

        float s = Mathf.Max(0.6f, Screen.height / 1080f);
        titleStyle.fontSize = Mathf.RoundToInt(72 * s);
        buttonStyle.fontSize = Mathf.RoundToInt(28 * s);
        labelStyle.fontSize = Mathf.RoundToInt(22 * s);
        subtitleStyle.fontSize = Mathf.RoundToInt(28 * s);

        bool dead = health != null && health.isDead;
        if (dead) DrawDeathScreen(s);
        else if (victoryActive) DrawVictoryScreen(s);
        else if (paused) DrawPauseScreen(s);

        if (screenFade > 0f)
        {
            GUI.color = new Color(0f, 0f, 0f, screenFade);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }

    void DrawDeathScreen(float s)
    {
        float a = Mathf.Clamp01((deathTimer - deathScreenDelay) / Mathf.Max(0.01f, deathFadeTime));
        if (a <= 0f) return;

        Overlay(new Color(0.10f, 0f, 0f, 0.88f * a));
        GUI.color = new Color(1f, 1f, 1f, a);

        float w = 400f * s, h = 64f * s, gap = 18f * s;
        float x = (Screen.width - w) / 2f;
        float y = Screen.height * 0.5f - 40f * s;

        GUI.contentColor = accentColor;
        GUI.Label(new Rect(0f, y - 190f * s, Screen.width, 100f * s), "YOU DIED", titleStyle);
        GUI.contentColor = Color.white;

        if (GUI.Button(new Rect(x, y, w, h), "Try Again", buttonStyle)) RestartLevel();
        y += h + gap;
        if (GUI.Button(new Rect(x, y, w, h), "Main Menu", buttonStyle)) GoToMainMenu();

        GUI.color = Color.white;
    }

    void DrawVictoryScreen(float s)
    {
        float a = Mathf.Clamp01((victoryTimer - victoryScreenDelay) / Mathf.Max(0.01f, victoryFadeTime));

        Overlay(new Color(0f, 0.02f, 0.04f, 0.85f * a));
        GUI.color = new Color(1f, 1f, 1f, a);

        float w = 400f * s, h = 60f * s, gap = 16f * s;
        float x = (Screen.width - w) / 2f;
        float y = Screen.height * 0.5f - 30f * s;

        GUI.contentColor = victoryColor;
        GUI.Label(new Rect(0f, y - 220f * s, Screen.width, 100f * s), "YOU SURVIVED", titleStyle);
        GUI.contentColor = Color.white;

        string sub = (monsters != null && monsters.Length > 1) ? "All the monsters have been destroyed." : "The monster has been destroyed.";
        GUI.Label(new Rect(0f, y - 120f * s, Screen.width, 50f * s), sub, subtitleStyle);

        if (GUI.Button(new Rect(x, y, w, h), "Keep Exploring", buttonStyle)) KeepExploring();
        y += h + gap;
        if (GUI.Button(new Rect(x, y, w, h), "Play Again", buttonStyle)) RestartLevel();
        y += h + gap;
        if (GUI.Button(new Rect(x, y, w, h), "Main Menu", buttonStyle)) GoToMainMenu();

        GUI.color = Color.white;
    }


    void DrawPauseScreen(float s)
    {
        Overlay(new Color(0f, 0f, 0f, 0.72f));

        float w = 400f * s, h = 60f * s, gap = 16f * s;
        float x = (Screen.width - w) / 2f;
        float y = Screen.height * 0.5f - 170f * s;

        GUI.Label(new Rect(0f, y - 150f * s, Screen.width, 100f * s), "PAUSED", titleStyle);

        if (GUI.Button(new Rect(x, y, w, h), "Resume", buttonStyle)) SetPaused(false);
        y += h + gap;
        if (GUI.Button(new Rect(x, y, w, h), "Restart", buttonStyle)) RestartLevel();
        y += h + gap;
        if (GUI.Button(new Rect(x, y, w, h), "Main Menu", buttonStyle)) GoToMainMenu();
        y += h + gap;
        if (GUI.Button(new Rect(x, y, w, h), "Quit Game", buttonStyle)) QuitGame();
        y += h + gap * 1.5f;

        GUI.Label(new Rect(x, y, w, 30f * s), "Volume", labelStyle);
        AudioListener.volume = GUI.HorizontalSlider(new Rect(x, y + 36f * s, w, 20f * s), AudioListener.volume, 0f, 1f);
    }

    static void Overlay(Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    static Texture2D MakeTex(Color c)
    {
        Texture2D t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    void EnsureStyles()
    {
        if (buttonStyle != null) return;

        texNormal = MakeTex(new Color(0.07f, 0.07f, 0.08f, 0.95f));
        texHover = MakeTex(accentColor * 0.55f + new Color(0f, 0f, 0f, 0.45f));
        texActive = MakeTex(accentColor);

        titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        titleStyle.normal.textColor = Color.white;

        labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
        labelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.85f);

        subtitleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        subtitleStyle.normal.textColor = new Color(1f, 1f, 1f, 0.85f);

        buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter };
        buttonStyle.normal.background = texNormal;
        buttonStyle.focused.background = texNormal;
        buttonStyle.hover.background = texHover;
        buttonStyle.active.background = texActive;
        buttonStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
        buttonStyle.focused.textColor = new Color(0.9f, 0.9f, 0.9f);
        buttonStyle.hover.textColor = Color.white;
        buttonStyle.active.textColor = Color.white;
    }

    // ------------------------------------------------------------------ Canvas UI

    public void Resume() { SetPaused(false); }

    public void SetVolume(float value) { AudioListener.volume = value; }

    bool UseCanvas
    {
        get { return pausePanel != null || deathPanel != null || victoryPanel != null; }
    }

    void LateUpdate()
    {
        RefreshCanvas();
    }

    void RefreshCanvas()
    {
        if (!UseCanvas) return;

        bool dead = health != null && health.isDead;

        if (pausePanel != null)
            pausePanel.SetActive(paused && !dead);

        if (deathPanel != null)
        {
            float a = dead ? Mathf.Clamp01((deathTimer - deathScreenDelay) / Mathf.Max(0.01f, deathFadeTime)) : 0f;
            SetGroup(deathPanel, a);
        }

        if (victoryPanel != null)
        {
            float a = (victoryActive && !dead) ? Mathf.Clamp01((victoryTimer - victoryScreenDelay) / Mathf.Max(0.01f, victoryFadeTime)) : 0f;
            SetGroup(victoryPanel, a);
            if (victorySubtitle != null)
                victorySubtitle.text = (monsters != null && monsters.Length > 1) ? "All the monsters have been destroyed." : "The monster has been destroyed.";
        }

        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = screenFade;
            fadeOverlay.blocksRaycasts = screenFade > 0f;
        }
    }

    static void SetGroup(CanvasGroup group, float alpha)
    {
        bool show = alpha > 0f;
        if (group.gameObject.activeSelf != show) group.gameObject.SetActive(show);
        group.alpha = alpha;
        group.interactable = alpha >= 0.5f;
        group.blocksRaycasts = show;
    }
}
// Put this file in a folder named "Editor" (Assets/Editor/HorrorUIBuilder.cs).
// It adds two menu items:
//     Tools > Horror Game > Build Main Menu UI     (run it in the MainMenu scene)
//     Tools > Horror Game > Build Pause Menu UI    (run it in the game scene; also builds the death and victory screens)
// Each one creates a Canvas with every panel, button, text and slider, and wires everything up for you.
// After that, edit the layout like any other UI: select things and move / resize / restyle them in the Scene view.

using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class HorrorUIBuilder
{
    // ------------------------------------------------------------------ look (change these colours to restyle everything the builder makes)
    static readonly Color ButtonColor = new Color(0.07f, 0.07f, 0.08f, 0.95f);
    static readonly Color ButtonHover = new Color(0.45f, 0.06f, 0.05f, 1f);
    static readonly Color ButtonPressed = new Color(0.75f, 0.08f, 0.06f, 1f);
    static readonly Color TextLight = new Color(0.92f, 0.92f, 0.92f, 1f);
    static readonly Color Accent = new Color(0.75f, 0.08f, 0.06f, 1f);
    static readonly Color VictoryColor = new Color(0.85f, 0.92f, 1f, 1f);

    const float ButtonW = 440f;
    const float ButtonH = 70f;
    const float ButtonGap = 18f;

    // ================================================================== MAIN MENU
    [MenuItem("Tools/Horror Game/Build Main Menu UI")]
    public static void BuildMainMenu()
    {
        if (!CheckTMP()) return;
        if (!ReplaceExisting("MainMenuCanvas")) return;

        Canvas canvas = CreateCanvas("MainMenuCanvas");
        EnsureEventSystem();
        Transform root = canvas.transform;

        // drawn first = behind everything else: the fade used between camera shots
        GameObject cameraFade = CreatePanel(root, "CameraFade", Color.black, true);
        SetupFadeGroup(cameraFade);

        // light darkening so the text reads over the 3D background
        CreatePanel(root, "DarkOverlay", new Color(0f, 0f, 0f, 0.35f), false).GetComponent<Image>().raycastTarget = false;

        CreateText(root, "GameTitle", "YOUR GAME TITLE", 130f, FontStyles.Bold, Color.white,
                   new Vector2(0f, 330f), new Vector2(1500f, 200f));

        // ---- main panel
        RectTransform mainPanel = CreateContainer(root, "MainPanel");
        RectTransform mainCol = CreateButtonColumn(mainPanel, "Buttons", new Vector2(0f, -60f), ColumnHeight(3));
        Button play = CreateButton(mainCol, "PlayButton", "PLAY");
        Button options = CreateButton(mainCol, "OptionsButton", "OPTIONS");
        Button quit = CreateButton(mainCol, "QuitButton", "QUIT");

        // ---- difficulty panel
        RectTransform diffPanel = CreateContainer(root, "DifficultyPanel");
        CreateText(diffPanel, "Header", "SELECT DIFFICULTY", 80f, FontStyles.Bold, TextLight,
                   new Vector2(0f, 170f), new Vector2(1300f, 120f));
        RectTransform diffCol = CreateButtonColumn(diffPanel, "Buttons", new Vector2(0f, -70f), ColumnHeight(4));
        Button easy = CreateButton(diffCol, "EasyButton", "EASY");
        Button normal = CreateButton(diffCol, "NormalButton", "NORMAL");
        Button difficult = CreateButton(diffCol, "DifficultButton", "DIFFICULT");
        Button diffBack = CreateButton(diffCol, "BackButton", "BACK");

        // ---- options panel
        RectTransform optPanel = CreateContainer(root, "OptionsPanel");
        CreateText(optPanel, "Header", "OPTIONS", 80f, FontStyles.Bold, TextLight,
                   new Vector2(0f, 170f), new Vector2(1300f, 120f));
        Slider volume = CreateVolumeSlider(optPanel, new Vector2(0f, 30f));
        RectTransform optCol = CreateButtonColumn(optPanel, "Buttons", new Vector2(0f, -150f), ColumnHeight(1));
        Button optBack = CreateButton(optCol, "BackButton", "BACK");

        // drawn last = in front of everything: the fade used when the game starts
        GameObject startFade = CreatePanel(root, "FadeOverlay", Color.black, true);
        SetupFadeGroup(startFade);

        // while editing you see the main panel; the other two start switched off (the script shows the right one)
        diffPanel.gameObject.SetActive(false);
        optPanel.gameObject.SetActive(false);

        // ---- wire up the scripts
        MainMenu menu = Object.FindFirstObjectByType<MainMenu>();
        if (menu == null)
        {
            GameObject go = new GameObject("MenuManager");
            Undo.RegisterCreatedObjectUndo(go, "Build Main Menu UI");
            menu = go.AddComponent<MainMenu>();
        }
        Undo.RecordObject(menu, "Wire MainMenu");
        menu.mainPanel = mainPanel.gameObject;
        menu.difficultyPanel = diffPanel.gameObject;
        menu.optionsPanel = optPanel.gameObject;
        menu.fadeOverlay = startFade.GetComponent<CanvasGroup>();
        EditorUtility.SetDirty(menu);

        OnClick(play, menu.PlayGame);
        OnClick(options, menu.OpenOptions);
        OnClick(quit, menu.QuitGame);
        OnClick(easy, menu.PlayEasy);
        OnClick(normal, menu.PlayNormal);
        OnClick(difficult, menu.PlayDifficult);
        OnClick(diffBack, menu.BackToMain);
        OnClick(optBack, menu.CloseOptions);
        UnityEventTools.AddPersistentListener(volume.onValueChanged, new UnityAction<float>(menu.SetVolume));

        // the camera shots use the "CameraFade" image
        MenuCameraShots shots = Object.FindFirstObjectByType<MenuCameraShots>();
        if (shots != null)
        {
            Undo.RecordObject(shots, "Wire MenuCameraShots");
            shots.fade = cameraFade.GetComponent<CanvasGroup>();
            EditorUtility.SetDirty(shots);
        }

        Finish(canvas.gameObject,
               "Main menu UI built." +
               (shots != null ? "\n\nThe camera fade was connected to MenuCameraShots." : "\n\nNo MenuCameraShots was found in this scene; drag CameraFade into its Fade field later.") +
               "\n\nTo edit the Difficulty or Options panels, tick them on in the Hierarchy (the script switches them at runtime).");
    }

    // ================================================================== PAUSE MENU (+ death and victory screens)
    [MenuItem("Tools/Horror Game/Build Pause Menu UI")]
    public static void BuildPauseMenu()
    {
        if (!CheckTMP()) return;
        if (!ReplaceExisting("GameMenusCanvas")) return;

        Canvas canvas = CreateCanvas("GameMenusCanvas");
        EnsureEventSystem();
        Transform root = canvas.transform;

        // ---- pause
        GameObject pause = CreatePanel(root, "PausePanel", new Color(0f, 0f, 0f, 0.72f), false);
        CreateText(pause.transform, "Title", "PAUSED", 110f, FontStyles.Bold, Color.white,
                   new Vector2(0f, 300f), new Vector2(1200f, 160f));
        RectTransform pauseCol = CreateButtonColumn(pause.transform, "Buttons", new Vector2(0f, 60f), ColumnHeight(4));
        Button resume = CreateButton(pauseCol, "ResumeButton", "RESUME");
        Button restart = CreateButton(pauseCol, "RestartButton", "RESTART");
        Button pauseMenu = CreateButton(pauseCol, "MainMenuButton", "MAIN MENU");
        Button quit = CreateButton(pauseCol, "QuitButton", "QUIT GAME");
        Slider volume = CreateVolumeSlider(pause.transform, new Vector2(0f, -230f));

        // ---- death
        GameObject death = CreatePanel(root, "DeathPanel", new Color(0.10f, 0f, 0f, 0.88f), true);
        CreateText(death.transform, "Title", "YOU DIED", 130f, FontStyles.Bold, Accent,
                   new Vector2(0f, 230f), new Vector2(1400f, 200f));
        RectTransform deathCol = CreateButtonColumn(death.transform, "Buttons", new Vector2(0f, -30f), ColumnHeight(2));
        Button tryAgain = CreateButton(deathCol, "TryAgainButton", "TRY AGAIN");
        Button deathMenu = CreateButton(deathCol, "MainMenuButton", "MAIN MENU");

        // ---- victory
        GameObject victory = CreatePanel(root, "VictoryPanel", new Color(0f, 0.02f, 0.04f, 0.85f), true);
        CreateText(victory.transform, "Title", "YOU SURVIVED", 130f, FontStyles.Bold, VictoryColor,
                   new Vector2(0f, 250f), new Vector2(1400f, 200f));
        TextMeshProUGUI subtitle = CreateText(victory.transform, "Subtitle", "The monster has been destroyed.", 40f,
                   FontStyles.Normal, new Color(1f, 1f, 1f, 0.85f), new Vector2(0f, 150f), new Vector2(1400f, 70f));
        RectTransform victoryCol = CreateButtonColumn(victory.transform, "Buttons", new Vector2(0f, -40f), ColumnHeight(3));
        Button keep = CreateButton(victoryCol, "KeepExploringButton", "KEEP EXPLORING");
        Button again = CreateButton(victoryCol, "PlayAgainButton", "PLAY AGAIN");
        Button victoryMenu = CreateButton(victoryCol, "MainMenuButton", "MAIN MENU");

        // ---- fade to black when changing scene (always last = in front)
        GameObject fade = CreatePanel(root, "FadeOverlay", Color.black, true);
        SetupFadeGroup(fade);

        // while editing you see the pause panel; death and victory start switched off (the script shows them when needed)
        death.SetActive(false);
        victory.SetActive(false);

        // ---- wire up the script
        GameMenus menus = Object.FindFirstObjectByType<GameMenus>();
        if (menus == null)
        {
            GameObject go = new GameObject("GameMenus");
            Undo.RegisterCreatedObjectUndo(go, "Build Pause Menu UI");
            menus = go.AddComponent<GameMenus>();
        }
        Undo.RecordObject(menus, "Wire GameMenus");
        menus.pausePanel = pause;
        menus.deathPanel = death.GetComponent<CanvasGroup>();
        menus.victoryPanel = victory.GetComponent<CanvasGroup>();
        menus.fadeOverlay = fade.GetComponent<CanvasGroup>();
        menus.volumeSlider = volume;
        menus.victorySubtitle = subtitle;
        EditorUtility.SetDirty(menus);

        OnClick(resume, menus.Resume);
        OnClick(restart, menus.RestartLevel);
        OnClick(pauseMenu, menus.GoToMainMenu);
        OnClick(quit, menus.QuitGame);
        OnClick(tryAgain, menus.RestartLevel);
        OnClick(deathMenu, menus.GoToMainMenu);
        OnClick(keep, menus.KeepExploring);
        OnClick(again, menus.RestartLevel);
        OnClick(victoryMenu, menus.GoToMainMenu);

        Finish(canvas.gameObject,
               "Pause, death and victory screens built and connected to GameMenus." +
               "\n\nTo edit the death or victory screen, tick it on in the Hierarchy (the script shows and hides them at runtime)." +
               "\n\nThe pause panel is visible in the Game view while editing, and hidden when you press Play until you pause.");
    }

    // ================================================================== helpers
    static float ColumnHeight(int buttons)
    {
        return buttons * ButtonH + (buttons - 1) * ButtonGap;
    }

    static bool CheckTMP()
    {
        if (TMP_Settings.instance == null)
        {
            EditorUtility.DisplayDialog("TextMeshPro needs setting up",
                "Import the TMP Essential Resources first:\n\nWindow > TextMeshPro > Import TMP Essential Resources\n\nThen run this tool again.", "OK");
            return false;
        }
        return true;
    }

    static bool ReplaceExisting(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing == null) return true;

        bool replace = EditorUtility.DisplayDialog("Replace existing UI?",
            "\"" + objectName + "\" already exists in this scene.\n\nReplace it with a fresh one? Anything you changed on it will be lost.",
            "Replace", "Cancel");
        if (!replace) return false;

        Undo.DestroyObjectImmediate(existing);
        return true;
    }

    static Canvas CreateCanvas(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, "Build UI");

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        EditorApplication.ExecuteMenuItem("GameObject/UI/Event System");     // picks the right input module for your project
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static RectTransform CreateContainer(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        Stretch(rt);
        return rt;
    }

    static GameObject CreatePanel(Transform parent, string name, Color color, bool withCanvasGroup)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform);
        go.GetComponent<Image>().color = color;
        if (withCanvasGroup) go.AddComponent<CanvasGroup>();
        return go;
    }

    // a black full-screen image that fades: invisible, and it must never block the buttons
    static void SetupFadeGroup(GameObject panel)
    {
        panel.GetComponent<Image>().raycastTarget = false;
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, FontStyles style,
                                      Color color, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    // buttons placed inside are stacked top to bottom (edit spacing and alignment on its Vertical Layout Group)
    static RectTransform CreateButtonColumn(Transform parent, string name, Vector2 anchoredPos, float height)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(ButtonW + 20f, height);

        VerticalLayoutGroup layout = go.GetComponent<VerticalLayoutGroup>();
        layout.spacing = ButtonGap;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return rt;
    }

    static Button CreateButton(Transform parent, string name, string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        ((RectTransform)go.transform).sizeDelta = new Vector2(ButtonW, ButtonH);

        Image image = go.GetComponent<Image>();
        image.color = Color.white;                       // the colour comes from the Button's Color Tint below

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = ButtonHover;
        colors.pressedColor = ButtonPressed;
        colors.selectedColor = ButtonColor;
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TextMeshProUGUI text = CreateText(go.transform, "Label", label, 34f, FontStyles.Bold, TextLight, Vector2.zero, Vector2.zero);
        Stretch(text.rectTransform);
        return button;
    }

    static Slider CreateVolumeSlider(Transform parent, Vector2 anchoredPos)
    {
        CreateText(parent, "VolumeLabel", "VOLUME", 30f, FontStyles.Normal, TextLight,
                   anchoredPos + new Vector2(0f, 42f), new Vector2(ButtonW, 40f));

        GameObject go = DefaultControls.CreateSlider(UIResources());
        go.name = "VolumeSlider";
        go.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(ButtonW, 28f);

        Slider slider = go.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    static DefaultControls.Resources UIResources()
    {
        DefaultControls.Resources r = new DefaultControls.Resources();
        r.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        r.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        r.inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
        r.knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        r.checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
        r.dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
        r.mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
        return r;
    }

    static void OnClick(Button button, UnityAction action)
    {
        UnityEventTools.AddPersistentListener(button.onClick, action);
    }

    static void Finish(GameObject canvasObject, string message)
    {
        Selection.activeGameObject = canvasObject;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Done", message, "OK");
    }
}

#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CombatUIPrefabGenerator
{
    private const string PreBattlePath =
        "Assets/Resources/UI/PreBattleRollPanel.prefab";
    private const string EnemyWorldPath =
        "Assets/Resources/UI/EnemyWorldUI.prefab";
    private const string BattleActionPath =
        "Assets/Resources/UI/BattleActionPanel.prefab";
    private const string BattleRewardPath =
        "Assets/Resources/UI/BattleRewardPanel.prefab";
    private const string GameOverPath =
        "Assets/Resources/UI/GameOverPanel.prefab";
    private const string FontPath =
        "Assets/Arts/Font/fusion-pixel-12px-proportional-zh_hans SDF D.asset";

    static CombatUIPrefabGenerator()
    {
        EditorApplication.delayCall += GenerateMissingPrefabs;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    [MenuItem("Tools/Zero/Combat/Generate Missing UI Prefabs %#g")]
    public static void GenerateMissingPrefabs()
    {
        bool created = false;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PreBattlePath) == null)
        {
            GeneratePreBattleRollPanel();
            created = true;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(EnemyWorldPath) == null)
        {
            GenerateEnemyWorldUI();
            created = true;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(BattleActionPath) == null)
        {
            GenerateBattleActionPanel();
            created = true;
        }
        else if (EnsureBattleActionPanelStruggleButton())
        {
            created = true;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(BattleRewardPath) == null)
        {
            GenerateBattleRewardPanel();
            created = true;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(GameOverPath) == null)
        {
            GenerateGameOverPanel();
            created = true;
        }

        if (created)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated or upgraded Zero combat UI prefabs.");
        }
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += GenerateMissingPrefabs;
        }
    }

    private static void GeneratePreBattleRollPanel()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Sprite panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
            "UI/Skin/UISprite.psd"
        );

        GameObject root = CreateObject(
            "PreBattleRollPanel",
            null,
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(PreBattleRollPanel)
        );
        RectTransform rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect);
        Image overlay = root.GetComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.72f);
        overlay.raycastTarget = true;

        PreBattleRollPanel panel = root.GetComponent<PreBattleRollPanel>();
        panel.layer = UILayer.Popup;

        GameObject window = CreateObject(
            "Window",
            rootRect,
            typeof(CanvasRenderer),
            typeof(Image)
        );
        SetRect(window.GetComponent<RectTransform>(), Vector2.zero, new Vector2(760f, 520f));
        Image windowImage = window.GetComponent<Image>();
        windowImage.sprite = panelSprite;
        windowImage.type = Image.Type.Sliced;
        windowImage.color = new Color(0.12f, 0.13f, 0.16f, 0.98f);

        TMP_Text enemyName = CreateText(
            "EnemyNameText",
            window.transform,
            font,
            new Vector2(0f, 185f),
            new Vector2(640f, 70f),
            "怪物名称",
            42f
        );
        TMP_Text healthRange = CreateText(
            "HealthRangeText",
            window.transform,
            font,
            new Vector2(0f, 95f),
            new Vector2(620f, 55f),
            "生命范围：8～12",
            30f
        );
        TMP_Text reward = CreateText(
            "RewardText",
            window.transform,
            font,
            new Vector2(0f, 35f),
            new Vector2(620f, 55f),
            "基础掉落：12",
            30f
        );
        TMP_Text stableHealth = CreateText(
            "StableHealthText",
            window.transform,
            font,
            new Vector2(0f, -35f),
            new Vector2(620f, 55f),
            "不 ROLL：生命 11",
            28f
        );

        Button rollButton = CreateButton(
            "RollButton",
            window.transform,
            font,
            panelSprite,
            new Vector2(-170f, -155f),
            "ROLL"
        );
        Button stableButton = CreateButton(
            "StableButton",
            window.transform,
            font,
            panelSprite,
            new Vector2(170f, -155f),
            "不 ROLL"
        );

        SerializedObject serialized = new(panel);
        serialized.FindProperty("enemyNameText").objectReferenceValue = enemyName;
        serialized.FindProperty("healthRangeText").objectReferenceValue = healthRange;
        serialized.FindProperty("rewardText").objectReferenceValue = reward;
        serialized.FindProperty("stableHealthText").objectReferenceValue = stableHealth;
        serialized.FindProperty("rollButton").objectReferenceValue = rollButton;
        serialized.FindProperty("stableButton").objectReferenceValue = stableButton;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PreBattlePath);
        Object.DestroyImmediate(root);
    }

    private static void GenerateEnemyWorldUI()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Sprite panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
            "UI/Skin/UISprite.psd"
        );

        GameObject root = CreateObject(
            "EnemyWorldUI",
            null,
            typeof(Canvas),
            typeof(CanvasGroup),
            typeof(EnemyWorldUI)
        );
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(320f, 190f);
        rootRect.localScale = Vector3.one * 0.005f;

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingLayerID = SortingLayer.NameToID("Player");
        canvas.sortingOrder = 100;

        GameObject rewardRoot = CreateWorldRow(
            "RewardRoot",
            rootRect,
            panelSprite,
            new Vector2(0f, 60f)
        );
        TMP_Text rewardText = CreateText(
            "RewardText",
            rewardRoot.transform,
            font,
            Vector2.zero,
            new Vector2(280f, 48f),
            "掉落 12",
            28f
        );

        GameObject healthRoot = CreateWorldRow(
            "HealthRoot",
            rootRect,
            panelSprite,
            new Vector2(0f, 5f)
        );
        TMP_Text healthText = CreateText(
            "HealthText",
            healthRoot.transform,
            font,
            Vector2.zero,
            new Vector2(280f, 48f),
            "HP 11/11",
            28f
        );

        GameObject intentRoot = CreateWorldRow(
            "IntentRoot",
            rootRect,
            panelSprite,
            new Vector2(0f, -50f)
        );
        TMP_Text intentText = CreateText(
            "IntentText",
            intentRoot.transform,
            font,
            Vector2.zero,
            new Vector2(280f, 48f),
            "意图：等待",
            26f
        );

        EnemyWorldUI worldUI = root.GetComponent<EnemyWorldUI>();
        SerializedObject serialized = new(worldUI);
        serialized.FindProperty("rewardRoot").objectReferenceValue = rewardRoot;
        serialized.FindProperty("rewardText").objectReferenceValue = rewardText;
        serialized.FindProperty("healthRoot").objectReferenceValue = healthRoot;
        serialized.FindProperty("healthText").objectReferenceValue = healthText;
        serialized.FindProperty("intentRoot").objectReferenceValue = intentRoot;
        serialized.FindProperty("intentText").objectReferenceValue = intentText;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        healthRoot.SetActive(false);
        intentRoot.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(root, EnemyWorldPath);
        Object.DestroyImmediate(root);
    }

    private static void GenerateBattleActionPanel()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Sprite panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
            "UI/Skin/UISprite.psd"
        );

        GameObject root = CreateObject(
            "BattleActionPanel",
            null,
            typeof(BattleActionPanel)
        );
        Stretch(root.GetComponent<RectTransform>());
        BattleActionPanel panel = root.GetComponent<BattleActionPanel>();
        panel.layer = UILayer.Popup;

        GameObject window = CreateObject(
            "Window",
            root.transform,
            typeof(CanvasRenderer),
            typeof(Image)
        );
        SetRect(
            window.GetComponent<RectTransform>(),
            new Vector2(0f, -405f),
            new Vector2(760f, 300f)
        );
        Image windowImage = window.GetComponent<Image>();
        windowImage.sprite = panelSprite;
        windowImage.type = Image.Type.Sliced;
        windowImage.color = new Color(0.1f, 0.11f, 0.14f, 0.96f);

        TMP_Text preview = CreateText(
            "PreviewText",
            window.transform,
            font,
            new Vector2(-105f, 48f),
            new Vector2(500f, 110f),
            "普通攻击  消耗 1  伤害 3\n敌人生命：11 → 8",
            28f
        );
        preview.alignment = TextAlignmentOptions.Left;

        TMP_Text feedback = CreateText(
            "FeedbackText",
            window.transform,
            font,
            new Vector2(-105f, -72f),
            new Vector2(500f, 45f),
            string.Empty,
            24f
        );
        feedback.color = new Color(1f, 0.55f, 0.45f);

        Button attack = CreateButton(
            "AttackButton",
            window.transform,
            font,
            panelSprite,
            new Vector2(245f, 50f),
            "攻击"
        );
        Button struggle = CreateButton(
            "StruggleButton",
            window.transform,
            font,
            panelSprite,
            new Vector2(245f, -55f),
            "挣扎"
        );

        SerializedObject serialized = new(panel);
        serialized.FindProperty("previewText").objectReferenceValue = preview;
        serialized.FindProperty("feedbackText").objectReferenceValue = feedback;
        serialized.FindProperty("attackButton").objectReferenceValue = attack;
        serialized.FindProperty("struggleButton").objectReferenceValue = struggle;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, BattleActionPath);
        Object.DestroyImmediate(root);
    }

    private static bool EnsureBattleActionPanelStruggleButton()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(BattleActionPath);
        try
        {
            BattleActionPanel panel = root.GetComponent<BattleActionPanel>();
            if (panel == null)
            {
                return false;
            }

            SerializedObject serialized = new(panel);
            SerializedProperty struggleProperty =
                serialized.FindProperty("struggleButton");
            if (struggleProperty == null ||
                struggleProperty.objectReferenceValue != null)
            {
                return false;
            }

            Transform window = root.transform.Find("Window");
            if (window == null)
            {
                return false;
            }

            TMP_FontAsset font =
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Sprite panelSprite =
                AssetDatabase.GetBuiltinExtraResource<Sprite>(
                    "UI/Skin/UISprite.psd"
                );
            Button struggle = CreateButton(
                "StruggleButton",
                window,
                font,
                panelSprite,
                new Vector2(245f, -85f),
                "挣扎"
            );
            struggleProperty.objectReferenceValue = struggle;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            RectTransform windowRect = window.GetComponent<RectTransform>();
            if (windowRect != null && windowRect.sizeDelta.y < 300f)
            {
                windowRect.sizeDelta = new Vector2(
                    windowRect.sizeDelta.x,
                    300f
                );
            }

            PrefabUtility.SaveAsPrefabAsset(root, BattleActionPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void GenerateBattleRewardPanel()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Sprite panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
            "UI/Skin/UISprite.psd"
        );

        GameObject root = CreateObject(
            "BattleRewardPanel",
            null,
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(BattleRewardPanel)
        );
        Stretch(root.GetComponent<RectTransform>());
        Image overlay = root.GetComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.76f);
        overlay.raycastTarget = true;

        BattleRewardPanel panel = root.GetComponent<BattleRewardPanel>();
        panel.layer = UILayer.Popup;

        GameObject window = CreateObject(
            "Window",
            root.transform,
            typeof(CanvasRenderer),
            typeof(Image)
        );
        SetRect(
            window.GetComponent<RectTransform>(),
            Vector2.zero,
            new Vector2(860f, 650f)
        );
        Image windowImage = window.GetComponent<Image>();
        windowImage.sprite = panelSprite;
        windowImage.type = Image.Type.Sliced;
        windowImage.color = new Color(0.1f, 0.11f, 0.14f, 0.98f);

        TMP_Text title = CreateText(
            "TitleText",
            window.transform,
            font,
            new Vector2(0f, 270f),
            new Vector2(720f, 70f),
            "战斗胜利",
            44f
        );
        title.color = new Color(1f, 0.82f, 0.3f);

        TMP_Text summary = CreateText(
            "SummaryText",
            window.transform,
            font,
            new Vector2(0f, 155f),
            new Vector2(700f, 145f),
            "基础掉落：12\n本场损失：-6\n本场数字：6",
            30f
        );

        TMP_Text safe = CreateText(
            "SafeText",
            window.transform,
            font,
            new Vector2(-210f, 5f),
            new Vector2(330f, 105f),
            "获得 6（100%）",
            28f
        );

        TMP_Text greedy = CreateText(
            "GreedyText",
            window.transform,
            font,
            new Vector2(210f, 5f),
            new Vector2(330f, 105f),
            "50% 获得 15\n50% 获得 0",
            28f
        );

        Button safeButton = CreateButton(
            "SafeButton",
            window.transform,
            font,
            panelSprite,
            new Vector2(-210f, -105f),
            "安全领取"
        );
        Button greedyButton = CreateButton(
            "GreedyButton",
            window.transform,
            font,
            panelSprite,
            new Vector2(210f, -105f),
            "贪婪"
        );

        TMP_Text result = CreateText(
            "ResultText",
            window.transform,
            font,
            new Vector2(0f, -235f),
            new Vector2(700f, 70f),
            "道具不会因贪婪失败而丢失",
            27f
        );
        result.color = new Color(0.7f, 0.85f, 1f);

        SerializedObject serialized = new(panel);
        serialized.FindProperty("summaryText").objectReferenceValue = summary;
        serialized.FindProperty("safeText").objectReferenceValue = safe;
        serialized.FindProperty("greedyText").objectReferenceValue = greedy;
        serialized.FindProperty("resultText").objectReferenceValue = result;
        serialized.FindProperty("safeButton").objectReferenceValue = safeButton;
        serialized.FindProperty("greedyButton").objectReferenceValue = greedyButton;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, BattleRewardPath);
        Object.DestroyImmediate(root);
    }

    private static void GenerateGameOverPanel()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Sprite panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
            "UI/Skin/UISprite.psd"
        );

        GameObject root = CreateObject(
            "GameOverPanel",
            null,
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(GameOverPanel)
        );
        Stretch(root.GetComponent<RectTransform>());
        Image overlay = root.GetComponent<Image>();
        overlay.color = new Color(0.03f, 0f, 0.02f, 0.9f);
        overlay.raycastTarget = true;

        GameOverPanel panel = root.GetComponent<GameOverPanel>();
        panel.layer = UILayer.Top;

        GameObject window = CreateObject(
            "Window",
            root.transform,
            typeof(CanvasRenderer),
            typeof(Image)
        );
        SetRect(
            window.GetComponent<RectTransform>(),
            Vector2.zero,
            new Vector2(780f, 520f)
        );
        Image windowImage = window.GetComponent<Image>();
        windowImage.sprite = panelSprite;
        windowImage.type = Image.Type.Sliced;
        windowImage.color = new Color(0.13f, 0.06f, 0.08f, 0.98f);

        TMP_Text title = CreateText(
            "TitleText",
            window.transform,
            font,
            new Vector2(0f, 175f),
            new Vector2(650f, 80f),
            "跌破归零",
            52f
        );
        title.color = new Color(1f, 0.3f, 0.25f);

        TMP_Text reason = CreateText(
            "ReasonText",
            window.transform,
            font,
            new Vector2(0f, 80f),
            new Vector2(620f, 60f),
            "数字跌破 0",
            32f
        );

        TMP_Text finalNumber = CreateText(
            "FinalNumberText",
            window.transform,
            font,
            new Vector2(0f, 5f),
            new Vector2(620f, 60f),
            "最终数字：-1",
            34f
        );

        Button retry = CreateButton(
            "RetryButton",
            window.transform,
            font,
            panelSprite,
            new Vector2(-170f, -135f),
            "重新开始"
        );
        Button titleButton = CreateButton(
            "TitleButton",
            window.transform,
            font,
            panelSprite,
            new Vector2(170f, -135f),
            "返回标题"
        );

        SerializedObject serialized = new(panel);
        serialized.FindProperty("reasonText").objectReferenceValue = reason;
        serialized.FindProperty("finalNumberText").objectReferenceValue =
            finalNumber;
        serialized.FindProperty("retryButton").objectReferenceValue = retry;
        serialized.FindProperty("titleButton").objectReferenceValue =
            titleButton;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, GameOverPath);
        Object.DestroyImmediate(root);
    }

    private static GameObject CreateWorldRow(
        string name,
        RectTransform parent,
        Sprite sprite,
        Vector2 position
    )
    {
        GameObject row = CreateObject(
            name,
            parent,
            typeof(CanvasRenderer),
            typeof(Image)
        );
        SetRect(row.GetComponent<RectTransform>(), position, new Vector2(300f, 50f));
        Image image = row.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.05f, 0.05f, 0.07f, 0.88f);
        image.raycastTarget = false;
        return row;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        TMP_FontAsset font,
        Vector2 position,
        Vector2 size,
        string value,
        float fontSize
    )
    {
        GameObject gameObject = CreateObject(
            name,
            parent,
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        SetRect(gameObject.GetComponent<RectTransform>(), position, size);

        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        if (font != null)
        {
            text.font = font;
        }

        text.text = value;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        TMP_FontAsset font,
        Sprite sprite,
        Vector2 position,
        string label
    )
    {
        GameObject gameObject = CreateObject(
            name,
            parent,
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );
        SetRect(
            gameObject.GetComponent<RectTransform>(),
            position,
            new Vector2(270f, 86f)
        );

        Image image = gameObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.83f, 0.67f, 0.28f, 1f);

        Button button = gameObject.GetComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text = CreateText(
            "Label",
            gameObject.transform,
            font,
            Vector2.zero,
            new Vector2(250f, 76f),
            label,
            30f
        );
        text.color = Color.black;
        return button;
    }

    private static GameObject CreateObject(
        string name,
        Transform parent,
        params System.Type[] components
    )
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        foreach (System.Type component in components)
        {
            gameObject.AddComponent(component);
        }

        gameObject.layer = LayerMask.NameToLayer("UI");
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        return gameObject;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchoredPosition,
        Vector2 size
    )
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }
}
#endif

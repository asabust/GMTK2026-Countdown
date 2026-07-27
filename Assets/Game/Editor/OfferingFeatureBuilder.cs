#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OfferingFeatureBuilder
{
    private const string FontPath =
        "Assets/Arts/Font/fusion-pixel-12px-proportional-zh_hans SDF D.asset";
    private const string PortraitPath = "Assets/Arts/Character/商人.png";
    private const string DialoguePath =
        "Assets/Arts/UI/事件-占位符/对话框.png";
    private const string ButtonPath =
        "Assets/Arts/UI/事件-占位符/常规选择按钮.png";
    private const string OfferingBarPath =
        "Assets/Arts/UI/事件-占位符/献祭长条.png";
    private const string OfferingHandlePath =
        "Assets/Arts/UI/事件-占位符/献祭条滑杆.png";
    private const string OfferingIconPath =
        "Assets/Arts/UI/图标/事件图标.png";
    private const string OfferingFolder = "Assets/Game/Data/Offerings";
    private const string OfferingAssetPath =
        OfferingFolder + "/DefaultOffering.asset";

    [MenuItem("Tools/Zero/Build Offering Feature")]
    public static void Build()
    {
        EnsureFolder("Assets/Game/Data", "Offerings");
        CollectibleDefinition[] items =
        {
            LoadCollectible("Wrench"),
            LoadCollectible("GirlsThoughts"),
            LoadCollectible("GuardianShield"),
            LoadCollectible("MagicPotion")
        };
        if (items.Any(item => item == null))
        {
            Debug.LogError(
                "All four battle item collectibles are required. " +
                "Run Tools/Zero/Build Battle Item Feature first."
            );
            return;
        }

        foreach (CollectibleDefinition item in items)
        {
            SetMaximumStacks(item, 3);
        }
        OfferingDefinition definition = CreateDefinition(items);
        CreatePanelPrefab();

        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        ConfigurePersistentPlayer();
        AddOfferingToGameScene(definition);
        EditorSceneManager.RestoreSceneManagerSetup(setup);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Offering feature assets and scenes created.");
    }

    private static OfferingDefinition CreateDefinition(
        IReadOnlyList<CollectibleDefinition> items
    )
    {
        OfferingDefinition definition =
            AssetDatabase.LoadAssetAtPath<OfferingDefinition>(
                OfferingAssetPath
            );
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<OfferingDefinition>();
            AssetDatabase.CreateAsset(definition, OfferingAssetPath);
        }

        SerializedObject serialized = new(definition);
        serialized.FindProperty("offeringId").stringValue = "default_offering";
        serialized.FindProperty("maximumAmount").intValue =
            OfferingDefinition.MaximumAllowedAmount;
        serialized.FindProperty("attackIncrease").intValue = 1;

        OfferingOutcomeType[] outcomeTypes =
        {
            OfferingOutcomeType.RandomItem,
            OfferingOutcomeType.LoseAll,
            OfferingOutcomeType.AttackIncrease,
            OfferingOutcomeType.DoubleReturn,
            OfferingOutcomeType.FullReturn
        };
        int[] weights = { 10, 30, 20, 20, 20 };
        SerializedProperty outcomes = serialized.FindProperty("outcomes");
        outcomes.arraySize = outcomeTypes.Length;
        for (int i = 0; i < outcomeTypes.Length; i++)
        {
            SerializedProperty entry = outcomes.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("outcome").enumValueIndex =
                (int)outcomeTypes[i];
            entry.FindPropertyRelative("weight").intValue = weights[i];
        }

        SerializedProperty pool = serialized.FindProperty("itemPool");
        pool.arraySize = items.Count;
        for (int i = 0; i < items.Count; i++)
        {
            SerializedProperty poolEntry = pool.GetArrayElementAtIndex(i);
            poolEntry.FindPropertyRelative("collectible").objectReferenceValue =
                items[i];
            poolEntry.FindPropertyRelative("weight").intValue = 1;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static CollectibleDefinition LoadCollectible(string assetName) =>
        AssetDatabase.LoadAssetAtPath<CollectibleDefinition>(
            $"Assets/Game/Data/Collectibles/{assetName}.asset"
        );

    private static void SetMaximumStacks(
        CollectibleDefinition definition,
        int maximum
    )
    {
        SerializedObject serialized = new(definition);
        serialized.FindProperty("maximumStacks").intValue = maximum;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
    }

    private static void ConfigurePersistentPlayer()
    {
        Scene scene = EditorSceneManager.OpenScene(
            "Assets/Scenes/Persistent.unity",
            OpenSceneMode.Single
        );
        GameObject player = scene.GetRootGameObjects()
            .First(root => root.name == "Player");

        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            inventory = player.AddComponent<PlayerInventory>();
        }
        SerializedObject serializedInventory = new(inventory);
        serializedInventory.FindProperty("itemSlotCapacity").intValue = 4;
        serializedInventory.ApplyModifiedPropertiesWithoutUndo();

        if (player.GetComponent<PlayerRunStats>() == null)
        {
            player.AddComponent<PlayerRunStats>();
        }

        EditorSceneManager.SaveScene(scene);
    }

    private static void AddOfferingToGameScene(
        OfferingDefinition definition
    )
    {
        Scene scene = EditorSceneManager.OpenScene(
            "Assets/Scenes/GameScene.unity",
            OpenSceneMode.Single
        );
        GameObject offering = scene.GetRootGameObjects()
            .FirstOrDefault(root => root.name == "TestOffering");
        if (offering == null)
        {
            offering = new GameObject("TestOffering");
            offering.transform.position = new Vector3(1.5f, 0.5f, 0f);

            GridObject grid = offering.AddComponent<GridObject>();
            SerializedObject gridData = new(grid);
            gridData.FindProperty("occupantType").enumValueIndex =
                (int)GridOccupantType.Interactable;
            gridData.ApplyModifiedPropertiesWithoutUndo();

            offering.AddComponent<FogVisibilityTarget>();
            offering.AddComponent<OfferingInteractable>();

            SpriteRenderer renderer = offering.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite(OfferingIconPath);
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = 5;
            renderer.color = new Color(0.78f, 0.36f, 0.42f, 1f);
            renderer.transform.localScale = Vector3.one * 0.72f;
        }

        OfferingInteractable interactable =
            offering.GetComponent<OfferingInteractable>();
        SerializedObject serialized = new(interactable);
        serialized.FindProperty("definition").objectReferenceValue = definition;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene);
    }

    private static void CreatePanelPrefab()
    {
        TMP_FontAsset font =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Sprite dialogue = LoadSprite(DialoguePath);
        Sprite buttonSprite = LoadSprite(ButtonPath);

        GameObject root = UIObject("OfferingPanel", null);
        Stretch(root.GetComponent<RectTransform>());
        Image blocker = root.AddComponent<Image>();
        blocker.color = new Color(0.035f, 0.025f, 0.035f, 0.9f);
        OfferingPanel panel = root.AddComponent<OfferingPanel>();
        panel.layer = UILayer.Popup;

        Image portrait = ImageObject(
            "OfferingPortrait",
            root.transform,
            LoadSprite(PortraitPath),
            new Vector2(-600f, 30f),
            new Vector2(420f, 540f)
        );
        portrait.preserveAspect = true;
        portrait.color = new Color(0.13f, 0.11f, 0.15f, 1f);

        Text(
            "Title",
            root.transform,
            font,
            "供奉",
            46f,
            new Vector2(190f, 445f),
            new Vector2(500f, 70f),
            new Color(1f, 0.78f, 0.45f)
        );

        Slider slider = SliderObject(
            root.transform,
            LoadSprite(OfferingBarPath),
            LoadSprite(OfferingHandlePath),
            new Vector2(210f, 330f),
            new Vector2(900f, 58f)
        );
        TMP_Text amountText = Text(
            "AmountText",
            root.transform,
            font,
            "1 / 100",
            34f,
            new Vector2(210f, 385f),
            new Vector2(500f, 55f),
            Color.white
        );
        TMP_Text previewText = Text(
            "PreviewText",
            root.transform,
            font,
            "供奉后：100 > 99",
            28f,
            new Vector2(210f, 270f),
            new Vector2(700f, 50f),
            new Color(1f, 0.78f, 0.45f)
        );

        Button minusTen = ButtonObject(
            "DecreaseTen",
            root.transform,
            font,
            "-10",
            new Vector2(-90f, 195f),
            new Vector2(165f, 75f),
            buttonSprite,
            out _
        );
        Button minusOne = ButtonObject(
            "DecreaseOne",
            root.transform,
            font,
            "-1",
            new Vector2(105f, 195f),
            new Vector2(165f, 75f),
            buttonSprite,
            out _
        );
        Button plusOne = ButtonObject(
            "IncreaseOne",
            root.transform,
            font,
            "+1",
            new Vector2(300f, 195f),
            new Vector2(165f, 75f),
            buttonSprite,
            out _
        );
        Button plusTen = ButtonObject(
            "IncreaseTen",
            root.transform,
            font,
            "+10",
            new Vector2(495f, 195f),
            new Vector2(165f, 75f),
            buttonSprite,
            out _
        );

        Image dialogueBox = ImageObject(
            "DialogueBox",
            root.transform,
            dialogue,
            new Vector2(120f, -330f),
            new Vector2(1384f, 394f)
        );
        dialogueBox.type = Image.Type.Sliced;
        TMP_Text dialogueText = Text(
            "DialogueText",
            dialogueBox.transform,
            font,
            "选择你要供奉的数字。",
            30f,
            new Vector2(-160f, 65f),
            new Vector2(780f, 80f),
            new Color(0.12f, 0.1f, 0.12f)
        );
        TMP_Text feedbackText = Text(
            "FeedbackText",
            dialogueBox.transform,
            font,
            string.Empty,
            25f,
            new Vector2(-160f, -45f),
            new Vector2(790f, 120f),
            new Color(0.48f, 0.12f, 0.08f)
        );
        Button confirm = ButtonObject(
            "ConfirmButton",
            dialogueBox.transform,
            font,
            "确认供奉",
            new Vector2(480f, 50f),
            new Vector2(315f, 100f),
            buttonSprite,
            out TMP_Text confirmText
        );
        Button leave = ButtonObject(
            "LeaveButton",
            dialogueBox.transform,
            font,
            "离开",
            new Vector2(480f, -75f),
            new Vector2(315f, 100f),
            buttonSprite,
            out _
        );

        SerializedObject serializedPanel = new(panel);
        SetReference(serializedPanel, "amountSlider", slider);
        SetReference(serializedPanel, "amountText", amountText);
        SetReference(serializedPanel, "previewText", previewText);
        SetReference(serializedPanel, "dialogueText", dialogueText);
        SetReference(serializedPanel, "feedbackText", feedbackText);
        SetReference(serializedPanel, "decreaseTenButton", minusTen);
        SetReference(serializedPanel, "decreaseOneButton", minusOne);
        SetReference(serializedPanel, "increaseOneButton", plusOne);
        SetReference(serializedPanel, "increaseTenButton", plusTen);
        SetReference(serializedPanel, "confirmButton", confirm);
        SetReference(serializedPanel, "confirmButtonText", confirmText);
        SetReference(serializedPanel, "leaveButton", leave);
        serializedPanel.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(
            root,
            "Assets/Resources/UI/OfferingPanel.prefab"
        );
        Object.DestroyImmediate(root);
    }

    private static Slider SliderObject(
        Transform parent,
        Sprite backgroundSprite,
        Sprite handleSprite,
        Vector2 position,
        Vector2 size
    )
    {
        GameObject root = UIObject("AmountSlider", parent);
        Center(root.GetComponent<RectTransform>(), position, size);
        Slider slider = root.AddComponent<Slider>();
        slider.wholeNumbers = true;
        slider.minValue = 1f;
        slider.maxValue = 100f;
        slider.value = 1f;

        Image background = ImageObject(
            "Background",
            root.transform,
            backgroundSprite,
            Vector2.zero,
            size
        );
        background.type = Image.Type.Sliced;
        background.color = new Color(0.35f, 0.2f, 0.2f, 1f);

        GameObject fillArea = UIObject("Fill Area", root.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        Stretch(fillAreaRect);
        fillAreaRect.offsetMin = new Vector2(8f, 8f);
        fillAreaRect.offsetMax = new Vector2(-8f, -8f);
        Image fill = ImageObject(
            "Fill",
            fillArea.transform,
            null,
            Vector2.zero,
            size
        );
        RectTransform fillRect = fill.rectTransform;
        Stretch(fillRect);
        fill.color = new Color(0.92f, 0.13f, 0.08f, 1f);

        GameObject handleArea = UIObject("Handle Slide Area", root.transform);
        RectTransform handleAreaRect =
            handleArea.GetComponent<RectTransform>();
        Stretch(handleAreaRect);
        handleAreaRect.offsetMin = new Vector2(25f, 0f);
        handleAreaRect.offsetMax = new Vector2(-25f, 0f);
        Image handle = ImageObject(
            "Handle",
            handleArea.transform,
            handleSprite,
            Vector2.zero,
            new Vector2(74f, 105f)
        );
        handle.preserveAspect = true;

        slider.fillRect = fillRect;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private static void SetReference(
        SerializedObject target,
        string property,
        Object value
    )
    {
        target.FindProperty(property).objectReferenceValue = value;
    }

    private static Sprite LoadSprite(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .FirstOrDefault();

    private static void EnsureFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static GameObject UIObject(string name, Transform parent)
    {
        GameObject go = new(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Image ImageObject(
        string name,
        Transform parent,
        Sprite sprite,
        Vector2 position,
        Vector2 size
    )
    {
        GameObject go = UIObject(name, parent);
        Center(go.GetComponent<RectTransform>(), position, size);
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        return image;
    }

    private static TMP_Text Text(
        string name,
        Transform parent,
        TMP_FontAsset font,
        string value,
        float fontSize,
        Vector2 position,
        Vector2 size,
        Color color
    )
    {
        GameObject go = UIObject(name, parent);
        Center(go.GetComponent<RectTransform>(), position, size);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private static Button ButtonObject(
        string name,
        Transform parent,
        TMP_FontAsset font,
        string label,
        Vector2 position,
        Vector2 size,
        Sprite sprite,
        out TMP_Text labelText
    )
    {
        Image image = ImageObject(name, parent, sprite, position, size);
        image.type = Image.Type.Sliced;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        labelText = Text(
            "Label",
            image.transform,
            font,
            label,
            27f,
            Vector2.zero,
            size - new Vector2(20f, 20f),
            Color.black
        );
        return button;
    }

    private static void Center(
        RectTransform rect,
        Vector2 position,
        Vector2 size
    )
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
#endif

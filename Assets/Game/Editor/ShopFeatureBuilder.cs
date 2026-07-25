using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ShopFeatureBuilder
{
    private const string FontPath =
        "Assets/Arts/Font/fusion-pixel-12px-proportional-zh_hans SDF.asset";
    private const string MerchantPath = "Assets/Arts/Character/商人.png";
    private const string DialoguePath =
        "Assets/Arts/UI/事件-占位符/对话框.png";
    private const string ButtonPath =
        "Assets/Arts/UI/事件-占位符/常规选择按钮.png";
    private const string PricePath =
        "Assets/Arts/UI/事件-占位符/价格标签.png";
    private const string ShopIconPath = "Assets/Arts/UI/图标/商店图标.png";
    private const string DataFolder = "Assets/Game/Data/Collectibles";

    static ShopFeatureBuilder()
    {
        EditorApplication.delayCall += BuildWhenNeeded;
    }

    private static void BuildWhenNeeded()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode &&
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/UI/ShopPanel.prefab"
            ) == null)
        {
            Build();
        }
    }

    [MenuItem("Tools/Zero/Build Shop Feature")]
    public static void Build()
    {
        EnsureFolder("Assets/Game", "Data");
        EnsureFolder("Assets/Game/Data", "Collectibles");

        CollectibleDefinition potion = CreateDefinition(
            "MagicPotion", "magic_potion", "魔药",
            "一次性道具。使用后恢复 15 点数字。", CollectibleKind.Item,
            9, CollectibleEffectType.RestoreNumber, 15f,
            "Assets/Arts/UI/图标/技能1.png"
        );
        CollectibleDefinition coin = CreateDefinition(
            "LuckyCoin", "lucky_coin", "幸运硬币",
            "每层使第一次贪婪成功率提高 10%。", CollectibleKind.Relic,
            3, CollectibleEffectType.GreedChanceBonus, 0.1f,
            "Assets/Arts/UI/图标/技能2.png"
        );
        CollectibleDefinition wolf = CreateDefinition(
            "HungryWolf", "hungry_wolf", "饿狼",
            "将贪婪成功倍率从 2.5 倍改为 3.5 倍。", CollectibleKind.Relic,
            1, CollectibleEffectType.GreedMultiplierOverride, 3.5f,
            "Assets/Arts/UI/图标/技能3.png"
        );

        CreateShopPanelPrefab();
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        AddInventoryToPlayer();
        AddShopToGameScene(potion, coin, wolf);
        EditorSceneManager.RestoreSceneManagerSetup(setup);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Shop feature assets and scenes created.");
    }

    private static CollectibleDefinition CreateDefinition(
        string assetName,
        string id,
        string displayName,
        string description,
        CollectibleKind kind,
        int maximumStacks,
        CollectibleEffectType effect,
        float effectValue,
        string iconPath
    )
    {
        string path = $"{DataFolder}/{assetName}.asset";
        CollectibleDefinition definition =
            AssetDatabase.LoadAssetAtPath<CollectibleDefinition>(path);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<CollectibleDefinition>();
            AssetDatabase.CreateAsset(definition, path);
        }

        SerializedObject serialized = new(definition);
        serialized.FindProperty("collectibleId").stringValue = id;
        serialized.FindProperty("displayName").stringValue = displayName;
        serialized.FindProperty("description").stringValue = description;
        serialized.FindProperty("icon").objectReferenceValue = LoadSprite(iconPath);
        serialized.FindProperty("kind").enumValueIndex = (int)kind;
        serialized.FindProperty("maximumStacks").intValue = maximumStacks;
        serialized.FindProperty("effectType").enumValueIndex = (int)effect;
        serialized.FindProperty("effectValue").floatValue = effectValue;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static void CreateShopPanelPrefab()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Sprite dialogue = LoadSprite(DialoguePath);
        Sprite buttonSprite = LoadSprite(ButtonPath);
        Sprite priceSprite = LoadSprite(PricePath);

        GameObject root = UIObject("ShopPanel", null);
        Stretch(root.GetComponent<RectTransform>());
        Image blocker = root.AddComponent<Image>();
        blocker.color = new Color(0.04f, 0.04f, 0.05f, 0.88f);
        ShopPanel panel = root.AddComponent<ShopPanel>();
        panel.layer = UILayer.Popup;

        Image portrait = ImageObject(
            "MerchantPortrait", root.transform, LoadSprite(MerchantPath),
            new Vector2(-610f, 70f), new Vector2(400f, 500f)
        );
        portrait.preserveAspect = true;

        TMP_Text number = Text(
            "NumberText", root.transform, font, "10", 52f,
            new Vector2(-710f, 410f), new Vector2(220f, 80f), Color.white
        );
        Text("NumberLabel", root.transform, font, "当前数字", 24f,
            new Vector2(-710f, 465f), new Vector2(220f, 40f),
            new Color(1f, 0.82f, 0.25f));

        ShopProductView[] views = new ShopProductView[6];
        for (int i = 0; i < views.Length; i++)
        {
            int column = i % 3;
            int row = i / 3;
            Vector2 position = new(-180f + column * 330f, 260f - row * 230f);
            views[i] = ProductCard(
                root.transform, font, priceSprite, i, position
            );
        }

        Image dialogueBox = ImageObject(
            "DialogueBox", root.transform, dialogue,
            new Vector2(120f, -350f), new Vector2(1384f, 394f)
        );
        dialogueBox.type = Image.Type.Sliced;
        TMP_Text dialogueText = Text(
            "DialogueText", dialogueBox.transform, font,
            "看看这些东西吧。", 27f, new Vector2(-120f, 20f),
            new Vector2(850f, 190f), Color.white
        );
        TMP_Text feedback = Text(
            "FeedbackText", dialogueBox.transform, font, string.Empty, 23f,
            new Vector2(-120f, -90f), new Vector2(850f, 45f),
            new Color(1f, 0.62f, 0.3f)
        );
        Button buy = ButtonObject(
            "BuyButton", dialogueBox.transform, font, "购买",
            new Vector2(480f, 45f), new Vector2(310f, 100f),
            buttonSprite, out TMP_Text buyLabel
        );
        Button leave = ButtonObject(
            "LeaveButton", dialogueBox.transform, font, "离开",
            new Vector2(480f, -75f), new Vector2(310f, 100f),
            buttonSprite, out _
        );

        SerializedObject serializedPanel = new(panel);
        serializedPanel.FindProperty("merchantPortrait").objectReferenceValue =
            portrait;
        serializedPanel.FindProperty("numberText").objectReferenceValue = number;
        serializedPanel.FindProperty("dialogueText").objectReferenceValue =
            dialogueText;
        serializedPanel.FindProperty("feedbackText").objectReferenceValue =
            feedback;
        serializedPanel.FindProperty("buyButton").objectReferenceValue = buy;
        serializedPanel.FindProperty("buyButtonText").objectReferenceValue =
            buyLabel;
        serializedPanel.FindProperty("leaveButton").objectReferenceValue = leave;

        SerializedProperty productViews =
            serializedPanel.FindProperty("productViews");
        productViews.arraySize = views.Length;
        for (int i = 0; i < views.Length; i++)
        {
            SerializedProperty element = productViews.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("selectButton").objectReferenceValue =
                views[i].selectButton;
            element.FindPropertyRelative("icon").objectReferenceValue =
                views[i].icon;
            element.FindPropertyRelative("nameText").objectReferenceValue =
                views[i].nameText;
            element.FindPropertyRelative("priceText").objectReferenceValue =
                views[i].priceText;
            element.FindPropertyRelative("stateText").objectReferenceValue =
                views[i].stateText;
        }
        serializedPanel.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(
            root, "Assets/Resources/UI/ShopPanel.prefab"
        );
        Object.DestroyImmediate(root);
    }

    private static ShopProductView ProductCard(
        Transform parent,
        TMP_FontAsset font,
        Sprite priceSprite,
        int index,
        Vector2 position
    )
    {
        GameObject card = UIObject($"Product{index + 1}", parent);
        RectTransform rect = card.GetComponent<RectTransform>();
        Center(rect, position, new Vector2(280f, 200f));
        Image background = card.AddComponent<Image>();
        background.color = new Color(0.16f, 0.17f, 0.2f, 0.96f);
        Button button = card.AddComponent<Button>();
        button.targetGraphic = background;

        Image icon = ImageObject(
            "Icon", card.transform, null,
            new Vector2(0f, 30f), new Vector2(105f, 105f)
        );
        icon.preserveAspect = true;
        TMP_Text name = Text(
            "Name", card.transform, font, "商品", 23f,
            new Vector2(0f, -48f), new Vector2(250f, 38f), Color.white
        );
        Image price = ImageObject(
            "PriceTag", card.transform, priceSprite,
            new Vector2(0f, -88f), new Vector2(165f, 58f)
        );
        TMP_Text priceText = Text(
            "Price", price.transform, font, "0", 25f,
            new Vector2(18f, -2f), new Vector2(100f, 42f),
            new Color(0.75f, 0.08f, 0.04f)
        );
        TMP_Text state = Text(
            "State", card.transform, font, string.Empty, 34f,
            Vector2.zero, new Vector2(250f, 80f),
            new Color(1f, 0.3f, 0.22f)
        );
        return new ShopProductView
        {
            selectButton = button,
            icon = icon,
            nameText = name,
            priceText = priceText,
            stateText = state
        };
    }

    private static void AddInventoryToPlayer()
    {
        Scene scene = EditorSceneManager.OpenScene(
            "Assets/Scenes/Persistent.unity", OpenSceneMode.Single
        );
        GameObject player = scene.GetRootGameObjects()
            .First(root => root.name == "Player");
        if (player.GetComponent<PlayerInventory>() == null)
            player.AddComponent<PlayerInventory>();
        EditorSceneManager.SaveScene(scene);
    }

    private static void AddShopToGameScene(
        CollectibleDefinition potion,
        CollectibleDefinition coin,
        CollectibleDefinition wolf
    )
    {
        Scene scene = EditorSceneManager.OpenScene(
            "Assets/Scenes/GameScene.unity", OpenSceneMode.Single
        );
        GameObject shop = scene.GetRootGameObjects()
            .FirstOrDefault(root => root.name == "TestShop");
        if (shop == null)
        {
            shop = new GameObject("TestShop");
            shop.transform.position = new Vector3(0.5f, 2.5f, 0f);
            GridObject grid = shop.AddComponent<GridObject>();
            SerializedObject gridData = new(grid);
            gridData.FindProperty("occupantType").enumValueIndex =
                (int)GridOccupantType.Interactable;
            gridData.ApplyModifiedPropertiesWithoutUndo();
            shop.AddComponent<FogVisibilityTarget>();
            shop.AddComponent<ShopInteractable>();
            SpriteRenderer renderer = shop.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite(ShopIconPath);
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = 5;
            renderer.transform.localScale = Vector3.one * 0.7f;
        }

        ShopInteractable interactable = shop.GetComponent<ShopInteractable>();
        SerializedObject serialized = new(interactable);
        SerializedProperty products = serialized.FindProperty("products");
        products.arraySize = 3;
        SetProduct(products.GetArrayElementAtIndex(0), potion, 10);
        SetProduct(products.GetArrayElementAtIndex(1), coin, 14);
        SetProduct(products.GetArrayElementAtIndex(2), wolf, 20);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetProduct(
        SerializedProperty product,
        CollectibleDefinition definition,
        int price
    )
    {
        product.FindPropertyRelative("collectible").objectReferenceValue =
            definition;
        product.FindPropertyRelative("price").intValue = price;
    }

    private static Sprite LoadSprite(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();

    private static void EnsureFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static GameObject UIObject(string name, Transform parent)
    {
        GameObject go = new(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Image ImageObject(
        string name, Transform parent, Sprite sprite,
        Vector2 position, Vector2 size
    )
    {
        GameObject go = UIObject(name, parent);
        Center(go.GetComponent<RectTransform>(), position, size);
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        return image;
    }

    private static TMP_Text Text(
        string name, Transform parent, TMP_FontAsset font, string value,
        float fontSize, Vector2 position, Vector2 size, Color color
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
        text.raycastTarget = false;
        return text;
    }

    private static Button ButtonObject(
        string name, Transform parent, TMP_FontAsset font, string label,
        Vector2 position, Vector2 size, Sprite sprite, out TMP_Text labelText
    )
    {
        Image image = ImageObject(name, parent, sprite, position, size);
        image.type = Image.Type.Sliced;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        labelText = Text(
            "Label", image.transform, font, label, 28f,
            Vector2.zero, size - new Vector2(20f, 20f), Color.black
        );
        return button;
    }

    private static void Center(
        RectTransform rect, Vector2 position, Vector2 size
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

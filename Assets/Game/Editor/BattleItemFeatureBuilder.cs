#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BattleItemFeatureBuilder
{
    private const string CollectibleFolder =
        "Assets/Game/Data/Collectibles";
    private const string BattlePanelPath =
        "Assets/Resources/UI/BattleActionPanel.prefab";
    private const string HudPanelPath =
        "Assets/Resources/UI/GameHUDPanel.prefab";
    private const string ShopPath =
        "Assets/Game/Data/Shops/DefaultShopInventory.asset";
    private const string OfferingPath =
        "Assets/Game/Data/Offerings/DefaultOffering.asset";
    private const string FontPath =
        "Assets/Arts/Font/fusion-pixel-12px-proportional-zh_hans SDF D.asset";
    private const string BattleFramePath =
        "Assets/Arts/UI/战斗UI-占位符/战斗框架.png";
    private const string StatusFramePath =
        "Assets/Arts/UI/战斗UI-占位符/战斗框架_无指向按钮版.png";
    private const string DescriptionFramePath =
        "Assets/Arts/UI/战斗UI-占位符/介绍框.png";
    private const string BackFramePath =
        "Assets/Arts/UI/战斗UI-占位符/返回键.png";
    private const string AttackIconPath =
        "Assets/Arts/UI/图标/普攻.png";

    [MenuItem("Tools/Zero/Build Battle Item Feature")]
    public static void Build()
    {
        CollectibleDefinition wrench = UpsertCollectible(
            "Wrench",
            "wrench",
            "扳手",
            "接下来 3 次玩家行动中，基础攻击力 +2。可叠加。",
            "Assets/Arts/UI/icon/扳手.png",
            CollectibleKind.Item,
            3,
            1,
            CollectibleEffectType.TimedAttackBonus,
            2f,
            3
        );
        CollectibleDefinition heart = UpsertCollectible(
            "GirlsThoughts",
            "girls_thoughts",
            "少女的心事",
            "替主角承受下一次怪物攻击。不可叠加。",
            "Assets/Arts/UI/icon/少女的心事.png",
            CollectibleKind.Item,
            3,
            2,
            CollectibleEffectType.NegateNextAttack,
            1f,
            0
        );
        CollectibleDefinition shield = UpsertCollectible(
            "GuardianShield",
            "guardian_shield",
            "守护者之盾",
            "为下一个敌人阶段提供 6 点护盾。不可叠加。",
            "Assets/Arts/UI/icon/守护者之盾.png",
            CollectibleKind.Item,
            3,
            3,
            CollectibleEffectType.NextEnemyPhaseShield,
            6f,
            0
        );
        CollectibleDefinition potion = UpsertCollectible(
            "MagicPotion",
            "magic_potion",
            "魔药",
            "立即恢复 6 点数字，不超过数字上限。",
            "Assets/Arts/UI/icon/魔药.png",
            CollectibleKind.Item,
            3,
            4,
            CollectibleEffectType.RestoreNumber,
            6f,
            0
        );

        CollectibleDefinition lucky = UpdateExistingRelic(
            "LuckyCoin",
            "Assets/Arts/UI/icon/幸运金币.png",
            1
        );
        CollectibleDefinition wolf = UpdateExistingRelic(
            "HungryWolf",
            "Assets/Arts/UI/icon/饿狼.png",
            2
        );

        CollectibleDefinition[] items = { wrench, heart, shield, potion };
        UpdateShop(items, lucky, wolf);
        UpdateOffering(items);
        CreateBattlePanelPrefab();
        ConfigureHudPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "Battle items, shop/offering data, battle menu and HUD were updated."
        );
    }

    private static CollectibleDefinition UpsertCollectible(
        string assetName,
        string id,
        string displayName,
        string description,
        string iconPath,
        CollectibleKind kind,
        int maximumStacks,
        int inventoryOrder,
        CollectibleEffectType effectType,
        float effectValue,
        int effectDuration
    )
    {
        string path = $"{CollectibleFolder}/{assetName}.asset";
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
        serialized.FindProperty("icon").objectReferenceValue =
            LoadSprite(iconPath);
        serialized.FindProperty("kind").enumValueIndex = (int)kind;
        serialized.FindProperty("maximumStacks").intValue = maximumStacks;
        serialized.FindProperty("inventoryOrder").intValue = inventoryOrder;
        serialized.FindProperty("effectType").enumValueIndex = (int)effectType;
        serialized.FindProperty("effectValue").floatValue = effectValue;
        serialized.FindProperty("effectDuration").intValue = effectDuration;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static CollectibleDefinition UpdateExistingRelic(
        string assetName,
        string iconPath,
        int order
    )
    {
        CollectibleDefinition definition =
            AssetDatabase.LoadAssetAtPath<CollectibleDefinition>(
                $"{CollectibleFolder}/{assetName}.asset"
            );
        if (definition == null)
        {
            return null;
        }

        SerializedObject serialized = new(definition);
        serialized.FindProperty("icon").objectReferenceValue =
            LoadSprite(iconPath);
        serialized.FindProperty("inventoryOrder").intValue = order;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static void UpdateShop(
        IReadOnlyList<CollectibleDefinition> items,
        CollectibleDefinition lucky,
        CollectibleDefinition wolf
    )
    {
        ShopInventoryDefinition shop =
            AssetDatabase.LoadAssetAtPath<ShopInventoryDefinition>(ShopPath);
        if (shop == null)
        {
            Debug.LogWarning("Default shop was not found; item assets were kept.");
            return;
        }

        List<(CollectibleDefinition collectible, int price)> products = new()
        {
            (items[0], 3),
            (items[1], 5),
            (items[2], 3),
            (items[3], 4)
        };
        if (lucky != null)
        {
            products.Add((lucky, 14));
        }
        if (wolf != null)
        {
            products.Add((wolf, 20));
        }

        SerializedObject serialized = new(shop);
        SerializedProperty productArray = serialized.FindProperty("products");
        productArray.arraySize = products.Count;
        for (int i = 0; i < products.Count; i++)
        {
            SerializedProperty product =
                productArray.GetArrayElementAtIndex(i);
            product.FindPropertyRelative("collectible").objectReferenceValue =
                products[i].collectible;
            product.FindPropertyRelative("price").intValue = products[i].price;
            product.FindPropertyRelative("stock").intValue = 1;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(shop);
    }

    private static void UpdateOffering(
        IReadOnlyList<CollectibleDefinition> items
    )
    {
        OfferingDefinition offering =
            AssetDatabase.LoadAssetAtPath<OfferingDefinition>(OfferingPath);
        if (offering == null)
        {
            return;
        }

        SerializedObject serialized = new(offering);
        SerializedProperty pool = serialized.FindProperty("itemPool");
        pool.arraySize = items.Count;
        for (int i = 0; i < items.Count; i++)
        {
            SerializedProperty entry = pool.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("collectible").objectReferenceValue =
                items[i];
            entry.FindPropertyRelative("weight").intValue = 1;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(offering);
    }

    private static void CreateBattlePanelPrefab()
    {
        TMP_FontAsset font =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Sprite battleFrame = LoadSprite(BattleFramePath);
        Sprite descriptionFrame = LoadSprite(DescriptionFramePath);
        Sprite backFrame = LoadSprite(BackFramePath);

        GameObject root = UIObject("BattleActionPanel", null);
        Stretch(root.GetComponent<RectTransform>());
        BattleActionPanel panel = root.AddComponent<BattleActionPanel>();
        panel.layer = UILayer.Popup;

        TMP_Text preview = Text(
            "PreviewText",
            root.transform,
            font,
            "普通攻击  消耗 1  伤害 3",
            25f,
            new Vector2(510f, 365f),
            new Vector2(650f, 80f),
            TextAlignmentOptions.Center
        );
        TMP_Text feedback = Text(
            "FeedbackText",
            root.transform,
            font,
            string.Empty,
            22f,
            new Vector2(510f, 315f),
            new Vector2(650f, 45f),
            TextAlignmentOptions.Center
        );
        feedback.color = new Color(1f, 0.65f, 0.35f);

        GameObject primaryMenu = UIObject("PrimaryMenu", root.transform);
        SetRect(
            primaryMenu.GetComponent<RectTransform>(),
            new Vector2(760f, -40f),
            new Vector2(350f, 600f)
        );

        CreateActionButton(
            "AttackButton",
            primaryMenu.transform,
            font,
            battleFrame,
            LoadSprite(AttackIconPath),
            "普攻",
            new Vector2(0f, 210f),
            out Button attackButton,
            out TMP_Text attackLabel,
            out _,
            out _
        );
        for (int i = 0; i < 3; i++)
        {
            CreateActionButton(
                $"Skill{i + 1}Button",
                primaryMenu.transform,
                font,
                battleFrame,
                LoadSprite($"Assets/Arts/UI/图标/技能{i + 1}.png"),
                $"技能 {i + 1}",
                new Vector2(0f, 105f - i * 105f),
                out Button skill,
                out _,
                out _,
                out _
            );
            skill.gameObject.SetActive(false);
        }
        CreateActionButton(
            "ItemButton",
            primaryMenu.transform,
            font,
            battleFrame,
            LoadSprite("Assets/Arts/UI/icon/魔药.png"),
            "道具",
            new Vector2(0f, -210f),
            out Button itemButton,
            out TMP_Text itemLabel,
            out _,
            out _
        );

        GameObject statusRoot = UIObject(
            "BattleStatusRoot",
            primaryMenu.transform
        );
        SetRect(
            statusRoot.GetComponent<RectTransform>(),
            new Vector2(-350f, 0f),
            new Vector2(300f, 280f)
        );
        Sprite statusFrame = LoadSprite(StatusFramePath);
        CreateStatusCard(
            "WrenchStatus",
            statusRoot.transform,
            font,
            statusFrame,
            LoadSprite("Assets/Arts/UI/icon/扳手.png"),
            "攻击 +2\n剩余 3 次",
            new Vector2(0f, 90f),
            out GameObject wrenchStatus,
            out TMP_Text wrenchStatusText
        );
        CreateStatusCard(
            "ShieldStatus",
            statusRoot.transform,
            font,
            statusFrame,
            LoadSprite("Assets/Arts/UI/icon/守护者之盾.png"),
            "护盾 6\n下个敌人阶段",
            Vector2.zero,
            out GameObject shieldStatus,
            out TMP_Text shieldStatusText
        );
        CreateStatusCard(
            "HeartStatus",
            statusRoot.transform,
            font,
            statusFrame,
            LoadSprite("Assets/Arts/UI/icon/少女的心事.png"),
            "替伤 x1\n下一次怪物攻击",
            new Vector2(0f, -90f),
            out GameObject heartStatus,
            out TMP_Text heartStatusText
        );
        statusRoot.SetActive(false);

        GameObject itemMenu = UIObject("ItemMenu", root.transform);
        Stretch(itemMenu.GetComponent<RectTransform>());

        Image descriptionFrameImage = ImageObject(
            "DescriptionFrame",
            itemMenu.transform,
            descriptionFrame,
            new Vector2(390f, -30f),
            new Vector2(300f, 330f)
        );
        TMP_Text itemName = Text(
            "ItemNameText",
            descriptionFrameImage.transform,
            font,
            "道具名称",
            28f,
            new Vector2(0f, 105f),
            new Vector2(230f, 55f),
            TextAlignmentOptions.Center
        );
        TMP_Text itemDescription = Text(
            "ItemDescriptionText",
            descriptionFrameImage.transform,
            font,
            "道具说明",
            21f,
            new Vector2(0f, -25f),
            new Vector2(225f, 185f),
            TextAlignmentOptions.TopLeft
        );
        itemDescription.enableWordWrapping = true;

        Button[] slotButtons = new Button[4];
        Image[] slotIcons = new Image[4];
        TMP_Text[] slotLabels = new TMP_Text[4];
        TMP_Text[] slotCounts = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            CreateActionButton(
                $"ItemSlot{i + 1}",
                itemMenu.transform,
                font,
                battleFrame,
                null,
                "空",
                new Vector2(760f, 210f - i * 105f),
                out slotButtons[i],
                out slotLabels[i],
                out slotIcons[i],
                out slotCounts[i]
            );
            slotButtons[i].gameObject.AddComponent<BattleItemSlotSelection>();
        }

        Button backButton = ImageButton(
            "BackButton",
            itemMenu.transform,
            backFrame,
            new Vector2(760f, -285f),
            new Vector2(190f, 72f)
        );
        Text(
            "Label",
            backButton.transform,
            font,
            "返回",
            26f,
            Vector2.zero,
            new Vector2(150f, 50f),
            TextAlignmentOptions.Center
        );
        itemMenu.SetActive(false);

        SerializedObject serialized = new(panel);
        SetReference(serialized, "previewText", preview);
        SetReference(serialized, "feedbackText", feedback);
        SetReference(serialized, "primaryMenu", primaryMenu);
        SetReference(serialized, "attackButton", attackButton);
        SetReference(serialized, "attackButtonLabel", attackLabel);
        SetReference(serialized, "itemButton", itemButton);
        SetReference(serialized, "itemButtonLabel", itemLabel);
        SetReference(serialized, "statusRoot", statusRoot);
        SetReference(serialized, "wrenchStatus", wrenchStatus);
        SetReference(serialized, "wrenchStatusText", wrenchStatusText);
        SetReference(serialized, "shieldStatus", shieldStatus);
        SetReference(serialized, "shieldStatusText", shieldStatusText);
        SetReference(serialized, "heartStatus", heartStatus);
        SetReference(serialized, "heartStatusText", heartStatusText);
        SetReference(serialized, "itemMenu", itemMenu);
        SetArray(serialized, "itemSlotButtons", slotButtons);
        SetArray(serialized, "itemSlotIcons", slotIcons);
        SetArray(serialized, "itemSlotLabels", slotLabels);
        SetArray(serialized, "itemSlotCounts", slotCounts);
        SetReference(serialized, "itemNameText", itemName);
        SetReference(serialized, "itemDescriptionText", itemDescription);
        SetReference(serialized, "backButton", backButton);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, BattlePanelPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void ConfigureHudPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(HudPanelPath);
        try
        {
            GameHUDPanel panel = root.GetComponent<GameHUDPanel>();
            Transform itemArea = FindDeep(root.transform, "ItemArea");
            Transform relicArea = FindDeep(root.transform, "RelicArea");
            if (panel == null || itemArea == null || relicArea == null)
            {
                Debug.LogWarning("HUD item/relic areas were not found.");
                return;
            }

            ClearChildren(itemArea);
            ClearChildren(relicArea);
            TMP_FontAsset font =
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Sprite frame = AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd"
            );

            Image[] itemIcons = new Image[4];
            TMP_Text[] itemCounts = new TMP_Text[4];
            for (int i = 0; i < itemIcons.Length; i++)
            {
                CreateHudSlot(
                    $"ItemSlot{i + 1}",
                    itemArea,
                    font,
                    frame,
                    i,
                    out itemIcons[i],
                    out itemCounts[i]
                );
            }

            Image[] relicIcons = new Image[3];
            TMP_Text[] relicCounts = new TMP_Text[3];
            for (int i = 0; i < relicIcons.Length; i++)
            {
                CreateHudSlot(
                    $"RelicSlot{i + 1}",
                    relicArea,
                    font,
                    frame,
                    i,
                    out relicIcons[i],
                    out relicCounts[i]
                );
            }

            SerializedObject serialized = new(panel);
            SetArray(serialized, "itemIcons", itemIcons);
            SetArray(serialized, "itemCounts", itemCounts);
            SetArray(serialized, "relicIcons", relicIcons);
            SetArray(serialized, "relicCounts", relicCounts);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, HudPanelPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void CreateHudSlot(
        string name,
        Transform parent,
        TMP_FontAsset font,
        Sprite frame,
        int index,
        out Image icon,
        out TMP_Text count
    )
    {
        Image background = ImageObject(
            name,
            parent,
            frame,
            new Vector2(40f + index * 78f, 0f),
            new Vector2(70f, 82f)
        );
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = backgroundRect.anchorMax =
            new Vector2(0f, 0.5f);
        background.type = Image.Type.Sliced;
        background.color = new Color(0.08f, 0.07f, 0.1f, 0.82f);
        icon = ImageObject(
            "Icon",
            background.transform,
            null,
            Vector2.zero,
            new Vector2(60f, 68f)
        );
        icon.preserveAspect = true;
        icon.enabled = false;
        count = Text(
            "Count",
            background.transform,
            font,
            string.Empty,
            19f,
            new Vector2(20f, -24f),
            new Vector2(48f, 28f),
            TextAlignmentOptions.BottomRight
        );
    }

    private static void CreateActionButton(
        string name,
        Transform parent,
        TMP_FontAsset font,
        Sprite frame,
        Sprite iconSprite,
        string label,
        Vector2 position,
        out Button button,
        out TMP_Text labelText,
        out Image icon,
        out TMP_Text count
    )
    {
        button = ImageButton(
            name,
            parent,
            frame,
            position,
            new Vector2(320f, 92f)
        );
        icon = ImageObject(
            "Icon",
            button.transform,
            iconSprite,
            new Vector2(-119f, 0f),
            new Vector2(76f, 76f)
        );
        icon.preserveAspect = true;
        icon.enabled = iconSprite != null;
        labelText = Text(
            "Label",
            button.transform,
            font,
            label,
            27f,
            new Vector2(28f, 0f),
            new Vector2(190f, 55f),
            TextAlignmentOptions.Center
        );
        count = Text(
            "Count",
            button.transform,
            font,
            string.Empty,
            19f,
            new Vector2(105f, -24f),
            new Vector2(60f, 28f),
            TextAlignmentOptions.BottomRight
        );
    }

    private static void CreateStatusCard(
        string name,
        Transform parent,
        TMP_FontAsset font,
        Sprite frame,
        Sprite iconSprite,
        string value,
        Vector2 position,
        out GameObject card,
        out TMP_Text statusText
    )
    {
        Image background = ImageObject(
            name,
            parent,
            frame,
            position,
            new Vector2(300f, 82f)
        );
        background.preserveAspect = false;
        card = background.gameObject;

        Image icon = ImageObject(
            "Icon",
            card.transform,
            iconSprite,
            new Vector2(-111f, 0f),
            new Vector2(68f, 68f)
        );
        icon.preserveAspect = true;

        statusText = Text(
            "StatusText",
            card.transform,
            font,
            value,
            20f,
            new Vector2(31f, 0f),
            new Vector2(205f, 64f),
            TextAlignmentOptions.MidlineLeft
        );
    }

    private static GameObject UIObject(string name, Transform parent)
    {
        GameObject result = new(name, typeof(RectTransform));
        result.layer = LayerMask.NameToLayer("UI");
        result.transform.SetParent(parent, false);
        return result;
    }

    private static Image ImageObject(
        string name,
        Transform parent,
        Sprite sprite,
        Vector2 position,
        Vector2 size
    )
    {
        GameObject result = UIObject(name, parent);
        SetRect(result.GetComponent<RectTransform>(), position, size);
        Image image = result.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = sprite != null;
        return image;
    }

    private static Button ImageButton(
        string name,
        Transform parent,
        Sprite sprite,
        Vector2 position,
        Vector2 size
    )
    {
        Image image = ImageObject(name, parent, sprite, position, size);
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static TMP_Text Text(
        string name,
        Transform parent,
        TMP_FontAsset font,
        string value,
        float size,
        Vector2 position,
        Vector2 dimensions,
        TextAlignmentOptions alignment
    )
    {
        GameObject result = UIObject(name, parent);
        SetRect(result.GetComponent<RectTransform>(), position, dimensions);
        TMP_Text text = result.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.text = value;
        text.color = Color.white;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 position,
        Vector2 size
    )
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Sprite LoadSprite(string path) =>
        AssetDatabase.LoadAssetAtPath<Sprite>(path);

    private static void SetReference(
        SerializedObject serialized,
        string propertyName,
        UnityEngine.Object value
    )
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetArray<T>(
        SerializedObject serialized,
        string propertyName,
        T[] values
    ) where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }
        foreach (Transform child in root)
        {
            Transform found = FindDeep(child, name);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }
}
#endif

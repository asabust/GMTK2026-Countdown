using System.Collections.Generic;
using Game.Runtime.Data;
using UnityEditor;
using UnityEngine;

public static class AudioInfoListSynchronizer
{
    private const string AssetPath = "Assets/GameData/AudioInfoListSO.asset";

    private static readonly Dictionary<AudioName, string> ClipPaths = new()
    {
        [AudioName.BgmTitle] = "Bgm/Menu_Misc.ogg",
        [AudioName.BgmGameplay] = "Bgm/inGame.ogg",
        [AudioName.UiClick] = "ui/Menu_UI_Click.ogg",
        [AudioName.UiClose] = "ui/Menu_UI_Close.ogg",
        [AudioName.UiSlider] = "Sfx/Slider_FX.ogg",
        [AudioName.SfxPlayerAttack] = "Sfx/Plyr_Attack.ogg",
        [AudioName.SfxPlayerBloodthirst] = "Sfx/Plyr_Skill.ogg",
        [AudioName.SfxPlayerVengeance] = "Sfx/Plyr_Skill.ogg",
        [AudioName.SfxPlayerParasite] = "Sfx/Plyr_Skill.ogg",
        [AudioName.SfxPlayerHit] = "Sfx/Plyr_Hit.ogg",
        [AudioName.SfxPlayerDefendHit] = "Sfx/Plyr_Defence_Hit.ogg",
        [AudioName.SfxItemWrench] = "Sfx/Plyr_Skill.ogg",
        [AudioName.SfxItemMaiden] = "Sfx/Plyr_Skill.ogg",
        [AudioName.SfxItemShield] = "Sfx/Plyr_Skill.ogg",
        [AudioName.SfxItemPotion] = "Sfx/Plyr_Skill.ogg",
        [AudioName.SfxPlayerFootstep] = "Sfx/Plyr_Footstep.ogg",
        [AudioName.SfxPlayerDeath] = "Sfx/Plyr_Death.ogg",
        [AudioName.SfxChickenAttack] = "Sfx/Chicken_Attack.ogg",
        [AudioName.SfxChickenHit] = "Sfx/Chicken_Hit.ogg",
        [AudioName.SfxOilAttack] = "Sfx/fty_Attack.ogg",
        [AudioName.SfxOilHit] = "Sfx/fty_Hit.ogg",
        [AudioName.SfxBoxHit] = "Sfx/Box_Hit.ogg",
        [AudioName.SfxBoxExplode] = "Sfx/Box_Expo.ogg",
        [AudioName.SfxHamsterAttack] = "Sfx/Hamster_Attack.ogg",
        [AudioName.SfxHamsterHit] = "Sfx/Hamster_Hit.ogg",
        [AudioName.SfxBossP1Attack] = "Sfx/Clock_Attack_Lv1.ogg",
        [AudioName.SfxBossP1Charge] = "Sfx/Clock_Charge_Lv1.ogg",
        [AudioName.SfxBossP1Hit] = "Sfx/Clock_Hit_Lv1.ogg",
        [AudioName.SfxBossP2Attack] = "Sfx/Clock_Attack_Lv2.ogg",
        [AudioName.SfxBossP2StealSuccess] =
            "Sfx/Clock_Stolen_Sucess.ogg",
        [AudioName.SfxBossP2StealFail] = "Sfx/Clock_Stolen_Fail.ogg",
        [AudioName.SfxBossP2Hit] = "Sfx/Clock_Hit_Lv2.ogg",
        [AudioName.UiGetItem] = "Sfx/Get.ogg",
        [AudioName.UiGetCollectible] = "Sfx/Get.ogg",
        [AudioName.UiGetSkill] = "Sfx/Get.ogg",
        [AudioName.UiInteractRest] = "ui/Menu_UI_Click.ogg",
        [AudioName.UiInteractShop] = "ui/Menu_UI_Click.ogg",
        [AudioName.UiBuy] = "Sfx/Buy.ogg",
        [AudioName.UiSoldOut] = "Sfx/Intr_Fail.ogg",
        [AudioName.UiInteractOffering] = "Sfx/Intr_Offer.ogg",
        [AudioName.UiOfferingSuccess] = "Sfx/Intr_Sucess.ogg",
        [AudioName.UiOfferingFail] = "Sfx/Intr_Fail.ogg",
        [AudioName.UiOfferingRefund] = "Sfx/Intr_Fail.ogg",
        [AudioName.UiNotEnough] = "Sfx/Intr_Fail.ogg",
        [AudioName.UiGreedFail] = "Sfx/Intr_Fail.ogg",
        [AudioName.UiGreedSuccess] = "Sfx/Intr_Sucess.ogg",
    };

    [InitializeOnLoadMethod]
    private static void ScheduleSync()
    {
        EditorApplication.delayCall += Sync;
    }

    [MenuItem("Tools/Zero/Synchronize Audio Info List")]
    public static void Sync()
    {
        AudioInfoListSO asset =
            AssetDatabase.LoadAssetAtPath<AudioInfoListSO>(AssetPath);
        if (asset == null)
        {
            Debug.LogError($"Audio list asset not found: {AssetPath}");
            return;
        }

        List<AudioInf> entries = new();
        foreach (AudioName name in System.Enum.GetValues(typeof(AudioName)))
        {
            if (name == AudioName.None)
            {
                continue;
            }

            ClipPaths.TryGetValue(name, out string relativePath);
            AudioClip clip = string.IsNullOrEmpty(relativePath)
                ? null
                : AssetDatabase.LoadAssetAtPath<AudioClip>(
                    $"Assets/Arts/CountDownFX/{relativePath}"
                );
            entries.Add(new AudioInf
            {
                audioName = name,
                clip = clip,
                volume = 1f,
                loop = name == AudioName.BgmTitle ||
                       name == AudioName.BgmGameplay ||
                       name == AudioName.BgmBoss
            });
        }

        asset.audioInfos = entries;
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }
}

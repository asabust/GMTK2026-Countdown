using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime.Data
{
    [CreateAssetMenu(fileName = "AudioInfoListSO", menuName = "Audio Info List")]
    public class AudioInfoListSO : ScriptableObject
    {
        public List<AudioInf> audioInfos = new List<AudioInf>();

        public AudioInf GetAudioInfo(AudioName audioName)
        {
            return audioInfos?.Find(
                x => x != null && x.audioName == audioName
            );
        }
    }


    [System.Serializable]
    public class AudioInf
    {
        public AudioName audioName;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume;
        public bool loop;
    }

    public enum AudioName
    {
        None,
        BgmTitle,
        BgmGameplay,
        BgmBoss,
        UiClick,
        UiClose,
        UiSlider,
        SfxPlayerAttack,
        SfxPlayerBloodthirst,
        SfxPlayerVengeance,
        SfxPlayerParasite,
        SfxPlayerHit,
        SfxPlayerDefendHit,
        SfxItemWrench,
        SfxItemMaiden,
        SfxItemShield,
        SfxItemPotion,
        SfxPlayerFootstep,
        SfxPlayerDeath,
        UiDeathPanel,
        SfxChickenAttack,
        SfxChickenHit,
        SfxOilDrink,
        SfxOilAttack,
        SfxOilHit,
        SfxBoxHit,
        SfxBoxExplode,
        SfxHamsterAttack,
        SfxHamsterHit,
        SfxBossP1Attack,
        SfxBossP1Charge,
        SfxBossP1Hit,
        SfxBossP2Attack,
        SfxBossP2StealSuccess,
        SfxBossP2StealFail,
        SfxBossP2Hit,
        UiGetItem,
        UiGetCollectible,
        UiGetSkill,
        UiInteractRest,
        UiInteractShop,
        UiBuy,
        UiSoldOut,
        UiInteractOffering,
        UiOfferingSuccess,
        UiOfferingFail,
        UiOfferingRefund,
        UiNotEnough,
        UiGreedFail,
        UiGreedSuccess,
    }
}

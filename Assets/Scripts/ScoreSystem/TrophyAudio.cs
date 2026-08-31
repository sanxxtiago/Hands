using UnityEngine;

public class TrophyAudio : MonoBehaviour
{
    private void OnEnable()
    {
        TrophyView.OnTrophyLanded += HandleTrophyLanded;
    }

    private void OnDisable()
    {
        TrophyView.OnTrophyLanded -= HandleTrophyLanded;
    }

    private void HandleTrophyLanded(TrophyTier tier)
    {
        if (tier == TrophyTier.None)
            return;

        AudioType audioType = MapTierToAudioType(tier);
        AudioManager.Play(audioType);
    }

    private static AudioType MapTierToAudioType(TrophyTier tier)
    {
        return tier switch
        {
            TrophyTier.Gold => AudioType.TrophyGold,
            TrophyTier.Silver => AudioType.TrophySilver,
            TrophyTier.Bronze => AudioType.TrophyBronze,
            _ => AudioType.TrophyBronze
        };
    }
}

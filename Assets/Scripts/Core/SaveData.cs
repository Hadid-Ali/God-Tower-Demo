using UnityEngine;

namespace GodTower.Core
{
    /// <summary>Small PlayerPrefs wrapper for level completion marks and player settings.</summary>
    public static class SaveData
    {
        const string CompletedKey = "gt.level.completed.";
        const string MasterVolumeKey = "gt.settings.masterVolume";
        const string SfxVolumeKey = "gt.settings.sfxVolume";
        const string ReducedShakeKey = "gt.settings.reducedShake";

        public static bool IsCompleted(int levelIndex) => PlayerPrefs.GetInt(CompletedKey + levelIndex, 0) == 1;

        public static void MarkCompleted(int levelIndex)
        {
            PlayerPrefs.SetInt(CompletedKey + levelIndex, 1);
            PlayerPrefs.Save();
        }

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            set { PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f);
            set { PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        public static bool ReducedShake
        {
            get => PlayerPrefs.GetInt(ReducedShakeKey, 0) == 1;
            set { PlayerPrefs.SetInt(ReducedShakeKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }
    }
}

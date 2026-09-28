using System;
using System.Collections.Generic;
using GodTower.Effects;
using UnityEngine;

namespace GodTower.Level
{
    /// <summary>A progress-triggered event. Each encounter fires once, the first time the climber reaches its occurrence.</summary>
    [Serializable]
    public struct Encounter
    {
        [Range(0f, 100f), Tooltip("Where in the level it fires, as a percentage of the goal height (the same scale as the progress bar). " +
                                  "Must be above the level's start point.")]
        public float occurrence;
        public EffectType effect;
        [Range(0f, 100f), Tooltip("How far it pushes the climber down, as a percentage of the goal height. " +
                                  "Only for effects that push (e.g. the Dragon-style push-down); 0 = the effect's own setting.")]
        public float pushPercent;

        public Encounter(float occurrence, EffectType effect, float pushPercent = 0f)
        {
            this.occurrence = occurrence;
            this.effect = effect;
            this.pushPercent = pushPercent;
        }

        /// <summary>The occurrence converted to display units for a level with this goal height.</summary>
        public float HeightUnits(float goalHeight) => goalHeight * occurrence / 100f;
    }

    /// <summary>What makes each level distinct: goal height, pacing, hazard strength and encounters.</summary>
    [CreateAssetMenu(menuName = "God Tower/Level Config", fileName = "Level")]
    public sealed class LevelConfig : ScriptableObject
    {
        [Header("Identity")]
        public int number = 1;
        public string displayName = "Tower Base";

        [Header("Progress")]
        [Tooltip("Summit height in display units.")]
        public float goalHeight = 1000f;
        [Tooltip("Display units per world meter.")]
        public float unitsPerMeter = 20f;
        [Tooltip("Share of the goal height where the climber starts; the tower below it is scenery. This start point is the climber's floor.")]
        [Range(0f, 0.9f)] public float startProgress = 0.15f;
        [Tooltip("Height above the start (display units) within which a fall is harmless; beyond it, falling back below the start loses.")]
        public float safeZoneHeight = 60f;

        [Header("Climbing")]
        [Tooltip("Hand-over-hand climb speed in meters per second.")]
        public float climbSpeed = 1.8f;

        [Header("Hazard tuning (display units)")]
        public float gloveKnockback = 150f;
        public float axeKnockback = 90f;
        [Tooltip("Initial downward speed (m/s) when an explosion knocks the climber off the tower.")]
        public float explosionFallSpeed = 5f;

        [Header("Encounters (ascending height)")]
        public List<Encounter> encounters = new List<Encounter>();

        public float GoalMeters => goalHeight / unitsPerMeter;
        public float StartUnits => goalHeight * Mathf.Clamp(startProgress, 0f, 0.9f);
        public float StartMeters => StartUnits / unitsPerMeter;
        public float SafeZoneMeters => safeZoneHeight / unitsPerMeter;
        public float ToMeters(float units) => units / unitsPerMeter;
        public float ToUnits(float meters) => meters * unitsPerMeter;

        /// <summary>Returns human-readable problems; empty when the level is playable.</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            if (goalHeight <= 0f) problems.Add("goalHeight must be positive");
            if (unitsPerMeter <= 0f) problems.Add("unitsPerMeter must be positive");
            if (climbSpeed <= 0f) problems.Add("climbSpeed must be positive");
            if (startProgress < 0f || startProgress > 0.9f) problems.Add("startProgress must be within [0, 0.9]");
            if (safeZoneHeight < 0f || StartUnits + safeZoneHeight >= goalHeight) problems.Add("start + safeZoneHeight must stay below the goal");

            float previous = float.MinValue;
            for (int i = 0; i < encounters.Count; i++)
            {
                float percent = encounters[i].occurrence;
                if (percent <= startProgress * 100f || percent >= 100f)
                    problems.Add($"encounter {i} at {percent}% is outside (start {startProgress * 100f}%, 100%) and will never fire");
                if (percent < previous) problems.Add($"encounter {i} is not in ascending order");
                previous = percent;
            }
            return problems;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            encounters.Sort((a, b) => a.occurrence.CompareTo(b.occurrence));
        }
#endif
    }
}

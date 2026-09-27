using System;
using System.Collections.Generic;
using GodTower.Effects;

namespace GodTower.Level
{
    /// <summary>
    /// Fires each encounter once, the first time the climber's height reaches it. Losing height
    /// (knockback) never re-fires an encounter, so every level is deterministic and finishable.
    /// </summary>
    public sealed class EncounterRunner
    {
        readonly List<Encounter> _encounters;
        int _next;

        public EncounterRunner(IEnumerable<Encounter> encounters)
        {
            _encounters = new List<Encounter>(encounters);
            _encounters.Sort((a, b) => a.height.CompareTo(b.height));
        }

        public int Remaining => _encounters.Count - _next;

        /// <summary>Drops encounters at or below <paramref name="heightUnits"/> without firing them (e.g. below the start point).</summary>
        public void SkipUpTo(float heightUnits)
        {
            while (_next < _encounters.Count && _encounters[_next].height <= heightUnits) _next++;
        }

        /// <summary>Triggers every encounter at or below <paramref name="heightUnits"/> that has not fired yet.</summary>
        public void Tick(float heightUnits, Action<EffectType> trigger)
        {
            while (_next < _encounters.Count && heightUnits >= _encounters[_next].height)
            {
                trigger(_encounters[_next].effect);
                _next++;
            }
        }
    }
}

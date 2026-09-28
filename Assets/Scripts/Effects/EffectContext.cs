using GodTower.Cameras;
using GodTower.Level;
using GodTower.Player;
using UnityEngine;

namespace GodTower.Effects
{
    /// <summary>Shared references handed to every effect when a level starts.</summary>
    public sealed class EffectContext
    {
        public ClimberController Climber { get; }
        public ClimberView View { get; }
        public CameraRig Rig { get; }
        public LevelConfig Config { get; }

        public EffectContext(ClimberController climber, ClimberView view, CameraRig rig, LevelConfig config)
        {
            Climber = climber;
            View = view;
            Rig = rig;
            Config = config;
        }
    }
}

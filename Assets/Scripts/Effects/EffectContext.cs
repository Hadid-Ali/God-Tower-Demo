using GodTower.Cameras;
using GodTower.Level;
using GodTower.Player;
using UnityEngine;

namespace GodTower.Effects
{
    /// <summary>Full-screen feedback the effects can request from the HUD.</summary>
    public interface IScreenFeedback
    {
        void Flash(Color color, float alpha, float duration);
    }

    /// <summary>Shared references handed to every effect when a level starts.</summary>
    public sealed class EffectContext
    {
        public ClimberController Climber { get; }
        public ClimberView View { get; }
        public CameraRig Rig { get; }
        public IScreenFeedback Screen { get; }
        public LevelConfig Config { get; }

        public EffectContext(ClimberController climber, ClimberView view, CameraRig rig, IScreenFeedback screen, LevelConfig config)
        {
            Climber = climber;
            View = view;
            Rig = rig;
            Screen = screen;
            Config = config;
        }
    }
}

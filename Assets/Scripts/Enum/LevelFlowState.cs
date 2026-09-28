namespace GodTower.Core
{
    /// <summary>Flow of one level: Intro → Playing ⇄ Paused → Won | Lost.</summary>
    public enum LevelFlowState
    {
        Intro,
        Playing,
        Paused,
        Won,
        Lost,
    }
}

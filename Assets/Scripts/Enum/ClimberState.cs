namespace GodTower.Player
{
    /// <summary>Explicit climber states; values are also sent to the optional Animator's State parameter.</summary>
    public enum ClimberState
    {
        Idle = 0,
        Climb = 1,
        Hit = 2,
        Fall = 3,
        Win = 4,
        Lose = 5,
    }
}

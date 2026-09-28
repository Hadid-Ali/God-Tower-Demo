namespace GodTower.Effects
{
    /// <summary>Where a carrier (e.g. the phoenix) is in its fly-in, settle, carry, fly-off cycle.</summary>
    public enum CarrierPhase
    {
        Hidden,
        Arriving,
        Settling,
        Carrying,
        Departing,
    }
}

namespace GodTower.Net
{
    /// <summary>How the webhook listener answers a parsed request.</summary>
    public enum BumpRoute
    {
        Bump,
        Preflight,
        NotFound,
        MethodNotAllowed,
    }
}

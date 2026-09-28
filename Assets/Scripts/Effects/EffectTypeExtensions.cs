namespace GodTower.Effects
{
    public static class EffectTypeExtensions
    {
        /// <summary>
        /// "Gift" events (as sent by viewers in the reference) show in the left, blue feed;
        /// tower attacks show in the right, red feed.
        /// </summary>
        public static bool IsGift(this EffectType type) =>
            type == EffectType.GloveBurst || type == EffectType.Jetpack || type == EffectType.Phoenix;

        public static string DisplayName(this EffectType type)
        {
            switch (type)
            {
                case EffectType.GloveBurst: return "Boxing";
                case EffectType.Jetpack: return "JetPack";
                case EffectType.Phoenix: return "Phoenix";
                case EffectType.Axe: return "Axe";
                case EffectType.Explosion: return "Bomb";
                case EffectType.Dragon: return "Dragon";
                default: return type.ToString();
            }
        }
    }
}

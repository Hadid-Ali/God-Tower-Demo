using System.Collections.Generic;
using GodTower.Effects;
using UnityEngine;

namespace GodTower.UI
{
    /// <summary>
    /// Two short stacks of event toasts, as in the reference: gifts (webhook gloves, boosts) on the
    /// left in blue, attacks on the right in red. Kept in the middle band, clear of the meter ends.
    /// </summary>
    public sealed class NotificationFeed : MonoBehaviour
    {
        [SerializeField] NotificationItem itemPrefab;
        [SerializeField] RectTransform leftStack;
        [SerializeField] RectTransform rightStack;
        [SerializeField, Min(1)] int maxPerSide = 3;
        [SerializeField] float lifetime = 3.2f;
        [SerializeField, Tooltip("Same event within this window increments the existing toast.")] float coalesceWindow = 1.5f;
        [SerializeField] Color giftColor = new Color(0.16f, 0.45f, 0.95f, 0.92f);
        [SerializeField] Color attackColor = new Color(0.95f, 0.32f, 0.12f, 0.92f);

        readonly List<NotificationItem> _left = new List<NotificationItem>();
        readonly List<NotificationItem> _right = new List<NotificationItem>();

        public void Post(EffectType type, string source)
        {
            bool gift = type.IsGift();
            List<NotificationItem> items = gift ? _left : _right;
            string key = type.ToString();

            if (items.Count > 0)
            {
                NotificationItem newest = items[items.Count - 1];
                if (newest.Key == key && newest.Age < coalesceWindow)
                {
                    newest.Increment();
                    return;
                }
            }

            if (items.Count >= maxPerSide) Remove(items, 0);

            NotificationItem item = Instantiate(itemPrefab, gift ? leftStack : rightStack);
            item.Show(key, source, type.DisplayName(), gift ? giftColor : attackColor);
            items.Add(item);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Expire(_left, dt);
            Expire(_right, dt);
        }

        void Expire(List<NotificationItem> items, float dt)
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                items[i].Tick(dt);
                if (items[i].Age >= lifetime) Remove(items, i);
            }
        }

        static void Remove(List<NotificationItem> items, int index)
        {
            NotificationItem item = items[index];
            items.RemoveAt(index);
            if (item != null) Destroy(item.gameObject);
        }

        public void ClearAll()
        {
            while (_left.Count > 0) Remove(_left, 0);
            while (_right.Count > 0) Remove(_right, 0);
        }
    }
}

using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace GodTower.Core
{
    /// <summary>
    /// Marshals work from background threads (e.g. the webhook listener) onto Unity's main thread.
    /// Any thread may call <see cref="Enqueue"/>; the queue is drained once per frame in Update.
    /// </summary>
    public sealed class MainThreadDispatcher : MonoBehaviour
    {
        const int MaxActionsPerFrame = 64;

        readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

        public void Enqueue(Action action)
        {
            if (action != null) _queue.Enqueue(action);
        }

        void Update()
        {
            // Bounded per frame so a flood of requests can never stall a frame.
            for (int i = 0; i < MaxActionsPerFrame && _queue.TryDequeue(out var action); i++)
            {
                try { action(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
    }
}

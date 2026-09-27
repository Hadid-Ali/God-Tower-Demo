using UnityEngine;

namespace GodTower.Level
{
    /// <summary>
    /// Builds the tower for a level by stacking one segment prefab up to the goal height,
    /// places the optional summit on top and applies the skybox.
    /// </summary>
    public sealed class LevelBuilder : MonoBehaviour
    {
        [SerializeField] private GameObject segmentPrefab;
        [SerializeField, Tooltip("Stack height of one segment in meters. 0 = measure the first spawned segment.")]
        float segmentHeight;
        [SerializeField, Tooltip("Optional. Placed at the goal height. A child named \"StandPoint\" marks where the climber lands.")]
        GameObject summitPrefab;
        [SerializeField] Transform towerRoot;

        /// <summary>World position where the climber stands after reaching the summit.</summary>
        public Vector3 SummitStandPoint { get; private set; }

        public void Build(LevelConfig config)
        {
            if (towerRoot == null) towerRoot = transform;

            float goal = config.GoalMeters;
            GameObject first = Spawn(0f);
            Bounds bounds = MeasureBounds(first);
            float step = segmentHeight > 0f ? segmentHeight : bounds.size.y;
            // Where a segment's mesh ends relative to its pivot (the pivot is not necessarily at the bottom).
            float topAbovePivot = bounds.max.y - first.transform.position.y;

            // Stack whole segments so the column ends at the segment boundary nearest the goal, and nothing
            // rises past it: the summit sits on that top instead of being buried inside the column.
            int count = Mathf.Max(1, Mathf.RoundToInt((goal - topAbovePivot) / step) + 1);
            for (int i = 1; i < count; i++) Spawn(i * step);

            float columnTop = (count - 1) * step + topAbovePivot;
            SummitStandPoint = new Vector3(0f, columnTop, 0f);
            if (summitPrefab == null) return;

            GameObject summit = Instantiate(summitPrefab, SummitStandPoint, Quaternion.identity, towerRoot);
            Transform stand = summit.transform.Find("StandPoint");
            if (stand != null) SummitStandPoint = stand.position;
        }

        GameObject Spawn(float y) => Instantiate(segmentPrefab, new Vector3(0f, y, 0f), Quaternion.identity, towerRoot);

        static Bounds MeasureBounds(GameObject segment)
        {
            Vector3 origin = segment.transform.position;
            Renderer[] renderers = segment.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(origin + Vector3.up * 0.5f, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
            if (bounds.size.y < 0.1f) bounds.size = new Vector3(bounds.size.x, 0.1f, bounds.size.z);
            return bounds;
        }
    }
}


using UnityEngine;

/// <summary>
/// Represents a spawn region containing the spawn positions used by the
/// NetworkPlayerSpawner.
/// Attach this component to the parent GameObject of the spawn point transforms.
/// </summary>
public class SpawnRegion : MonoBehaviour
{
    [Header("Spawn Region")]
    [SerializeField]
    [Min(1)]
    private int expectedSpawnPointCount = 4;

    [SerializeField]
    private bool isEnabledForSpawning = true;

    public bool IsEnabledForSpawning =>
        isEnabledForSpawning;

    private Transform[] spawnPoints;

    public int SpawnPointCount =>
        spawnPoints != null ? spawnPoints.Length : 0;

    public bool HasExpectedSpawnPointCount =>
        SpawnPointCount == expectedSpawnPointCount;

    private void Awake()
    {
        RefreshSpawnPoints();
    }

    private void OnValidate()
    {
        expectedSpawnPointCount = Mathf.Max(1, expectedSpawnPointCount);
        RefreshSpawnPoints();
    }

    /// <summary>
    /// Reads the direct children of this region in hierarchy order.
    /// Each direct child represents one player spawn position.
    /// </summary>
    public void RefreshSpawnPoints()
    {
        int childCount = transform.childCount;
        spawnPoints = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            spawnPoints[i] = transform.GetChild(i);
        }

        if (childCount != expectedSpawnPointCount)
        {
            Debug.LogWarning(
                $"[SpawnRegion] A região '{name}' possui {childCount} " +
                $"SpawnPoints, mas o esperado é {expectedSpawnPointCount}.",
                this
            );
        }
    }

    /// <summary>
    /// Returns the spawn position at the requested zero-based index.
    /// Returns null when the index is invalid.
    /// </summary>
    public Transform GetSpawnPoint(int index)
    {
        if (spawnPoints == null)
            RefreshSpawnPoints();

        if (index < 0 || index >= spawnPoints.Length)
            return null;

        return spawnPoints[index];
    }
}

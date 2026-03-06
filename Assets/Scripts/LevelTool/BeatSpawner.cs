using UnityEngine;

public class BeatSpawner : MonoBehaviour
{
    public RythmLevelData levelData;
    public BeatSystem beatSystem; // ton script FMOD
    public GameObject blockPrefab;
    public RSE_OnObstacleHitPlayer onPlayerHit;

    [Header("Grid")]
    [SerializeField] public int gridWidth = 3;
    [SerializeField] public int gridDepth = 4;
    [SerializeField] public float laneWidth = 0.6f;
    [SerializeField] public float depthSpacing = 0.6f;
    [Tooltip("Si true, les blocs instanciés seront parentés sous ce GameObject (utile pour organisation et rotation).")]
    [SerializeField] public bool parentSpawnedBlocks = true;

    [Header("Gizmos")]
    [SerializeField] private Color gizmoColor = new Color(0f, 0.6f, 1f, 0.3f);
    [SerializeField] private Color gizmoOutline = Color.cyan;
    [SerializeField] private float gizmoHeight = 0.05f;

    private int currentBeatIndex = 0;
    private bool canSpawn = true;

    private void OnEnable()
    {
        if (beatSystem != null)
            beatSystem.OnBeat += HandleBeat;

        onPlayerHit.Action += OnDeath;
    }

    private void OnDisable()
    {
        if (beatSystem != null)
            beatSystem.OnBeat -= HandleBeat;

        onPlayerHit.Action -= OnDeath;
    }

    public void OnDeath()
    {
        canSpawn = false;
    }

    void HandleBeat()
    {
        if (!canSpawn) return;
        SpawnBeat(currentBeatIndex);
        currentBeatIndex++;
    }

    void SpawnBeat(int beatIndex)
    {
        BeatFrame frame = levelData.GetBeat(beatIndex);
        if (frame == null) return;

        foreach (var block in frame.blocks)
        {
            Vector3 localPos = GridToLocalPosition(block.x, block.z);
            GameObject go;
            if (parentSpawnedBlocks)
            {
                go = Instantiate(blockPrefab, transform);
                go.transform.localPosition = localPos;
                // keep local upright orientation
                go.transform.localRotation = Quaternion.identity;
            }
            else
            {
                Vector3 worldPos = transform.TransformPoint(localPos);
                go = Instantiate(blockPrefab, worldPos, transform.rotation);
            }
        }
    }

    // retourne la position locale (par rapport au transform) de la cellule x,z
    Vector3 GridToLocalPosition(int x, int z)
    {
        // centre la grille sur l'axe X : x in [0..gridWidth-1]
        float centerOffsetX = (gridWidth - 1) * 0.5f;
        float localX = (x - centerOffsetX) * laneWidth;

        // profondeur : z en avant (+Z local)
        float localZ = z * depthSpacing;

        return new Vector3(localX, 0f, localZ);
    }

    // dessine la grille dans l'éditeur quand l'objet est sélectionné
    private void OnDrawGizmosSelected()
    {
        if (gridWidth <= 0 || gridDepth <= 0) return;

        Gizmos.matrix = transform.localToWorldMatrix;

        // draw filled cells
        Gizmos.color = gizmoColor;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                Vector3 localPos = GridToLocalPosition(x, z);
                Vector3 size = new Vector3(laneWidth * 0.9f, gizmoHeight, depthSpacing * 0.9f);
                Gizmos.DrawCube(localPos + Vector3.up * (size.y * 0.5f), size);
            }
        }

        // draw outlines
        Gizmos.color = gizmoOutline;
        // outer rectangle
        float width = (gridWidth - 1) * laneWidth + laneWidth;
        float depth = (gridDepth - 1) * depthSpacing + depthSpacing;
        Vector3 center = new Vector3(0f, gizmoHeight * 0.5f, (gridDepth - 1) * depthSpacing * 0.5f);
        Vector3 boxSize = new Vector3(width, gizmoHeight, depth);
        Gizmos.DrawWireCube(center, boxSize);

        // draw cell lines (optional)
        for (int x = 0; x <= gridWidth; x++)
        {
            float localX = (x - 0.5f * (gridWidth - 1) - 0.5f) * laneWidth + 0.5f * laneWidth;
            if (gridWidth == 1) localX = 0f;
            Vector3 a = new Vector3(localX, 0f, 0f);
            Vector3 b = new Vector3(localX, 0f, (gridDepth - 1) * depthSpacing);
            Gizmos.DrawLine(a, b);
        }
        for (int z = 0; z <= gridDepth; z++)
        {
            float localZ = z * depthSpacing - 0.5f * depth;
            Vector3 a = new Vector3(-((gridWidth - 1) * 0.5f) * laneWidth - laneWidth * 0.5f + laneWidth * 0.5f, 0f, localZ);
            Vector3 b = new Vector3(((gridWidth - 1) * 0.5f) * laneWidth + laneWidth * 0.5f - laneWidth * 0.5f, 0f, localZ);
            // simpler: compute ends directly using GridToLocalPosition
            Vector3 left = GridToLocalPosition(0, Mathf.Clamp(z, 0, gridDepth - 1));
            Vector3 right = GridToLocalPosition(gridWidth - 1, Mathf.Clamp(z, 0, gridDepth - 1));
            left.z = localZ; right.z = localZ;
            Gizmos.DrawLine(left, right);
        }

        // restore matrix
        Gizmos.matrix = Matrix4x4.identity;
    }
}

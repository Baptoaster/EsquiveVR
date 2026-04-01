using System.Collections.Generic;
using UnityEngine;

public class LevelPreviewSystem : MonoBehaviour
{
    [Header("Level Data")]
    public RythmLevelData levelData;
    public GameObject blockPrefab;

    [Header("Grid Settings")]
    public int gridWidth = 3;
    public int gridLength = 4;
    public float spacingX = 0.6f;
    public float spacingZ = 0.6f;

    [Header("Movement Settings")]
    public float travelTime = 2f;    // temps pour que le bloc atteigne le joueur
    public float spawnDistance = 10f; // distance initiale

    [Header("Runtime")]
    public float bpm = 120f;
    private float beatDuration;

    // Pooling
    private List<GameObject> activeBlocks = new List<GameObject>();
    private Queue<GameObject> blockPool = new Queue<GameObject>();

    void Start()
    {
        beatDuration = 60f / bpm;
    }

    /// <summary>
    /// Met à jour la preview du niveau en fonction du temps courant
    /// </summary>
    public void SetTime(float currentTime)
    {
        if (levelData == null || blockPrefab == null)
            return;

        ClearActiveBlocks();

        // Calculer quels beats sont visibles
        for (int beatIndex = 0; beatIndex < levelData.totalBeats; beatIndex++)
        {
            float beatTime = beatIndex * beatDuration;
            float timeUntilHit = beatTime - currentTime;

            // garder seulement les obstacles visibles
            if (timeUntilHit < -1f || timeUntilHit > travelTime)
                continue;

            SpawnBeatPreview(beatIndex, timeUntilHit);
        }
    }

    void SpawnBeatPreview(int beatIndex, float timeUntilHit)
    {
        BeatFrame frame = levelData.GetBeat(beatIndex);
        if (frame == null) return;

        foreach (var block in frame.blocks)
        {
            Vector3 pos = GetBlockPosition(block, timeUntilHit);
            GameObject go = GetPooledBlock();
            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;
            go.SetActive(true);

            activeBlocks.Add(go);
        }
    }

    Vector3 GetBlockPosition(GridBlock block, float timeUntilHit)
    {
        // forward Z progression
        float progress = Mathf.Clamp01(1f - (timeUntilHit / travelTime));
        float forwardZ = Mathf.Lerp(spawnDistance, 0f, progress);

        // position x according to grid
        float x = (block.x - (gridWidth / 2f)) * spacingX;
        // use block.z as vertical (Y) position, centered
        float y = (block.z - (gridLength / 2f)) * spacingZ;

        return new Vector3(x, y, forwardZ);
    }

    #region Pooling

    GameObject GetPooledBlock()
    {
        if (blockPool.Count > 0)
        {
            return blockPool.Dequeue();
        }
        else
        {
            GameObject go = Instantiate(blockPrefab);
            go.SetActive(false);
            return go;
        }
    }

    void ClearActiveBlocks()
    {
        foreach (var go in activeBlocks)
        {
            go.SetActive(false);
            blockPool.Enqueue(go);
        }
        activeBlocks.Clear();
    }

    #endregion
}

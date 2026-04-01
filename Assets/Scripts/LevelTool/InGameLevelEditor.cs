using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


#if UNITY_EDITOR
using FMODUnity;
#endif

/// <summary>
/// Gestionnaire principal du level editor in-game
/// Gère la musique, la timeline, et la synchronisation des blocs
/// Similaire à l'éditeur de Beat Saber
/// </summary>
public class InGameLevelEditor : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private RythmLevelData levelData;
    
#if UNITY_EDITOR
    [SerializeField] private EventReference musicEvent;
#endif

    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 4;
    [SerializeField] private int gridDepth = 3;
    [SerializeField] private float spacingX = 0.6f;
    [SerializeField] private float spacingZ = 0.6f;

    [Header("Visual Settings")]
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private Material selectedBlockMaterial;
    [SerializeField] private Material normalBlockMaterial;

    [Header("Playback Settings")]
    [SerializeField] private float travelTime = 2f;
    [SerializeField] private float spawnDistance = 10f;

    // Music
#if UNITY_EDITOR
    private FMOD.Studio.EventInstance musicInstance;
#endif
    private bool isMusicPlaying = false;
    private bool musicInstanceStarted = false;
    private float beatDuration;

    // Timeline
    public float CurrentTime { get; private set; }
    public int CurrentBeat { get; private set; }
    public float MaxTime { get; private set; }

    // Grid & Selection
    private int selectedGridX = 0;
    private int selectedGridZ = 0;
    private bool isGridHighlighted = true;

    // Visual blocks
    private List<GameObject> activeBlocks = new List<GameObject>();
    private Queue<GameObject> blockPool = new Queue<GameObject>();

    // State
    private bool isInitialized = false;

    public bool IsMusicPlaying => isMusicPlaying;
    public bool IsInitialized => isInitialized;

    // Expose materials for PreviewBlock
    public Material NormalMaterial => normalBlockMaterial;
    public Material SelectedMaterial => selectedBlockMaterial;

    private void Awake()
    {
        if (levelData == null)
        {
            Debug.LogError("InGameLevelEditor: Level Data not assigned!");
            return;
        }

        beatDuration = 60f / levelData.bpm;
        MaxTime = levelData.totalBeats * beatDuration;

#if UNITY_EDITOR
        // Préparer la musique (ne pas la lancer encore)
        musicInstance = FMODUnity.RuntimeManager.CreateInstance(musicEvent);
#endif
        isInitialized = true;
    }

    private void Start()
    {
        // Placer la sélection au centre
        selectedGridX = gridWidth / 2;
        selectedGridZ = gridDepth / 2;
    }

    private void Update()
    {
        if (!isInitialized) return;

#if UNITY_EDITOR
        // Mettre à jour le temps de la musique
        if (isMusicPlaying)
        {
            musicInstance.getTimelinePosition(out int ms);
            CurrentTime = ms / 1000f;
        }
#endif

        CurrentBeat = Mathf.FloorToInt(CurrentTime / beatDuration);

        // Mettre à jour la visualisation
        UpdateBlockVisualization();

        // Gestion du clavier pour la navigation
        HandleGridNavigation();
    }

    /// <summary>
    /// Lance ou met en pause la musique
    /// </summary>
    public void ToggleMusic()
    {
        if (isMusicPlaying)
        {
            PauseMusic();
        }
        else
        {
            PlayMusic();
        }
    }

    public void PlayMusic()
    {
        if (isMusicPlaying) return;

#if UNITY_EDITOR
        if (!musicInstance.isValid())
        {
            musicInstance = FMODUnity.RuntimeManager.CreateInstance(musicEvent);
            musicInstanceStarted = false;
        }

        if (!musicInstanceStarted)
        {
            musicInstance.start();
            musicInstanceStarted = true;
        }
        else
        {
            // Resume if previously started and paused
            musicInstance.setPaused(false);
        }
#endif
        isMusicPlaying = true;
        Debug.Log("Music started");
    }

    public void PauseMusic()
    {
        if (!isMusicPlaying) return;

#if UNITY_EDITOR
        // Pause the instance rather than stop so timeline can be resumed
        if (musicInstance.isValid())
            musicInstance.setPaused(true);
#endif
        isMusicPlaying = false;
        Debug.Log("Music paused");
    }

    /// <summary>
    /// Définit la position actuelle dans la timeline
    /// </summary>
    public void SetTime(float time)
    {
        CurrentTime = Mathf.Clamp(time, 0f, MaxTime);

#if UNITY_EDITOR
        // Ensure we have an instance
        if (!musicInstance.isValid())
        {
            musicInstance = FMODUnity.RuntimeManager.CreateInstance(musicEvent);
            musicInstanceStarted = false;
        }

        // Set timeline position and keep paused so scrubbing works
        musicInstance.setTimelinePosition((int)(CurrentTime * 1000));
        musicInstance.setPaused(true);
#endif
    }

    /// <summary>
    /// Ajoute un bloc à la position sélectionnée au beat courant
    /// </summary>
    public void AddBlockAtSelectedPosition(ObstacleType type = ObstacleType.Normal)
    {
        GridBlock block = new GridBlock
        {
            x = selectedGridX,
            z = selectedGridZ,
            type = type
        };

        levelData.AddBlock(CurrentBeat, block);
        Debug.Log($"Block added at beat {CurrentBeat}, position ({selectedGridX}, {selectedGridZ})");
    }

    /// <summary>
    /// Supprime le bloc à la position sélectionnée au beat courant
    /// </summary>
    public void RemoveBlockAtSelectedPosition()
    {
        levelData.RemoveBlock(CurrentBeat, selectedGridX, selectedGridZ);
        Debug.Log($"Block removed at beat {CurrentBeat}, position ({selectedGridX}, {selectedGridZ})");
    }

    /// <summary>
    /// Supprime tous les blocs du beat courant
    /// </summary>
    public void ClearCurrentBeat()
    {
        BeatFrame frame = levelData.GetBeat(CurrentBeat);
        if (frame != null)
        {
            frame.blocks.Clear();
            Debug.Log($"Beat {CurrentBeat} cleared");
        }
    }

    /// <summary>
    /// Retourne le bloc à une position spécifique de la grille pour un beat donné
    /// </summary>
    public bool HasBlockAtPosition(int beatIndex, int x, int z, out GridBlock foundBlock)
    {
        foundBlock = default;
        BeatFrame frame = levelData.GetBeat(beatIndex);
        if (frame == null) return false;

        GridBlock block = frame.blocks.Find(b => b.x == x && b.z == z);
        if (block.x == x && block.z == z)
        {
            foundBlock = block;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Retourne le bloc à la position sélectionnée pour le beat courant (null si inexistant)
    /// </summary>
    public bool HasBlockAtSelectedPosition(out GridBlock foundBlock)
    {
        foundBlock = default;
        BeatFrame frame = levelData.GetBeat(CurrentBeat);
        if (frame == null) return false;

        GridBlock block = frame.blocks.Find(b => b.x == selectedGridX && b.z == selectedGridZ);
        if (block.x == selectedGridX && block.z == selectedGridZ)
        {
            foundBlock = block;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Sauvegarde le level (À implémenter selon votre système de sauvegarde)
    /// </summary>
    public void SaveLevel()
    {
        // À adapter selon votre système de sauvegarde
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(levelData);
        UnityEditor.AssetDatabase.SaveAssets();
        #endif
        Debug.Log("Level saved!");
    }

    #region Private Methods

    private void HandleGridNavigation()
    {
        // Navigation grille avec les flèches ou WASD
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
            selectedGridX = Mathf.Max(0, selectedGridX - 1);
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
            selectedGridX = Mathf.Min(gridWidth - 1, selectedGridX + 1);
        if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
            selectedGridZ = Mathf.Max(0, selectedGridZ - 1);
        if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
            selectedGridZ = Mathf.Min(gridDepth - 1, selectedGridZ + 1);

        // Ajouter/Supprimer un bloc
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (HasBlockAtSelectedPosition(out GridBlock _))
                RemoveBlockAtSelectedPosition();
            else
                AddBlockAtSelectedPosition();
        }

        // Contrôles musique
        if (Keyboard.current.pKey.wasPressedThisFrame)
            ToggleMusic();

        // Sauvegarde
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            SaveLevel();
    }

    private void UpdateBlockVisualization()
    {
        ClearActiveBlocks();

        // Afficher les blocs visibles à l'écran
        for (int beatIndex = 0; beatIndex < levelData.totalBeats; beatIndex++)
        {
            float beatTime = beatIndex * beatDuration;
            float timeUntilHit = beatTime - CurrentTime;

            // Garder seulement les obstacles visibles
            if (timeUntilHit < -0.5f || timeUntilHit > travelTime)
                continue;

            BeatFrame frame = levelData.GetBeat(beatIndex);
            if (frame == null) continue;

            foreach (var block in frame.blocks)
            {
                GameObject go = GetPooledBlock();

                // Ensure PreviewBlock component exists
                PreviewBlock preview = go.GetComponent<PreviewBlock>();
                if (preview == null)
                    preview = go.AddComponent<PreviewBlock>();

                preview.Init(beatIndex, block.x, block.z, (int)block.type, ComputeBlockPositionForUI, normalBlockMaterial, selectedBlockMaterial);
                preview.UpdateForTime(CurrentTime, beatIndex == CurrentBeat && block.x == selectedGridX && block.z == selectedGridZ);

                go.SetActive(true);
                activeBlocks.Add(go);
            }
        }

        // Afficher la grille de sélection actuelle
        if (isGridHighlighted)
        {
            float beatTime = CurrentBeat * beatDuration;
            float timeUntilHit = beatTime - CurrentTime;

            if (!HasBlockAtSelectedPosition(out GridBlock _))
            {
                GameObject go = GetPooledBlock();
                // Ensure PreviewBlock exists
                PreviewBlock preview = go.GetComponent<PreviewBlock>();
                if (preview == null)
                    preview = go.AddComponent<PreviewBlock>();

                // Initialize as a selection preview (use Normal type id)
                preview.Init(CurrentBeat, selectedGridX, selectedGridZ, (int)ObstacleType.Normal, ComputeBlockPositionForUI, normalBlockMaterial, selectedBlockMaterial);
                preview.UpdateForTime(CurrentTime, true);

                Renderer r = go.GetComponent<Renderer>();
                if (r != null && selectedBlockMaterial != null)
                    r.material = selectedBlockMaterial;

                go.SetActive(true);
                activeBlocks.Add(go);
            }
        }
    }

    // Delegate used by PreviewBlock
    private Vector3 ComputeBlockPositionForUI(int gridX, int gridZ, int beatIndex, float currentTime)
    {
        float beatTime = beatIndex * beatDuration;
        float timeUntilHit = beatTime - currentTime;

        float progress = Mathf.Clamp01(1f - (timeUntilHit / travelTime));
        float forwardZ = Mathf.Lerp(spawnDistance, 0f, progress);

        float x = (gridX - (gridWidth / 2f)) * spacingX;
        // Use gridZ as vertical position (Y) and center the grid vertically
        float y = (gridZ - (gridDepth / 2f)) * spacingZ;

        return new Vector3(x, y, forwardZ);
    }

    /// <summary>
    /// Compute deterministic world position for a block based on its beat and the current time.
    /// </summary>
    public Vector3 ComputeBlockPosition(GridBlock block, int beatIndex, float currentTime)
    {
        float beatTime = beatIndex * beatDuration;
        float timeUntilHit = beatTime - currentTime;

        float progress = Mathf.Clamp01(1f - (timeUntilHit / travelTime));
        float forwardZ = Mathf.Lerp(spawnDistance, 0f, progress);

        float x = (block.x - (gridWidth / 2f)) * spacingX;
        float y = (block.z - (gridDepth / 2f)) * spacingZ;

        return new Vector3(x, y, forwardZ);
    }

    private GameObject GetPooledBlock()
    {
        if (blockPool.Count > 0)
        {
            return blockPool.Dequeue();
        }
        else
        {
            GameObject go = Instantiate(blockPrefab);
            // Ensure it has a PreviewBlock component for deterministic editor movement
            if (go.GetComponent<PreviewBlock>() == null)
                go.AddComponent<PreviewBlock>();
            go.SetActive(false);
            return go;
        }
    }

    private void ClearActiveBlocks()
    {
        foreach (var go in activeBlocks)
        {
            go.SetActive(false);
            blockPool.Enqueue(go);
        }
        activeBlocks.Clear();
    }

    #endregion

    public int GetGridWidth() => gridWidth;
    public int GetGridDepth() => gridDepth;
    public int GetSelectedGridX() => selectedGridX;
    public int GetSelectedGridZ() => selectedGridZ;
    public RythmLevelData GetLevelData() => levelData;

    /// <summary>
    /// Compte le total de blocs dans le niveau
    /// </summary>
    public int CountTotalBlocks()
    {
        int count = 0;
        foreach (var frame in levelData.beatFrames)
        {
            count += frame.blocks.Count;
        }
        return count;
    }

    /// <summary>
    /// Compte les blocs dans un beat spécifique
    /// </summary>
    public int CountBlocksInBeat(int beatIndex)
    {
        BeatFrame frame = levelData.GetBeat(beatIndex);
        return frame != null ? frame.blocks.Count : 0;
    }
}
 
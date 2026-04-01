using UnityEngine;
using UnityEditor;

/// <summary>
/// Interface utilisateur complète pour l'éditeur de niveau in-game
/// Affiche la timeline, les contrôles et la grille d'édition
/// </summary>
public class InGameLevelEditorUI : MonoBehaviour
{
    [SerializeField] private InGameLevelEditor levelEditor;
    
    [Header("UI Settings")]
    [SerializeField] private float panelWidth = 350f;
    [SerializeField] private float sliderHeight = 30f;
    [SerializeField] private float gridCellSize = 45f;
    [SerializeField] private float gridSpacing = 3f;

    private Vector2 scrollPosition = Vector2.zero;
    private int selectedBlockType = 0;

    private void OnGUI()
    {
        if (levelEditor == null || !levelEditor.IsInitialized)
        {
            GUILayout.Label("Level Editor not initialized!");
            return;
        }

        // Afficher l'interface avec plusieurs panneaux
        GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));

        // Panneau de gauche - Timeline et contrôles
        DrawLeftPanel();

        GUILayout.Space(20);

        // Panneau central - Grille d'édition
        DrawGridPanel();

        GUILayout.Space(20);

        // Panneau de droite - Options
        DrawRightPanel();

        GUILayout.EndHorizontal();
    }

    private void DrawLeftPanel()
    {
        GUILayout.BeginVertical("box", GUILayout.Width(panelWidth), GUILayout.ExpandHeight(true));

        // Titre
        GUILayout.Label("LEVEL EDITOR", new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        }, GUILayout.Height(40));

        GUILayout.Space(20);

        // Infos actuelles
        GUILayout.Label($"Beat: {levelEditor.CurrentBeat}", EditorLabelStyle());
        GUILayout.Label($"Time: {levelEditor.CurrentTime:F2}s", EditorLabelStyle());

        GUILayout.Space(10);

        // Timeline Slider
        GUILayout.Label("Timeline", EditorLabelStyle());
        float newTime = GUILayout.HorizontalSlider(
            levelEditor.CurrentTime,
            0f,
            levelEditor.MaxTime,
            GUILayout.Height(sliderHeight)
        );

        if (Mathf.Abs(newTime - levelEditor.CurrentTime) > 0.01f)
        {
            levelEditor.SetTime(newTime);
        }

        GUILayout.Label($"{levelEditor.CurrentTime:F2}s / {levelEditor.MaxTime:F2}s", EditorLabelStyle());

        GUILayout.Space(15);

        // Contrôles musique
        GUILayout.Label("Playback", new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold
        }, GUILayout.Height(25));

        GUILayout.BeginHorizontal();
        
        string playButtonText = levelEditor.IsMusicPlaying ? "? PAUSE" : "? PLAY";
        if (GUILayout.Button(playButtonText, GUILayout.Height(40)))
        {
            levelEditor.ToggleMusic();
        }

        if (GUILayout.Button("? RESET", GUILayout.Height(40)))
        {
            levelEditor.SetTime(0f);
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(15);

        // Contrôles de précision
        GUILayout.Label("Precision Controls", new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 10
        }, GUILayout.Height(20));

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("? -0.5s", GUILayout.Height(30)))
        {
            levelEditor.SetTime(levelEditor.CurrentTime - 0.5f);
        }
        if (GUILayout.Button("? -1B", GUILayout.Height(30)))
        {
            levelEditor.SetTime(Mathf.Max(0, levelEditor.CurrentTime - 60f / levelEditor.GetLevelData().bpm));
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("? +1B", GUILayout.Height(30)))
        {
            levelEditor.SetTime(Mathf.Min(levelEditor.MaxTime, levelEditor.CurrentTime + 60f / levelEditor.GetLevelData().bpm));
        }
        if (GUILayout.Button("+0.5s ?", GUILayout.Height(30)))
        {
            levelEditor.SetTime(levelEditor.CurrentTime + 0.5f);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(15);

        // Boutons d'action
        GUILayout.Label("Actions", new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold
        }, GUILayout.Height(25));

        if (GUILayout.Button("CLEAR BEAT", GUILayout.Height(35)))
        {
            levelEditor.ClearCurrentBeat();
        }

        if (GUILayout.Button("SAVE LEVEL", GUILayout.Height(35)))
        {
            levelEditor.SaveLevel();
        }

        GUILayout.Space(15);

        // Instructions
        GUILayout.Label("Keyboard Shortcuts", new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 9
        }, GUILayout.Height(20));

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));

        GUILayout.Label(
            "? / ? or A / D: Move X\n" +
            "? / ? or W / S: Move Z\n" +
            "SPACE: Add/Remove\n" +
            "P: Play/Pause\n" +
            "ESC: Save\n\n" +
            "Tips:\n" +
            "- Edit grid in center\n" +
            "- Use timeline slider\n" +
            "- Watch preview live",
            new GUIStyle(GUI.skin.label)
            {
                fontSize = 8,
                wordWrap = true
            }
        );

        GUILayout.EndScrollView();

        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
    }

    private void DrawGridPanel()
    {
        GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));

        GUILayout.Label("GRID EDITOR", new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        }, GUILayout.Height(40));

        GUILayout.BeginVertical("box");

        int gridWidth = levelEditor.GetGridWidth();
        int gridDepth = levelEditor.GetGridDepth();
        int selectedX = levelEditor.GetSelectedGridX();
        int selectedZ = levelEditor.GetSelectedGridZ();

        // Afficher la grille
        for (int z = 0; z < gridDepth; z++)
        {
            GUILayout.BeginHorizontal();

            for (int x = 0; x < gridWidth; x++)
            {
                bool hasBlock = levelEditor.HasBlockAtPosition(levelEditor.CurrentBeat, x, z, out GridBlock block);
                
                bool isSelected = (x == selectedX && z == selectedZ);

                // Coloration
                Color originalColor = GUI.backgroundColor;
                if (isSelected && hasBlock)
                {
                    GUI.backgroundColor = new Color(1f, 1f, 0f); // Jaune - sélectionné avec bloc
                }
                else if (isSelected)
                {
                    GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f); // Gris clair - sélectionné vide
                }
                else if (hasBlock)
                {
                    GUI.backgroundColor = new Color(1f, 0.2f, 0.2f); // Rouge - bloc
                }
                else
                {
                    GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f); // Gris foncé - vide
                }

                string label = hasBlock ? "?" : "?";
                if (GUILayout.Button(label, GUILayout.Width(gridCellSize), GUILayout.Height(gridCellSize)))
                {
                    // Click sur la grille
                    if (hasBlock)
                    {
                        levelEditor.RemoveBlockAtSelectedPosition();
                    }
                    else
                    {
                        levelEditor.AddBlockAtSelectedPosition(ObstacleType.Normal);
                    }
                }

                GUI.backgroundColor = originalColor;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(gridSpacing);
        }

        GUILayout.EndVertical();

        // Infos sur la position sélectionnée
        GUILayout.Space(10);
        GUILayout.Label($"Selected: ({selectedX}, {selectedZ})", EditorLabelStyle());

        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
    }

    private void DrawRightPanel()
    {
        GUILayout.BeginVertical("box", GUILayout.Width(panelWidth), GUILayout.ExpandHeight(true));

        GUILayout.Label("OPTIONS", new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        }, GUILayout.Height(40));

        GUILayout.Space(15);

        // Type de bloc
        GUILayout.Label("Block Type", new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold
        }, GUILayout.Height(25));

        GUI.backgroundColor = selectedBlockType == 0 ? Color.cyan : Color.gray;
        if (GUILayout.Button("Normal", GUILayout.Height(35)))
        {
            selectedBlockType = 0;
        }
        GUI.backgroundColor = Color.white;

        GUI.backgroundColor = selectedBlockType == 1 ? Color.cyan : Color.gray;
        if (GUILayout.Button("Delayed Rush", GUILayout.Height(35)))
        {
            selectedBlockType = 1;
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(20);

        // Statistiques
        GUILayout.Label("Statistics", new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold
        }, GUILayout.Height(25));

        int totalBlocks = levelEditor.CountTotalBlocks();
        int blocksInCurrentBeat = levelEditor.CountBlocksInBeat(levelEditor.CurrentBeat);

        GUILayout.Label($"Total Blocks: {totalBlocks}", EditorLabelStyle());
        GUILayout.Label($"Beat #{levelEditor.CurrentBeat}: {blocksInCurrentBeat} blocks", EditorLabelStyle());
        GUILayout.Label($"Difficulty: {CalculateDifficulty(totalBlocks)}", EditorLabelStyle());

        GUILayout.Space(20);

        // Infos de la data
        RythmLevelData data = levelEditor.GetLevelData();
        GUILayout.Label("Level Info", new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold
        }, GUILayout.Height(25));

        GUILayout.Label($"BPM: {data.bpm}", EditorLabelStyle());
        GUILayout.Label($"Total Beats: {data.totalBeats}", EditorLabelStyle());
        GUILayout.Label($"Duration: {(data.totalBeats * 60f / data.bpm):F1}s", EditorLabelStyle());

        GUILayout.Space(20);

        // Debug
        GUILayout.Label("Debug", new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            fontStyle = FontStyle.Bold
        }, GUILayout.Height(20));

        GUILayout.Label($"Music Playing: {levelEditor.IsMusicPlaying}", EditorLabelStyle());
        GUILayout.Label($"Grid: {levelEditor.GetGridWidth()}x{levelEditor.GetGridDepth()}", EditorLabelStyle());

        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
    }

    #region Helper Methods

    private GUIStyle EditorLabelStyle()
    {
        return new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Normal
        };
    }

    private string CalculateDifficulty(int totalBlocks)
    {
        if (totalBlocks < 20) return "Easy";
        if (totalBlocks < 50) return "Normal";
        if (totalBlocks < 100) return "Hard";
        return "Expert";
    }

    #endregion
}

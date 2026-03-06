using UnityEditor;
using UnityEngine;

public class RythmLevelEditor : EditorWindow
{
    private RythmLevelData levelData;
    private int selectedBeat = 0;

    private const int gridWidth = 4;
    private const int gridDepth = 3;

    [MenuItem("Tools/Rhythm Level Editor")]
    public static void Open()
    {
        GetWindow<RythmLevelEditor>("Rhythm Editor");
    }

    private void OnGUI()
    {
        levelData = (RythmLevelData)EditorGUILayout.ObjectField("Level Data", levelData, typeof(RythmLevelData), false);

        if (levelData == null)
        {
            EditorGUILayout.HelpBox("Assign a Level Data asset.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();

        selectedBeat = EditorGUILayout.IntSlider("Beat", selectedBeat, 0, levelData.totalBeats);

        DrawGrid();

        if (GUILayout.Button("Clear Beat"))
        {
            levelData.beatFrames.RemoveAll(b => b.beatIndex == selectedBeat);
            EditorUtility.SetDirty(levelData);
        }
    }

    void DrawGrid()
    {
        BeatFrame frame = levelData.GetBeat(selectedBeat);

        for (int z = 0; z < gridDepth; z++)
        {
            EditorGUILayout.BeginHorizontal();

            for (int x = 0; x < gridWidth; x++)
            {
                bool hasBlock = frame != null && frame.blocks.Exists(b => b.x == x && b.z == z);

                GUI.backgroundColor = hasBlock ? Color.red : Color.gray;

                if (GUILayout.Button("", GUILayout.Width(50), GUILayout.Height(50)))
                {
                    if (hasBlock)
                        levelData.RemoveBlock(selectedBeat, x, z);
                    else
                        levelData.AddBlock(selectedBeat, new GridBlock { x = x, z = z });

                    EditorUtility.SetDirty(levelData);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        GUI.backgroundColor = Color.white;
    }
}

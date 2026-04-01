using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Écran de démarrage permettant de choisir entre Play ou Edit Mode
/// À placer sur une scène de démarrage simple
/// </summary>
public class GameBootLoader : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string gameplaySceneName = "Gameplay";
    [SerializeField] private string editorSceneName = "LevelEditor";

    [Header("UI")]
    [SerializeField] private int buttonWidth = 200;
    [SerializeField] private int buttonHeight = 100;

    private Texture2D playButtonTexture;
    private Texture2D editButtonTexture;

    void OnGUI()
    {
        // Fond sombre
        GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        GUI.backgroundColor = Color.white;

        // Titre
        GUILayout.BeginVertical();
        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.BeginVertical(GUILayout.Width(500));
        
        GUILayout.Label("Esquive VR", new GUIStyle(GUI.skin.label)
        {
            fontSize = 48,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        }, GUILayout.Height(80));

        GUILayout.Space(40);

        // Bouton Play
        if (GUILayout.Button("? PLAY GAME", GUILayout.Height(buttonHeight)))
        {
            LoadScene(gameplaySceneName);
        }

        GUILayout.Space(20);

        // Bouton Edit
        if (GUILayout.Button("? EDIT LEVEL", GUILayout.Height(buttonHeight)))
        {
            LoadScene(editorSceneName);
        }

        GUILayout.Space(20);

        // Bouton Quit
        if (GUILayout.Button("? QUIT", GUILayout.Height(buttonHeight)))
        {
            Application.Quit();
        }

        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
    }

    void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}

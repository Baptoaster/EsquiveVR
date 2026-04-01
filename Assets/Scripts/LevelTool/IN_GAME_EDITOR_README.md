# In-Game Level Editor - Documentation

## ?? Vue d'ensemble

Ce système permet d'éditer les niveaux de rythme directement dans le jeu, similaire à l'éditeur de **Beat Saber**. Les joueurs peuvent:
- Choisir entre le mode Jeu ou Mode Éditeur au démarrage
- Placer des blocs sur une timeline interactive
- Visualiser les blocs en temps réel
- Sauvegarder les modifications

## ?? Composants Principaux

### 1. **GameBootLoader** (Écran de démarrage)
- Affiche un menu de sélection Play/Edit
- Charge la scène appropriée
- Aucune configuration requise - juste ajouter à une scène de démarrage

### 2. **InGameLevelEditor** (Gestionnaire principal)
- Gère la timeline et la musique FMOD
- Contrôle la grille d'édition
- Synchronise la visualisation des blocs
- Sauvegarde les données de niveau

**À assigner sur un GameObject:**
- `Level Data`: Référence à une RythmLevelData ScriptableObject
- `Music Event`: EventReference FMOD (optionnel, utilisé uniquement en éditeur)
- `Block Prefab`: GameObject préfab pour afficher les blocs
- `Selected Block Material`: Material pour les blocs sélectionnés
- `Normal Block Material`: Material pour les blocs normaux

### 3. **InGameLevelEditorUI** (Interface utilisateur)
- Panneau gauche: Timeline et contrôles de lecture
- Panneau central: Grille d'édition 4x3
- Panneau droit: Options et statistiques

**À assigner:**
- `Level Editor`: Référence au InGameLevelEditor

## ?? Contrôles Clavier

### Navigation de la grille
- `? / ?` ou `A / D`: Déplacer à gauche/droite
- `? / ?` ou `W / S`: Déplacer avant/arrière

### Édition
- `SPACE`: Ajouter/Retirer un bloc à la position sélectionnée

### Lecture
- `P`: Play/Pause la musique
- `Scroll Slider`: Aller à n'importe quel moment de la musique

### Contrôles de précision
- Boutons `-0.5s` et `+0.5s`: Déplacer de 500ms
- Boutons `-1B` et `+1B`: Déplacer d'un beat

### Actions
- `ESC`: Sauvegarder le niveau
- `CLEAR BEAT`: Supprimer tous les blocs du beat actuel

## ?? Guide de Configuration

### Étape 1: Préparer les matériaux

1. Créez deux matériaux Unity:
   - `SelectedBlockMaterial` (couleur jaune/orange)
   - `NormalBlockMaterial` (couleur rouge/standard)

```csharp
// Dans l'éditeur Unity
// Créer > Material
// Assigner les couleurs souhaitées
```

### Étape 2: Créer le préfab des blocs

1. Créez un cube simple dans une scène
2. Ajoutez un Renderer (MeshRenderer)
3. Définissez la taille à environ 0.5x0.5x0.5
4. Déplacez-le pour tester la position
5. Drag & Drop dans Assets pour en faire un préfab
6. Supprimez-le de la scène

### Étape 3: Configurer la scène d'édition

1. Créez une nouvelle scène "LevelEditor"
2. Créez un GameObject vide nommé "Editor Manager"
3. Ajoutez le script `InGameLevelEditor`
4. Assignez tous les champs requis
5. Ajoutez le script `InGameLevelEditorUI` (peut être sur n'importe quel GameObject)
6. Assignez la référence au InGameLevelEditor

### Étape 4: Créer le menu de démarrage

1. Créez une scène "Boot"
2. Créez un GameObject vide
3. Ajoutez le script `GameBootLoader`
4. Modifiez les noms de scènes si nécessaire:
   ```csharp
   [SerializeField] private string gameplaySceneName = "Gameplay";
   [SerializeField] private string editorSceneName = "LevelEditor";
   ```

### Étape 5: Configurer les Scenes dans Build Settings

Dans `File > Build Settings`:
1. Ajouter "Boot" scene (index 0)
2. Ajouter "LevelEditor" scene
3. Ajouter "Gameplay" scene (ou votre scène de jeu)

## ?? Méthodes Publiques

### Gestion de la Timeline
```csharp
// Obtenir/modifier le temps actuel
editor.SetTime(float time);

// Obtenir le beat actuel
int beat = editor.CurrentBeat;

// Obtenir le temps actuel en secondes
float time = editor.CurrentTime;
```

### Contrôle de la Musique
```csharp
// Play/Pause
editor.ToggleMusic();
editor.PlayMusic();
editor.PauseMusic();

// Vérifier l'état
bool isPlaying = editor.IsMusicPlaying;
```

### Édition des Blocs
```csharp
// Ajouter/retirer un bloc à la position sélectionnée
editor.AddBlockAtSelectedPosition(ObstacleType.Normal);
editor.RemoveBlockAtSelectedPosition();

// Vérifier s'il y a un bloc
if (editor.HasBlockAtSelectedPosition(out GridBlock block))
{
    Debug.Log($"Block trouvé: {block.x}, {block.z}");
}

// Vérifier un bloc à une position quelconque
if (editor.HasBlockAtPosition(beatIndex, x, z, out GridBlock block))
{
    Debug.Log($"Block trouvé au beat {beatIndex}");
}

// Vider un beat
editor.ClearCurrentBeat();

// Sauvegarder
editor.SaveLevel();
```

### Grille et Sélection
```csharp
// Obtenir les dimensions de la grille
int width = editor.GetGridWidth();  // 4
int depth = editor.GetGridDepth();  // 3

// Obtenir la position sélectionnée
int x = editor.GetSelectedGridX();
int z = editor.GetSelectedGridZ();

// Obtenir les données du niveau
RythmLevelData data = editor.GetLevelData();
```

### Statistiques
```csharp
// Compter les blocs
int total = editor.CountTotalBlocks();
int inBeat = editor.CountBlocksInBeat(beatIndex);
```

## ?? Résolution des Problèmes

### La musique ne joue pas
- Vérifiez que FMOD est correctement installé
- Assurez-vous que l'`EventReference` est valide
- Vérifiez que vous utilisez le système uniquement en mode éditeur

### Les blocs ne s'affichent pas
- Vérifiez que le `Block Prefab` est assigné
- Vérifiez que le préfab a un `MeshRenderer` et `Collider`
- Vérifiez que les matériaux sont assignés

### Les sauvegarde ne fonctionnent pas
- Assurez-vous que `RythmLevelData` est un ScriptableObject
- Vérifiez que vous avez les droits d'écriture dans le dossier Assets

## ?? Améliorations Futures

- [ ] Support du clavier QWERTY français
- [ ] Undo/Redo system
- [ ] Copy/Paste de patterns de blocs
- [ ] Contrôle tactile pour tablettes/VR
- [ ] Preview audio waveform
- [ ] Différents types de blocs avec couleurs
- [ ] Grille redimensionnable
- [ ] Export/Import de patterns

## ?? Exemple d'Utilisation Complète

```csharp
public class LevelEditorTester : MonoBehaviour
{
    [SerializeField] private InGameLevelEditor editor;

    void Update()
    {
        // Afficher les infos
        Debug.Log($"Beat: {editor.CurrentBeat}, Time: {editor.CurrentTime:F2}s");

        // Ajouter un bloc à la position sélectionnée avec E
        if (Input.GetKeyDown(KeyCode.E))
        {
            editor.AddBlockAtSelectedPosition(ObstacleType.Normal);
        }

        // Sauvegarder avec Ctrl+S
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
        {
            editor.SaveLevel();
        }
    }
}
```

## ?? Support

Pour des questions ou des bugs, consultez la documentation du projet ou créez une issue sur le GitHub.

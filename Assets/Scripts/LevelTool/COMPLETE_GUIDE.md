# GUIDE COMPLET - In-Game Level Editor

## ?? Objectif
Ce système vous permet de créer et éditer des niveaux de rythme directement dans le jeu, similaire à Beat Saber, avec une interface graphique complète et intuitive.

## ??? Architecture du Système

```
GameBootLoader (Écran menu)
    ?
Boot Scene
    ?
    ??? PLAY GAME ? Gameplay Scene
    ??? EDIT LEVEL ? LevelEditor Scene
        ?? InGameLevelEditor (Gestionnaire)
        ?? InGameLevelEditorUI (Interface)
        ?? LevelPreviewSystem (Visualisation)
```

## ?? Fichiers Créés

| Fichier | Rôle |
|---------|------|
| `GameBootLoader.cs` | Menu de démarrage Play/Edit |
| `InGameLevelEditor.cs` | Gestionnaire principal (timeline, musique, grille) |
| `InGameLevelEditorUI.cs` | Interface graphique (3 panneaux) |
| `LevelPreviewSystem.cs` | Visualisation des blocs existants |

## ?? Mise en Place Rapide

### 1?? Préparez vos Assets

**Créez 2 matériaux:**
```
Assets > Create > Material > "SelectedBlockMaterial" (jaune)
Assets > Create > Material > "NormalBlockMaterial" (rouge)
```

**Créez un préfab de bloc:**
- Cube 3D simple
- Taille: 0.5 × 0.5 × 0.5
- Renderer avec les matériaux
- Drag ? Assets pour créer préfab
- Supprimer de la scène

**Assurez-vous d'avoir:**
- Une `RythmLevelData` ScriptableObject
- Un `EventReference` FMOD (optionnel)

### 2?? Créez les Scènes

**Scene "Boot"** (Démarrage)
```
1. GameObject ? Vide ? renommer "Boot"
2. Add Component ? GameBootLoader.cs
3. C'est tout!
```

**Scene "LevelEditor"** (Éditeur)
```
1. GameObject ? Vide ? "Editor"
2. Add Component ? InGameLevelEditor
3. Add Component ? InGameLevelEditorUI
4. Assigner tous les fields
5. Optionnel: Créer un Cube sol (Y scale 0.01, Y = -1)
```

**Scene "Gameplay"** (Jeu existant)
```
- Votre scène de jeu normale
- Sera chargée avec le bouton PLAY
```

### 3?? Configurez Build Settings

`File > Build Settings`
```
Index 0: Boot
Index 1: LevelEditor
Index 2: Gameplay (votre scène de jeu)
```

### 4?? Lancez!

Appuyez sur Play ? Écran de menu ? Cliquez "EDIT LEVEL" ?

## ?? Tutoriel d'Utilisation

### Interface

La fenêtre est divisée en 3 panneaux:

**GAUCHE - Timeline & Contrôles**
- Affiche le beat et le temps actuels
- Slider pour scrubber la timeline
- Boutons Play/Pause/Reset
- Boutons de précision (-0.5s, -1 beat, +1 beat, +0.5s)
- Boutons Clear Beat et Save

**CENTRE - Grille d'Édition**
- Grille 4×3 (colonnes × rangées)
- ? = bloc présent
- ? = case vide
- Couleur jaune = sélectionné
- Cliquer pour placer/retirer

**DROITE - Options & Stats**
- Sélectionner le type de bloc (Normal, Delayed Rush)
- Voir les statistiques (total blocs, blocs du beat)
- Infos du niveau (BPM, beats totaux, durée)
- Debug info

### Workflow de base

1. **Scrollez la timeline** pour aller au beat désiré
2. **Naviguez la grille** avec les flèches du clavier (????) ou WASD
3. **Appuyez SPACE** pour ajouter/retirer un bloc
4. **Cliquez sur la grille** pour placer directement
5. **Lancez la musique** avec P pour vérifier le timing
6. **Sauvegardez** avec ESC ou le bouton Save

### Exemple: Créer une séquence simple

```
1. Timeline: Allez à beat 0
2. Grille: Sélectionnez le bloc du centre
3. Clavier: Appuyez SPACE pour ajouter
4. Timeline: Passez à beat 1
5. Grille: Déplacez à gauche (? ou A)
6. Clavier: Appuyez SPACE pour ajouter
7. Timeline: Passez à beat 2
8. Grille: Déplacez à droite (? ou D)
9. Clavier: Appuyez SPACE pour ajouter
10. Clavier: Appuyez ESC pour sauvegarder
```

## ?? Tous les Raccourcis Clavier

```
Navigation:
  ? / ? ou A / D  ? Déplacer X
  ? / ? ou W / S  ? Déplacer Z
  SPACE           ? Ajouter/Retirer bloc

Timeline:
  P               ? Play/Pause
  Slider          ? Scrubber libre

Actions:
  ESC             ? Sauvegarder
  Clear Beat      ? Vider le beat actuel
```

## ?? Personnalisation

### Changer les dimensions de la grille

Ouvrez `InGameLevelEditor.cs`:
```csharp
[SerializeField] private int gridWidth = 4;  // ? Changer ici (défaut 4)
[SerializeField] private int gridDepth = 3;  // ? Changer ici (défaut 3)
```

### Changer les distances de spawn

```csharp
[SerializeField] private float travelTime = 2f;     // Temps avant arrivée
[SerializeField] private float spawnDistance = 10f; // Distance spawn
```

### Ajouter d'autres types de blocs

Dans `ObstacleBehaviour.cs`, ajoutez un type:
```csharp
public enum ObstacleType
{
    Normal,
    DelayedRush,
    MyNewType  // ? Nouveau type
}
```

Ensuite selectionnez-le dans l'interface UI.

## ?? Dépannage

### "Level Editor not initialized"
? Vérifiez que `Level Data` est assigné dans l'inspecteur

### Les blocs ne s'affichent pas
? Vérifiez:
  - Block Prefab assigné
  - Matériaux assignés
  - Caméra positionnée correctement
  - Renderer sur le préfab

### La musique ne joue pas
? Vérifiez:
  - EventReference valide
  - FMOD installé
  - Mode éditeur activé

### "Save not working"
? Vérifiez:
  - RythmLevelData est un ScriptableObject
  - Pas de fichiers verrouillés
  - Droits d'écriture sur Assets

## ?? Tips & Tricks

1. **Prévisualisez rapidement**: Appuyez P pour lancer la musique et vérifier le timing

2. **Copie rapide**: Éditez un beat, puis copyez manuellement aux beats suivants

3. **Patterns répétitifs**: Éditez le premier pattern, puis dupliquez-le

4. **Sauvegarde régulière**: Appuyez ESC régulièrement

5. **Visualisation**: Utilisez les boutons ±0.5s pour du fine-tuning

6. **Grille grande**: Zoomez avec la molette souris de votre scène

## ?? Méthodes Utiles (Pour Code Custom)

```csharp
// Obtenir la référence
InGameLevelEditor editor = GetComponent<InGameLevelEditor>();

// Timeline
editor.SetTime(5.5f);              // Aller à 5.5 secondes
editor.PlayMusic();                // Lancer musique
editor.PauseMusic();               // Mettre en pause
editor.ToggleMusic();              // Play/Pause

// Édition
editor.AddBlockAtSelectedPosition(ObstacleType.Normal);
editor.RemoveBlockAtSelectedPosition();
editor.ClearCurrentBeat();

// Vérification
if (editor.HasBlockAtSelectedPosition(out GridBlock block))
{
    Debug.Log($"Block: {block.x}, {block.z}");
}

// Stats
int total = editor.CountTotalBlocks();
int inBeat = editor.CountBlocksInBeat(42);

// Sauvegarde
editor.SaveLevel();
```

## ?? Concepts Clés

### Beat
Une unité de temps basée sur le BPM.
- BPM = 120 ? 1 beat = 0.5 secondes
- BPM = 100 ? 1 beat = 0.6 secondes

### Timeline
La position actuelle dans la chanson, mesurée en secondes ou beats.

### Grille
La disposition spatiale (4 colonnes × 3 rangées) où vous placez les blocs.

### RythmLevelData
ScriptableObject qui contient les données de tous les blocs du niveau.

### EventInstance
Instance FMOD d'une musique, utilisée pour le playback et le contrôle du temps.

## ?? Fichiers Connexes

- `RythmLevelData.cs` - Modèle de données du niveau
- `BeatFrame.cs` - Structure d'un beat
- `GridBlock.cs` - Structure d'un bloc
- `LevelPreviewSystem.cs` - Visualisation avancée
- `ObstacleBehaviour.cs` - Comportement des blocs en jeu

## ? Améliorations Futures (Idées)

- [ ] Undo/Redo système
- [ ] Copy/Paste de patterns
- [ ] Grille zoomable
- [ ] Waveform audio visuelle
- [ ] Prédéfinitions de patterns
- [ ] Import/Export JSON
- [ ] Aperçu waveform
- [ ] Support multi-langue
- [ ] Dark mode / Light mode UI
- [ ] Raccourcis clavier personnalisables

## ?? Problèmes Courants

**P: Comment dupliquer un beat?**
A: Éditez beat 1, puis copiez manuellement les blocs au beat 2 (pour l'instant)

**P: Peut-on redimensionner la grille?**
A: Oui, modifiez gridWidth et gridDepth dans InGameLevelEditor.cs

**P: Comment ajouter plus de types de blocs?**
A: Modifiez l'enum ObstacleType et l'UI pour afficher les boutons

**P: Les données se sauvegardent-elles automatiquement?**
A: Non, appuyez ESC ou le bouton "SAVE LEVEL" pour sauvegarder

**P: Peut-on avoir plusieurs niveaux?**
A: Oui, créez plusieurs RythmLevelData ScriptableObjects

## ?? Prêt à Commencer!

1. Suivez les étapes de mise en place rapide
2. Lancez le jeu
3. Cliquez "EDIT LEVEL"
4. Commencez à créer! ??

Bonne édition!

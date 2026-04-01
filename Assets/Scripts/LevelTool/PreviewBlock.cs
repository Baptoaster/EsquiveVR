using System;
using UnityEngine;

/// <summary>
/// Preview block used in editor mode. Moves deterministically based on timeline.
/// Independent from GridBlock/InGameLevelEditor types (uses primitives + delegate).
/// </summary>
[DisallowMultipleComponent]
public class PreviewBlock : MonoBehaviour
{
    private int beatIndex;
    private int gridX;
    private int gridZ;
    private int typeId;
    private Func<int,int,int,float,Vector3> positionFunc;
    private Material normalMat;
    private Material selectedMat;
    private Renderer rend;

    void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    public void Init(int beatIndex, int gridX, int gridZ, int typeId, Func<int,int,int,float,Vector3> positionFunc, Material normalMat, Material selectedMat)
    {
        this.beatIndex = beatIndex;
        this.gridX = gridX;
        this.gridZ = gridZ;
        this.typeId = typeId;
        this.positionFunc = positionFunc;
        this.normalMat = normalMat;
        this.selectedMat = selectedMat;

        // Disable gameplay behaviours if present
        var rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.linearVelocity = Vector3.zero; }

        foreach (var mb in GetComponents<MonoBehaviour>())
        {
            if (mb == this) continue;
            mb.enabled = false;
        }

        UpdateVisual(false);
    }

    public void UpdateForTime(float currentTime, bool isSelected)
    {
        if (positionFunc != null)
        {
            Vector3 pos = positionFunc(gridX, gridZ, beatIndex, currentTime);
            transform.position = pos;
        }

        UpdateVisual(isSelected);
    }

    private void UpdateVisual(bool isSelected)
    {
        if (rend == null) return;

        if (isSelected && selectedMat != null)
        {
            rend.material = selectedMat;
            return;
        }

        if (normalMat != null)
        {
            rend.material = normalMat;
            return;
        }
    }
}

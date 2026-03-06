using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BeatFrame
{
    public int beatIndex;
    public List<GridBlock> blocks = new List<GridBlock>();
}

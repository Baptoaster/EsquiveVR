using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Rhythm/Level Data")]
public class RythmLevelData : ScriptableObject
{
    public float bpm = 120f;
    public int totalBeats = 128;
    public List<BeatFrame> beatFrames = new List<BeatFrame>();

    public BeatFrame GetBeat(int index)
    {
        return beatFrames.Find(b => b.beatIndex == index);
    }

    public void AddBlock(int beatIndex, GridBlock block)
    {
        BeatFrame frame = GetBeat(beatIndex);

        if (frame == null)
        {
            frame = new BeatFrame { beatIndex = beatIndex };
            beatFrames.Add(frame);
        }

        if (!frame.blocks.Exists(b => b.x == block.x && b.z == block.z))
            frame.blocks.Add(block);
    }

    public void RemoveBlock(int beatIndex, int x, int z)
    {
        BeatFrame frame = GetBeat(beatIndex);
        if (frame == null) return;

        frame.blocks.RemoveAll(b => b.x == x && b.z == z);
    }
}

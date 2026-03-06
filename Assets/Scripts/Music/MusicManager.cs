using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [Header("References")]
    public FMODUnity.EventReference fmodEvent;
    public RSE_OnBeat onBeatEvent;
    public RSE_OnObstacleHitPlayer onPlayerDeathEvent;

    private FMOD.Studio.EventInstance instance;

    public BeatSystem bs;

    private void OnEnable()
    {
        bs.OnBeat += HandleBeat;
        onPlayerDeathEvent.Action += HandlePlayerDeath;
        instance = FMODUnity.RuntimeManager.CreateInstance("event:/MainMusic 2");
        bs.AssignBeatEvent(instance);
        instance.start();
    }

    private void OnDisable()
    {
        bs.OnBeat -= HandleBeat;
        onPlayerDeathEvent.Action -= HandlePlayerDeath;
    }

    public void HandlePlayerDeath()
    {
        bs.StopAndClear(instance);
        instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }

    public void HandleBeat()
    {
        Debug.Log("Beat!");
        onBeatEvent.Call();
    }
}

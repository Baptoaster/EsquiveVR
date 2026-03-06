using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

public class GetFMODSpectrumData : MonoBehaviour
{
    [SerializeField] private FMODUnity.EventReference _eventPath;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool downmixToMono = true;
    [SerializeField] RSE_OnObstacleHitPlayer onPlayerDeathEvent;

    public int _windowSize = 512; // bins souhaités
    public FMOD.DSP_FFT_WINDOW_TYPE _windowShape;

    private FMOD.Studio.EventInstance _event;
    private FMOD.ChannelGroup _channelGroup;
    private FMOD.DSP _dsp;
    private FMOD.DSP_PARAMETER_FFT _fftparam;

    public float[] _samples;

    // retry helpers
    private Coroutine _attachCoroutine;
    private bool _dspAttached = false;
    private bool _isShuttingDown = false; //Test
    private const int MAX_ATTACH_ATTEMPTS = 200; // ~20s si wait 0.1s

    private void OnEnable()
    {
        if (onPlayerDeathEvent != null)
            onPlayerDeathEvent.Action += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        if (onPlayerDeathEvent != null)
            onPlayerDeathEvent.Action -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath()
    {
        try
        {
            if (_event.isValid())
            {
                var res = _event.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                if (res != FMOD.RESULT.OK)
                    Debug.LogWarning($"[GetFMODSpectrumData] stop returned {res}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GetFMODSpectrumData] Exception while stopping event: {ex.Message}");
        }
    }

    private void Start()
    {
        // Prepare FMOD event + DSP (attachement asynchrone)
        PrepareFMODeventInstance();

        // on alloue samples au nombre de bins souhaités
        _samples = new float[_windowSize];
    }

    private void OnDestroy()
    {
        _isShuttingDown = true;

        if (_attachCoroutine != null)
            StopCoroutine(_attachCoroutine);

        // retirer le DSP s'il est attaché avant de le release
        try
        {
            if (_dspAttached && !_channelGroup.Equals(default(FMOD.ChannelGroup)))
            {
                var res = _channelGroup.removeDSP(_dsp);
                if (res != FMOD.RESULT.OK)
                    Debug.LogWarning($"[GetFMODSpectrumData] removeDSP returned {res}");
                else
                    _dspAttached = false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GetFMODSpectrumData] Exception while removing DSP: {ex.Message}");
        }

        try { if (!_dsp.Equals(default(FMOD.DSP))) _dsp.release(); } catch (Exception ex) { Debug.LogWarning($"[GetFMODSpectrumData] DSP.release error: {ex.Message}"); }
        try { if (_event.isValid()) { _event.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); _event.release(); } } catch { }
    }

    private void PrepareFMODeventInstance()
    {
        FMOD.RESULT res;

        _event = FMODUnity.RuntimeManager.CreateInstance(_eventPath);

        res = _event.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(gameObject.transform));
        if (res != FMOD.RESULT.OK)
            Debug.LogWarning($"[GetFMODSpectrumData] set3DAttributes => {res}");

        if (playOnStart)
        {
            res = _event.start();
            if (res != FMOD.RESULT.OK)
                Debug.LogWarning($"[GetFMODSpectrumData] EventInstance.start() => {res}");
        }

        // create DSP FFT
        res = FMODUnity.RuntimeManager.CoreSystem.createDSPByType(FMOD.DSP_TYPE.FFT, out _dsp);
        if (res != FMOD.RESULT.OK || _dsp.Equals(default(FMOD.DSP)))
        {
            Debug.LogError($"[GetFMODSpectrumData] createDSPByType failed: {res}");
            return;
        }

        // set window shape and window size (FMOD expects windowsize = bins * 2)
        _dsp.setParameterInt((int)FMOD.DSP_FFT.WINDOW, (int)_windowShape);
        _dsp.setParameterInt((int)FMOD.DSP_FFT.WINDOWSIZE, _windowSize * 2);

        // optionally downmix to mono to simplify handling
        if (downmixToMono)
        {
            _dsp.setParameterInt((int)FMOD.DSP_FFT.DOWNMIX, (int)FMOD.DSP_FFT_DOWNMIX_TYPE.MONO);
        }

        // attach asynchronously: start a coroutine to wait for the channel group to be ready
        if (_attachCoroutine != null)
            StopCoroutine(_attachCoroutine);
        _attachCoroutine = StartCoroutine(AttachDSPWhenReady());
    }

    private IEnumerator AttachDSPWhenReady()
    {
        int attempts = 0;
        while (!_isShuttingDown && attempts < MAX_ATTACH_ATTEMPTS)
        {
            attempts++;

            var res = _event.getChannelGroup(out _channelGroup);
            if (res == FMOD.RESULT.OK && !_channelGroup.Equals(default(FMOD.ChannelGroup)))
            {
                var addRes = _channelGroup.addDSP(0, _dsp);
                if (addRes == FMOD.RESULT.OK)
                {
                    _dspAttached = true;
                    Debug.Log("[GetFMODSpectrumData] DSP attached to channel group");
                    yield break;
                }
                else
                {
                    Debug.LogWarning($"[GetFMODSpectrumData] channelGroup.addDSP returned {addRes}");
                    // si échec, on retryera
                }
            }
            else
            {
                // message utile une seule fois
                if (attempts == 1)
                    Debug.LogWarning($"[GetFMODSpectrumData] getChannelGroup returned {res} - will retry until channel group is available");
            }

            yield return new WaitForSeconds(0.1f);
        }

        Debug.LogWarning("[GetFMODSpectrumData] Failed to attach DSP: channel group not available in time.");
    }

    private void Update()
    {
        GetSpectrumData();
    }

    private void GetSpectrumData()
    {
        if (_dsp.Equals(default(FMOD.DSP))) return;

        IntPtr dataPtr;
        uint length;
        FMOD.RESULT res;

        int parameterIndex = (int)FMOD.DSP_FFT.SPECTRUMDATA;

        res = _dsp.getParameterData(parameterIndex, out dataPtr, out length);
        if (res != FMOD.RESULT.OK || dataPtr == IntPtr.Zero)
        {
            // si le channel group n'était pas prêt, la coroutine gère le ré-attachement
            return;
        }

        try
        {
            _fftparam = Marshal.PtrToStructure<FMOD.DSP_PARAMETER_FFT>(dataPtr);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GetFMODSpectrumData] Marshal failed: {ex.Message}");
            return;
        }

        if (_fftparam.numchannels == 0 || _fftparam.length == 0)
        {
            // rien à lire
            return;
        }

        int binsToRead = Math.Min(_windowSize, _fftparam.length);

        if (_samples == null || _samples.Length != _windowSize)
            _samples = new float[_windowSize];

        for (int s = 0; s < binsToRead; s++)
        {
            float total = 0f;
            for (int c = 0; c < _fftparam.numchannels; c++)
            {
                try
                {
                    total += _fftparam.spectrum[c][s];
                }
                catch { }
            }
            _samples[s] = total / _fftparam.numchannels;
        }

        for (int s = binsToRead; s < _windowSize; s++)
            _samples[s] = 0f;
    }
}

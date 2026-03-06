using FMOD;
using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class FMODAudioVisualizer : MonoBehaviour
{
    [Header("Game Performance")]
    [SerializeField] private int fps = 0;
    [Header("FMOD Event")]
    [SerializeField] private FMODUnity.EventReference eventPath;
    [SerializeField] private bool playOnAwake = true;
    [Header("Audio Sample Data Settings")]
    [SerializeField] private int windowSize = 512;
    [SerializeField] private FMOD.DSP_FFT_WINDOW_TYPE windowShape;
    [Header("Select Metering Object Prefab")]
    [SerializeField] private GameObject MeterObject = null;
    [SerializeField] private float meterIntensity = 10f;
    [SerializeField] private float SpaceBetweenMeters = 0.5f;
    [Header("Meter Speed Settings")]
    [SerializeField] private float bufferStartSpeed = 0.005f;
    [SerializeField] private float bufferAccelRate = 1.2f;
    [Header("Limits")]
    [SerializeField, Tooltip("Valeur max acceptée pour une bande avant clipping")] private float maxBandValue = 5f;
    [SerializeField, Tooltip("Valeur max du decrease step pour éviter décroissance trop violente")] private float maxBufferDecrease = 0.5f;
    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] RSE_OnObstacleHitPlayer onPlayerDeathEvent;

    // expose a static read-only view of the current configured maxBandValue
    private static float _maxBandValueStatic = 5f;
    public static float MaxBandValue => _maxBandValueStatic;

    [Header("Internal")]
    private List<float> freqRanges = new List<float>();
    private int numSampleInFirstBand = 1;

    private EventInstance _event;
    private ChannelGroup channelGroup;
    private DSP DSPFFT;
    private DSP_PARAMETER_FFT fftparam;

    private GameObject[] bandMeters;

    [SerializeField] private float[] _samples;
    public static float[] freqBands = new float[7];
    [SerializeField] public static float[] bandBuffer = new float[7];
    private float[] bufferDecrease = new float[freqBands.Length];

    private float time = 0f;
    private int frameCount = 0;

    // DSP attach retry
    private Coroutine _attachCoroutine;
    private bool _dspAttached = false;
    private bool _isShuttingDown = false;
    private const int MAX_ATTACH_ATTEMPTS = 200;

    private void OnEnable()
    {
        if (onPlayerDeathEvent != null)
        {
            onPlayerDeathEvent.Action += HandlePlayerDeath;
        }
    }

    private void OnDisable()
    {
        if (onPlayerDeathEvent != null)
        {
            onPlayerDeathEvent.Action -= HandlePlayerDeath;
        }
    }

    private void HandlePlayerDeath()
    {
        try
        {
            if (_event.isValid())
            {
                var res = _event.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                if (res != FMOD.RESULT.OK && debugLogs)
                    UnityEngine.Debug.LogWarning($"[FMODAudioVisualizer] stop returned {res}");
            }
        }
        catch (Exception ex)
        {
            if (debugLogs) UnityEngine.Debug.LogWarning($"[FMODAudioVisualizer] Exception while stopping event: {ex.Message}");
        }
    }

    private void Start()
    {
        // Make sure the static mirrors the serialized value
        _maxBandValueStatic = maxBandValue;

        // Prepare FMOD event (creates dsp later)
        PrepareFMODeventInstance();

        // find out how many meters are needed
        SetNumberOfMeters();

        // allocate arrays BEFORE spawning objects so ParamCube can read correct sizes
        _samples = new float[windowSize];
        freqBands = new float[freqRanges.Count];
        bandBuffer = new float[freqRanges.Count];
        bufferDecrease = new float[freqRanges.Count];

        // spawn meters.
        SpawnMeterObjects();
    }

    private void OnValidate()
    {
        // ensure static value updated in editor when changing the serialized limit
        _maxBandValueStatic = maxBandValue;
    }

    private void OnDestroy()
    {
        _isShuttingDown = true;
        if (_attachCoroutine != null)
            StopCoroutine(_attachCoroutine);

        // remove DSP if attached
        try
        {
            if (_dspAttached && !channelGroup.Equals(default(ChannelGroup)))
            {
                var res = channelGroup.removeDSP(DSPFFT);
                if (res != FMOD.RESULT.OK && debugLogs)
                    UnityEngine.Debug.LogWarning($"[FMODAudioVisualizer] removeDSP returned {res}");
                _dspAttached = false;
            }
        }
        catch (Exception ex)
        {
            if (debugLogs) UnityEngine.Debug.LogWarning($"[FMODAudioVisualizer] Exception while removing DSP: {ex.Message}");
        }

        try { if (!DSPFFT.Equals(default(DSP))) DSPFFT.release(); } catch (Exception ex) { if (debugLogs) UnityEngine.Debug.LogWarning($"[FMODAudioVisualizer] DSP.release error: {ex.Message}"); }
        try { if (_event.isValid()) { _event.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); _event.release(); } } catch { }
    }

    private void PrepareFMODeventInstance()
    {
        FMOD.RESULT res;
        _event = RuntimeManager.CreateInstance(eventPath);

        res = _event.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject.transform));
        if (res != FMOD.RESULT.OK && debugLogs) UnityEngine.Debug.LogWarning($"[FMODAudioVisualizer] set3DAttributes => {res}");

        if (playOnAwake)
        {
            res = _event.start();
            if (res != FMOD.RESULT.OK && debugLogs) UnityEngine.Debug.LogWarning($"[FMODAudioVisualizer] EventInstance.start() => {res}");
        }

        // create DSP FFT
        res = RuntimeManager.CoreSystem.createDSPByType(DSP_TYPE.FFT, out DSPFFT);
        if (res != FMOD.RESULT.OK || DSPFFT.Equals(default(DSP)))
        {
            UnityEngine.Debug.LogError($"[FMODAudioVisualizer] createDSPByType failed: {res}");
            return;
        }

        DSPFFT.setParameterInt((int)DSP_FFT.WINDOW, (int)windowShape);
        DSPFFT.setParameterInt((int)DSP_FFT.WINDOWSIZE, windowSize * 2);
        // Optionnel : downmix en mono pour simplifier (activez si besoin)
        DSPFFT.setParameterInt((int)DSP_FFT.DOWNMIX, (int)DSP_FFT_DOWNMIX_TYPE.MONO);

        // try immediate attach, else start coroutine to wait for channelGroup
        var getRes = _event.getChannelGroup(out channelGroup);
        if (getRes == FMOD.RESULT.OK && !channelGroup.Equals(default(ChannelGroup)))
        {
            var addRes = channelGroup.addDSP(0, DSPFFT);
            if (addRes == FMOD.RESULT.OK)
            {
                _dspAttached = true;
                if (debugLogs) UnityEngine.Debug.Log("[FMODAudioVisualizer] DSP attached immediately.");
            }
            else
            {
                if (debugLogs) UnityEngine.Debug.LogWarning($"[FMODAudioVisualizer] channelGroup.addDSP returned {addRes} - will retry via coroutine");
                if (_attachCoroutine != null) StopCoroutine(_attachCoroutine);
                _attachCoroutine = StartCoroutine(AttachDSPWhenReady());
            }
        }
        else
        {
            if (debugLogs) UnityEngine.Debug.LogWarning($"[FMODAudioVisualizer] getChannelGroup returned {getRes} - will retry via coroutine");
            if (_attachCoroutine != null) StopCoroutine(_attachCoroutine);
            _attachCoroutine = StartCoroutine(AttachDSPWhenReady());
        }
    }

    private IEnumerator AttachDSPWhenReady()
    {
        int attempts = 0;
        while (!_isShuttingDown && attempts < MAX_ATTACH_ATTEMPTS)
        {
            attempts++;
            var res = _event.getChannelGroup(out channelGroup);
            if (res == FMOD.RESULT.OK && !channelGroup.Equals(default(ChannelGroup)))
            {
                var addRes = channelGroup.addDSP(0, DSPFFT);
                if (addRes == FMOD.RESULT.OK)
                {
                    _dspAttached = true;
                    if (debugLogs) UnityEngine.Debug.Log("[FMODAudioVisualizer] DSP attached to channel group (coroutine).");
                    yield break;
                }
                else
                {
                    if (debugLogs) UnityEngine.Debug.LogWarning($"[FMODAudioVisualizer] channelGroup.addDSP returned {addRes}");
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
        if (debugLogs) UnityEngine.Debug.LogWarning("[FMODAudioVisualizer] Failed to attach DSP: channel group not available in time.");
    }

    private void SpawnMeterObjects()
    {
        if (MeterObject == null)
        {
            if (debugLogs) UnityEngine.Debug.LogWarning("[FMODAudioVisualizer] MeterObject prefab not assigned.");
            return;
        }

        int count = freqRanges.Count;
        bandMeters = new GameObject[count];

        // distance between consecutive meters (preserve previous behaviour where step = 1 + SpaceBetweenMeters)
        float step = 1f + SpaceBetweenMeters;

        // center the whole row on the transform
        float totalWidth = (count - 1) * step;
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < count; i++)
        {
            // instantiate as child so localPosition is in the local space of this transform (rotation honored)
            bandMeters[i] = Instantiate(MeterObject, transform);

            // place meters along local X axis; transform (parent) rotation will orient them in world space
            Vector3 localPos = new Vector3(startX + i * step, 0f, 0f);
            bandMeters[i].transform.localPosition = localPos;

            // keep meter upright relative to the parent (no additional local rotation)
            bandMeters[i].transform.localRotation = Quaternion.identity;

            var pc = bandMeters[i].GetComponent<ParamCube>();
            if (pc != null)
            {
                // use the band index directly
                pc._band = i;
            }
        }
    }

    private void Update()
    {
        GetSpectrumData();
        FrequencyBands();
        BandBuffer();
        countFPS();
    }

    private void GetSpectrumData()
    {
        if (DSPFFT.Equals(default(DSP))) return;

        System.IntPtr data;
        uint length;
        int parameterIndex = (int)DSP_FFT.SPECTRUMDATA;

        var res = DSPFFT.getParameterData(parameterIndex, out data, out length);
        if (res != FMOD.RESULT.OK || data == System.IntPtr.Zero)
        {
            // not ready yet; coroutine handles reattach
            return;
        }

        try
        {
            fftparam = (DSP_PARAMETER_FFT)Marshal.PtrToStructure(data, typeof(DSP_PARAMETER_FFT));
        }
        catch (Exception ex)
        {
            if (debugLogs) UnityEngine.Debug.LogError($"[FMODAudioVisualizer] Marshal failed: {ex.Message}");
            return;
        }

        if (fftparam.numchannels == 0 || fftparam.length == 0)
        {
            // nothing to read yet
            return;
        }

        int binsToRead = Mathf.Min(windowSize, fftparam.length);

        if (_samples == null || _samples.Length != windowSize)
            _samples = new float[windowSize];

        for (int b = 0; b < binsToRead; b++)
        {
            float totalChannelData = 0f;
            for (int c = 0; c < fftparam.numchannels; c++)
            {
                try
                {
                    float val = fftparam.spectrum[c][b];
                    if (float.IsNaN(val) || float.IsInfinity(val)) val = 0f;
                    totalChannelData += val;
                }
                catch
                {
                    // ignore individual read errors
                }
            }
            _samples[b] = totalChannelData / Math.Max(1, fftparam.numchannels);
        }

        for (int b = binsToRead; b < windowSize; b++)
            _samples[b] = 0f;

        if (debugLogs)
        {
            // utile pour debug ponctuel : affiche la somme des premiers bins
            float sum = 0f;
            for (int i = 0; i < Mathf.Min(8, _samples.Length); i++) sum += _samples[i];
            UnityEngine.Debug.Log($"[FMODAudioVisualizer] sample[0..7] sum={sum}");
        }
    }

    private void SetNumberOfMeters()
    {
        float singleSizeOfOneSample = 22050f / windowSize;
        float HzForFirstBand = singleSizeOfOneSample;

        while (HzForFirstBand < 60f)
        {
            numSampleInFirstBand++;
            HzForFirstBand += singleSizeOfOneSample;
        }

        freqRanges.Clear();
        freqRanges.Add(HzForFirstBand);
        float hzRange = HzForFirstBand;
        float hzSize = HzForFirstBand;
        while (hzRange < 22050f)
        {
            hzSize *= 2;
            hzRange += hzSize;
            if (hzRange < 22050f)
                freqRanges.Add(hzRange);
        }
    }

    private void FrequencyBands()
    {
        int counter = 0;
        for (int i = 0; i < freqRanges.Count; i++)
        {
            float sum = 0f;
            int numSampleInThisBand = numSampleInFirstBand * (int)Mathf.Pow(2, i);

            int actualSamples = Mathf.Min(numSampleInThisBand, _samples.Length - counter);
            if (actualSamples <= 0)
            {
                freqBands[i] = 0f;
                continue;
            }

            for (int j = 0; j < actualSamples; j++)
            {
                float s = _samples[counter];
                if (float.IsNaN(s) || float.IsInfinity(s)) s = 0f;
                sum += s;
                counter++;
            }

            float average = sum / actualSamples;
            if (float.IsNaN(average) || float.IsInfinity(average)) average = 0f;

            // clamp pour éviter valeurs aberrantes
            float bandValue = Mathf.Clamp(average * meterIntensity, 0f, maxBandValue);
            freqBands[i] = bandValue;
        }
    }

    private void BandBuffer()
    {
        // Use time-based decrement and linear acceleration for buffer decrease.
        // This removes frame-rate dependence and prevents exponential runaway.
        for (int i = 0; i < freqRanges.Count; i++)
        {
            // safety: ensure arrays sized correctly
            if (i >= bandBuffer.Length || i >= bufferDecrease.Length) continue;

            if (freqBands[i] > bandBuffer[i])
            {
                // new peak, set buffer to that peak (capped)
                bandBuffer[i] = Mathf.Min(freqBands[i], maxBandValue);
                // ensure a non-zero starting decrease velocity
                bufferDecrease[i] = Mathf.Max(bufferStartSpeed, 1e-6f);
            }
            else if (freqBands[i] < bandBuffer[i])
            {
                // decrease based on current velocity, frame-rate independent
                float decreaseStep = bufferDecrease[i] * Time.deltaTime;
                // tiny fallback step so we never truly get stuck if something went wrong
                decreaseStep = Mathf.Max(decreaseStep, 1e-6f * Time.deltaTime);

                bandBuffer[i] -= decreaseStep;

                // linear acceleration (per second), capped by configured maxBufferDecrease
                bufferDecrease[i] = Mathf.Min(bufferDecrease[i] + bufferAccelRate * Time.deltaTime, maxBufferDecrease);
            }

            // safety clamps: never negative, never above configured max
            if (bandBuffer[i] < 0)
                bandBuffer[i] = 0f;

            bandBuffer[i] = Mathf.Clamp(bandBuffer[i], 0f, maxBandValue);
        }
    }

    private void countFPS()
    {
        time += Time.deltaTime;
        if (time > 1f)
        {
            time = 0f;
            fps = 0;
            fps += frameCount;
            frameCount = 0;
        }
        frameCount++;
    }

    public void PlayFMODEvent()
    {
        _event.start();
    }

    public void StopFMODEvent()
    {
        _event.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }

    public void PauseFMODEvent()
    {
        bool p = false;
        _event.getPaused(out p);
        p = !p;
        _event.setPaused(p);
    }
}

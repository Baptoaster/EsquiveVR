using UnityEngine;

public class LightningFlickering : MonoBehaviour
{
    [Header("Sources (FMOD preferred)")]
    [Tooltip("Si présent, le script lira les samples FMOD via GetFMODSpectrumData._samples")]
    [SerializeField] private GetFMODSpectrumData fmodSpectrumSource;
    [SerializeField]
    public AudioSource audioSource; // fallback si pas de FMOD

    [Header("Renderer / Emission")]
    [SerializeField]
    private Renderer targetRenderer;
    [SerializeField]
    private Color emissiveColor = Color.white;
    [SerializeField]
    [Range(1, 500)]
    private float sensitivity = 100f;
    [SerializeField]
    [Range(0f, 10f)]
    private float smoothing = 1f; // utilisé comme vitesse de convergence (plus grand => réactif)
    [SerializeField]
    private float threshold = 0.01f;
    [SerializeField]
    private string emissionPropertyName = "_EmissionColor";
    [SerializeField]
    private float defaultIntensity = 0f;

    private Material material;
    private int emissionPropertyID;
    private float currentIntensity;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            Debug.LogWarning($"{nameof(LightningFlickering)}: targetRenderer est null sur {gameObject.name}");
            return;
        }

        // Utiliser une instance du matériau pour éviter de modifier le sharedMaterial
        material = targetRenderer.material;
        emissionPropertyID = Shader.PropertyToID(emissionPropertyName);
    }

    private void Update()
    {
        if (material == null)
            return;

        float audioAverage = 0f;
        bool haveData = false;

        // Priorité FMOD samples si fournis
        if (fmodSpectrumSource != null && fmodSpectrumSource._samples != null && fmodSpectrumSource._samples.Length > 0)
        {
            var samples = fmodSpectrumSource._samples;
            double sum = 0.0;
            int count = samples.Length;

            for (int i = 0; i < count; i++)
            {
                float s = samples[i];
                if (float.IsNaN(s) || float.IsInfinity(s)) s = 0f;
                sum += s;
            }

            audioAverage = (float)(sum / Mathf.Max(1, count));
            haveData = true;
        }
        else if (audioSource != null)
        {
            // fallback : comportement précédent (Unity AudioSource)
            const int defaultSize = 1024;
            float[] spectrumData = new float[defaultSize];
            audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

            double sum = 0.0;
            for (int i = 0; i < spectrumData.Length; i++)
            {
                float s = spectrumData[i];
                if (float.IsNaN(s) || float.IsInfinity(s)) s = 0f;
                sum += s;
            }

            audioAverage = (float)(sum / Mathf.Max(1, spectrumData.Length));
            haveData = true;
        }

        if (!haveData)
        {
            // pas de source => on réduit doucement vers defaultIntensity
            float targetIntensity = defaultIntensity;
            currentIntensity = DampToward(currentIntensity, targetIntensity, smoothing);
            material.SetColor(emissionPropertyID, emissiveColor * currentIntensity);
            return;
        }

        if (audioAverage > threshold)
        {
            float targetIntensity = audioAverage * sensitivity;
            currentIntensity = DampToward(currentIntensity, targetIntensity, smoothing);
        }
        else
        {
            currentIntensity = DampToward(currentIntensity, defaultIntensity, smoothing);
        }

        material.SetColor(emissionPropertyID, emissiveColor * currentIntensity);
    }

    // Simple helper pour une interpolation dépendante du temps : vitesse de convergence = smooth
    private float DampToward(float current, float target, float smooth)
    {
        // si smooth <= 0 : aller instantanément
        if (smooth <= 0f) return target;
        // factor par frame (exponential-like): 1 - exp(-smooth * dt)
        float factor = 1f - Mathf.Exp(-smooth * Time.deltaTime);
        return Mathf.Lerp(current, target, factor);
    }

    private void OnDestroy()
    {
        if (material != null)
        {
            Destroy(material);
        }
    }

}

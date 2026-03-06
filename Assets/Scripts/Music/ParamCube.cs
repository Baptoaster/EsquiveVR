using UnityEngine;

public class ParamCube : MonoBehaviour
{
    [HideInInspector] public int _band;
    [SerializeField] private float _startScale = 1f;
    [SerializeField] private float _scaleMultipler = 10f;

    private float startPos;
    private float previousPos;

    // Start is called before the first frame update
    void Start()
    {
        startPos = transform.position.y;
        previousPos = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        // safety: ensure band index valid
        float rawBandValue = 0f;
        if (FMODAudioVisualizer.bandBuffer != null && _band >= 0 && _band < FMODAudioVisualizer.bandBuffer.Length)
        {
            rawBandValue = FMODAudioVisualizer.bandBuffer[_band];
            if (float.IsNaN(rawBandValue) || float.IsInfinity(rawBandValue)) rawBandValue = 0f;
        }

        // clamp to the visualizer's configured maximum value
        float clampedBandValue = Mathf.Clamp(rawBandValue, 0f, FMODAudioVisualizer.MaxBandValue);

        // compute resize amount and clamp final scale so it cannot grow beyond start + max * multiplier
        float resizeAmount = clampedBandValue * _scaleMultipler;
        float finalYScale = Mathf.Clamp(_startScale + resizeAmount, _startScale, _startScale + FMODAudioVisualizer.MaxBandValue * _scaleMultipler);

        transform.position = new Vector3(transform.position.x, startPos, transform.position.z);
        transform.localScale = new Vector3(transform.localScale.x, finalYScale, transform.localScale.z);
        transform.position = new Vector3(transform.position.x, transform.position.y + (finalYScale - _startScale) / 2f, transform.position.z);
    }
}

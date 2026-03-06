using FMODUnity;
using TMPro;
using UnityEngine;

public class DeathBehaviour : MonoBehaviour
{
    [SerializeField] RSE_OnObstacleHitPlayer onPlayerDeath;

    [SerializeField] GameObject deathGameObject;
    [SerializeField] Material deathGroundMaterial;
    [SerializeField] Material deathSkyboxMaterial;
    [SerializeField] TextMeshPro deathText;
    [SerializeField] StudioEventEmitter deathSound;

    [SerializeField, Range(0f, 1f)] float alphaGround;
    [SerializeField, Range(0f, 1f)] float alphaSkybox;

    string cachedCircleMaxPropertyName;

    private void OnEnable()
    {
        onPlayerDeath.Action += OnDeath;
    }

    private void OnDestroy()
    {
        onPlayerDeath.Action -= OnDeath;
    }

    public void OnDeath()
    {
        deathGameObject.SetActive(true);
        deathSound.Play();
    }
}

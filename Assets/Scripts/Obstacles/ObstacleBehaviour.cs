using MoreMountains.Feedbacks;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.Processors;

public class ObstacleBehaviour : MonoBehaviour
{
    [SerializeField] RSE_OnObstacleHitPlayer onObstacleHitPlayer;
    public RSE_OnBeat onBeat;
    public MMF_Player beatFeedbacks;
    public MMF_Player hitFeedbacks;

    [Header("Movement")]
    [SerializeField, Tooltip("Vitesse de déplacement le long du forward local (unités/sec).")]
    private float moveSpeed = 5f;
    [SerializeField, Tooltip("Si vrai et qu'un Rigidbody est présent, on utilisera la physique (rb.velocity). Sinon, on déplacera le Transform.")]
    private bool preferRigidbodyMovement = true;

    private Rigidbody _rb;
    private bool isDead = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // If preferRigidbodyMovement but no Rigidbody, fall back to transform movement.
        if (preferRigidbodyMovement && _rb == null)
        {
            preferRigidbodyMovement = false;
        }
    }

    private void OnEnable()
    {
        StartCoroutine(WaitForSpawn());
    }

    private void OnDisable()
    {
        if (onBeat != null)
            onBeat.Action -= HandleBeat;
    }

    public void HandleBeat()
    {
        if (beatFeedbacks != null)
            beatFeedbacks.PlayFeedbacks();
    }

    private void Update()
    {
        // transform-based movement when no Rigidbody or when not preferring Rigidbody
        if (!preferRigidbodyMovement)
        {
            transform.position += transform.up * moveSpeed * Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        // physics-based movement: set velocity to move along local forward
        if (preferRigidbodyMovement && _rb != null)
        {
            _rb.linearVelocity = transform.forward * moveSpeed;
        }
    }

    IEnumerator WaitForSpawn()
    {
        yield return new WaitForSeconds(0.6f);
        if (onBeat != null)
            onBeat.Action += HandleBeat;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            if (onObstacleHitPlayer != null)
            {
                if(isDead) return;
                onObstacleHitPlayer.Call();
                hitFeedbacks?.PlayFeedbacks();
                isDead = true;
            }
            Debug.Log("Player hit an obstacle!");
        }
    }

    // optionnel : exposer la vitesse en lecture à l'exécution
    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }
}

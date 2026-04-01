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

    [Header("Type")]
    public ObstacleType obstacleType;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private bool preferRigidbodyMovement = true;

    [Header("Delayed Rush Settings")]
    [SerializeField] private int beatsBeforeRush = 4;
    [SerializeField] private float idleSpeed = 0.5f;
    [SerializeField] private float rushSpeed = 10f;

    private Rigidbody _rb;
    private bool isDead = false;

    private int currentBeatCount = 0;
    private bool isRushing = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        if (preferRigidbodyMovement && _rb == null)
            preferRigidbodyMovement = false;
    }

    private void OnEnable()
    {
        onObstacleHitPlayer.Action += OnDeath;
        StartCoroutine(WaitForSpawn());
    }

    private void OnDisable()
    {
        onObstacleHitPlayer.Action -= OnDeath;
        if (onBeat != null)
            onBeat.Action -= HandleBeat;
    }

    IEnumerator WaitForSpawn()
    {
        yield return new WaitForSeconds(0.6f);

        if (onBeat != null)
            onBeat.Action += HandleBeat;

        InitBehaviour();
    }

    void InitBehaviour()
    {
        if (obstacleType == ObstacleType.DelayedRush)
        {
            moveSpeed = idleSpeed;
            isRushing = false;
            currentBeatCount = 0;
        }
    }

    public void HandleBeat()
    {
        beatFeedbacks?.PlayFeedbacks();

        if (obstacleType == ObstacleType.DelayedRush)
        {
            currentBeatCount++;

            if (!isRushing && currentBeatCount >= beatsBeforeRush)
            {
                StartRush();
            }
        }
    }

    void StartRush()
    {
        isRushing = true;
        moveSpeed = rushSpeed;

        // Optionnel : feedback visuel/son
        Debug.Log("RUSH !");
    }

    private void Update()
    {
        if (!preferRigidbodyMovement)
        {
            transform.position += transform.up * moveSpeed * Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        if (preferRigidbodyMovement && _rb != null)
        {
            _rb.linearVelocity = transform.forward * moveSpeed;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            if (onObstacleHitPlayer != null)
            {
                if (isDead) return;

                onObstacleHitPlayer.Call();
                hitFeedbacks?.PlayFeedbacks();
                isDead = true;
            }

            Debug.Log("Player hit an obstacle!");
        }
    }

    private void OnDeath()
    {
        hitFeedbacks?.PlayFeedbacks();
    }
}

public enum ObstacleType
{
    Normal,
    DelayedRush
}
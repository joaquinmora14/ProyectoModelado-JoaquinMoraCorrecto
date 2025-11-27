using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class AgentScript : MonoBehaviour
{
    private NavMeshAgent agent;
    private Rigidbody rb;

    [Header("Patrullaje")]
    [SerializeField] private List<Transform> targets = new List<Transform>();
    private int currentTargetIndex = 0;
    [SerializeField] private float reachThreshold = 0.5f;
    private bool isChasing = false;
    private bool finishedPatrol = false;

    [Header("Animación")]
    [SerializeField] private Animator anim;

    [Header("Detección del jugador")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField, Range(0f, 180f)] private float detectionAngle = 40f;
    [SerializeField] private LayerMask detectionMask = ~0;
    [SerializeField] private float eyeHeight = 1.6f;

    [Header("Captura")]
    [SerializeField] private float catchDistance = 1f;
    private bool gameOverTriggered = false;

    [Header("Persecución")]
    [SerializeField] private float loseSightTime = 2f;
    private float lastSeenTime = Mathf.NegativeInfinity;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (targets.Count > 0)
        {
            finishedPatrol = false;
            agent.SetDestination(targets[currentTargetIndex].position);
        }
        else
        {
            finishedPatrol = true;
        }
    }

    private void Update()
    {
        if (gameOverTriggered) return;

        // PROTECCIÓN ANTI-BUG: evita errores cuando el enemigo está fuera del NavMesh
        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        // ATRAPAR JUGADOR
        if (player != null && Vector3.Distance(transform.position, player.position) <= catchDistance)
        {
            GameOver();
            return;
        }

        if (!isChasing)
        {
            DetectPlayer();
            Patrol();
        }
        else
        {
            HandleChaseBehavior();
        }

        if (anim != null)
            anim.SetFloat("Speed", agent.velocity.magnitude);
    }

    // ----------------------------------------
    //  PERSECUCIÓN CORREGIDA
    // ----------------------------------------
    private void HandleChaseBehavior()
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;

        if (player == null) return;

        if (CanSeePlayer())
        {
            lastSeenTime = Time.time;
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            if (Time.time - lastSeenTime > loseSightTime)
            {
                FullReset();
            }
        }
    }

    // ----------------------------------------
    //  DETECCIÓN DE VISIÓN
    // ----------------------------------------
    private bool CanSeePlayer()
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 toPlayer = player.position - origin;

        if (toPlayer.magnitude > detectionRange)
            return false;

        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > detectionAngle)
            return false;

        if (Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, detectionRange, detectionMask))
        {
            return hit.transform == player || hit.collider.CompareTag("Player");
        }

        return false;
    }

    // ----------------------------------------
    //  PATRULLA
    // ----------------------------------------
    private void Patrol()
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;
        if (finishedPatrol) return;
        if (targets.Count == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= reachThreshold)
        {
            currentTargetIndex++;

            if (currentTargetIndex >= targets.Count)
                currentTargetIndex = 0;

            agent.SetDestination(targets[currentTargetIndex].position);
        }
    }

    // ----------------------------------------
    //  FULL RESET — ARREGLA TODO
    // ----------------------------------------
    public void FullReset()
    {
        // Debug.Log("FULL RESET");

        isChasing = false;
        finishedPatrol = false;
        gameOverTriggered = false;

        // Resetear Rigidbody
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Resetear NavMeshAgent
        agent.enabled = false;

        // Reposicionar al navmesh más cercano
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 3f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }

        agent.enabled = true;
        agent.isStopped = false;
        agent.ResetPath();

        // Reset animación
        if (anim != null)
            anim.SetFloat("Speed", 0);

        // Reset patrullaje
        currentTargetIndex = Random.Range(0, targets.Count);
        agent.SetDestination(targets[currentTargetIndex].position);
    }

    // ----------------------------------------
    //  DETECTAR JUGADOR
    // ----------------------------------------
    private void DetectPlayer()
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;
        if (player == null) return;

        if (CanSeePlayer())
        {
            isChasing = true;
            finishedPatrol = true;
            agent.isStopped = false;
            agent.SetDestination(player.position);
            lastSeenTime = Time.time;
        }
    }

    // ----------------------------------------
    //  GAME OVER
    // ----------------------------------------
    private void OnTriggerEnter(Collider other)
    {
        if (gameOverTriggered) return;
        if (other.CompareTag("Player"))
            GameOver();
    }

    private void GameOver()
    {
        if (gameOverTriggered) return;
        gameOverTriggered = true;
        SceneManager.LoadScene("GameOverScene");
    }

    // ----------------------------------------
    //  GIZMOS
    // ----------------------------------------
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, detectionRange);

        Vector3 forward = transform.forward;
        Quaternion leftRot = Quaternion.Euler(0, -detectionAngle, 0);
        Quaternion rightRot = Quaternion.Euler(0, detectionAngle, 0);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(origin, leftRot * forward * detectionRange);
        Gizmos.DrawRay(origin, rightRot * forward * detectionRange);
    }
}
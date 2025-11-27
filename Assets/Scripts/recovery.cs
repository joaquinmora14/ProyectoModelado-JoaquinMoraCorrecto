using UnityEngine;
using UnityEngine.AI;

public class recovery : MonoBehaviour
{
    private NavMeshAgent agent;
    private Rigidbody rb;

    public float snapDistance = 3f;     // hasta donde busca piso válido
    public float checkDelay = 0.1f;     // cada cuánto revisa
    private float nextCheck = 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + checkDelay;

        // si el agent está apagado o el rigidbody está en movimiento → no hacer nada
        if (!agent.enabled) return;
        if (rb.velocity.sqrMagnitude > 0.1f) return;

        NavMeshHit hit;
        // intenta encontrar un punto válido de navmesh cerca
        if (NavMesh.SamplePosition(transform.position, out hit, snapDistance, NavMesh.AllAreas))
        {
            // si la posición no coincide con el NavMesh → lo warpamos
            if (Vector3.Distance(transform.position, hit.position) > 0.1f)
            {
                agent.Warp(hit.position);
            }
        }
    }
}
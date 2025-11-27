using UnityEngine;
using UnityEngine.AI;

public class fix : MonoBehaviour
{
    private NavMeshAgent agent;
    private Rigidbody rb;

    public float snapDistance = 2f;   // cuanto busca navmesh
    public float checkInterval = 0.2f;
    private float nextCheck = 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + checkInterval;

        // No corregir mientras está siendo empujado
        if (rb.velocity.sqrMagnitude > 0.1f) return;

        // Si el agent está apagado, no corregimos
        if (!agent.enabled) return;

        NavMeshHit hit;

        // Busca navmesh cerca
        if (NavMesh.SamplePosition(transform.position, out hit, snapDistance, NavMesh.AllAreas))
        {
            float dist = Vector3.Distance(transform.position, hit.position);

            if (dist > 0.05f)   // si está fuera del mesh
            {
                agent.Warp(hit.position);  // lo pone EXACTAMENTE en el mesh
            }
        }
    }
}
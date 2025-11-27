using UnityEngine;
using UnityEngine.AI;

public class playerKnockback : MonoBehaviour
{
    [Header("Camera")]
    public Camera cam;

    [Header("Raycast Settings")]
    public float maxDistance = 100f;
    public LayerMask enemyMask;

    [Header("Knockback")]
    public float force = 20f;
    public float upForce = 1f;
    public float minVelocityToReenable = 0.2f; 
    public float maxNavSearchDistance = 3f;    

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryKnockbackEnemy();
        }
    }

    void TryKnockbackEnemy()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, enemyMask))
        {
            Rigidbody rb = hit.collider.attachedRigidbody;
            if (rb == null) return;

            Vector3 dir = (hit.collider.transform.position - cam.transform.position).normalized;
            Vector3 knock = dir * force + Vector3.up * upForce;

            NavMeshAgent agent = rb.GetComponent<NavMeshAgent>();

            if (agent != null)
            {
                StartCoroutine(ApplyKnockback(rb, agent, knock));
            }
            else
            {
                rb.isKinematic = false;
                rb.AddForce(knock, ForceMode.Impulse);
            }
        }
    }

    System.Collections.IEnumerator ApplyKnockback(Rigidbody rb, NavMeshAgent agent, Vector3 knock)
    {
        // 1) Desactivar NavMeshAgent
        agent.enabled = false;
        rb.isKinematic = false;

        // 2) Aplicar la fuerza real
        rb.velocity = Vector3.zero;
        rb.AddForce(knock, ForceMode.Impulse);

        // 3) Esperar a que el Rigidbody frene lo suficiente
        while (rb.velocity.magnitude > minVelocityToReenable)
        {
            yield return null;
        }

        // 4) Volver al NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(rb.transform.position, out hit, maxNavSearchDistance, NavMesh.AllAreas))
        {
            // alinearlo EXACTAMENTE al navmesh
            rb.transform.position = hit.position;
        }

        // 5) Reactivar el NavMeshAgent
        agent.enabled = true;
        agent.ResetPath();
        agent.isStopped = false;
    }
}

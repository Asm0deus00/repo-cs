using UnityEngine;

/// <summary>
/// Colócalo en un GameObject vacío con Box Collider (Is Trigger = true)
/// cruzando la línea de meta, orientado PERPENDICULAR a la dirección de carrera.
///
/// Setup en editor:
///   1. Crear GameObject vacío "LapTrigger" sobre la línea de meta.
///   2. Agregar Box Collider → marcar "Is Trigger".
///   3. Escalar el collider para que tape todo el ancho de la pista.
///   4. Asignar este script.
///   5. El coche debe tener el tag "Player".
/// </summary>
public class LapTrigger : MonoBehaviour
{
    [Tooltip("Activa en el Inspector para ver la dirección válida de paso")]
    public bool debugDraw = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Verificar que el coche cruza en la dirección correcta
        // (evita contar si va marcha atrás sobre la meta)
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null)
        {
            float dot = Vector3.Dot(rb.velocity.normalized, transform.forward);
            if (dot < 0f) return;   // va en dirección contraria, ignorar
        }

        RaceManager.Instance?.CompleteLap();
    }

    private void OnDrawGizmos()
    {
        if (!debugDraw) return;
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        // Flecha indicando dirección válida
        Gizmos.color = Color.green;
        Gizmos.DrawRay(Vector3.zero, Vector3.forward * 2f);
    }
}

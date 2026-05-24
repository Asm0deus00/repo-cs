using UnityEngine;

/// <summary>
/// Cámara que sigue al coche con suavizado.
/// Colócala en un GameObject vacío, asigna el target (el coche).
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Offset from target")]
    public Vector3 offset = new Vector3(0f, 3.5f, -7f);

    [Header("Smoothing")]
    [Range(1f, 20f)]
    public float positionSmoothing = 8f;
    [Range(1f, 20f)]
    public float rotationSmoothing = 6f;

    private Vector3    _currentVelocity;

    void LateUpdate()
    {
        if (target == null) return;

        // Posición deseada: offset rotado según la orientación del coche
        Vector3 desiredPos = target.TransformPoint(offset);
        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPos,
            ref _currentVelocity, 1f / positionSmoothing);

        // Mirar hacia el coche
        Quaternion desiredRot = Quaternion.LookRotation(
            target.position - transform.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, desiredRot,
            rotationSmoothing * Time.deltaTime);
    }
}

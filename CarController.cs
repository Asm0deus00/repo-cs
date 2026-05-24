using UnityEngine;

/// <summary>
/// Mueve el coche según los valores del ControllerInput.
/// Requiere un Rigidbody en el mismo GameObject.
/// Las ruedas (wheelFL, FR, RL, RR) son Transforms del modelo GLB.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    // ── Configuración movimiento ────────────────────────
    [Header("Movement")]
    [Tooltip("Velocidad máxima en m/s (pot al máximo, sin turbo)")]
    public float maxSpeed = 30f;

    [Tooltip("Ángulo máximo de giro en grados")]
    public float maxSteerAngle = 35f;

    [Tooltip("Qué tan rápido responde el coche a cambios de velocidad del pot (Lerp factor)")]
    public float speedSmoothing = 4f;

    // ── Turbo ───────────────────────────────────────────
    [Header("Turbo")]
    [Tooltip("Multiplicador de velocidad durante el burst")]
    public float turboMultiplier = 1.6f;

    [Tooltip("Duración del burst en segundos")]
    public float turboBurstDuration = 1.5f;

    [Tooltip("Tiempo de recarga completa en segundos")]
    public float turboCooldown = 3f;

    // ── Ruedas (Transforms del modelo GLB) ─────────────
    [Header("Wheel Transforms (GLB)")]
    public Transform wheelFL;
    public Transform wheelFR;
    public Transform wheelRL;
    public Transform wheelRR;

    [Tooltip("Radio aproximado de las ruedas en metros (para calcular RPM visual)")]
    public float wheelRadius = 0.33f;

    // ── Estado interno ──────────────────────────────────
    private Rigidbody _rb;
    private float _currentSpeed;      // m/s actual (lerpeado)
    private float _turboCharge = 1f;  // 0..1
    private bool  _turboActive;
    private float _turboTimer;

    // ── Propiedades públicas (leídas por HUD y DisplaySender) ──
    public float CurrentSpeedKmh => Mathf.Max(0f, _currentSpeed * 3.6f);
    public float TurboCharge     => _turboCharge;
    public bool  TurboIsActive   => _turboActive;

    // ── Lifecycle ──────────────────────────────────────
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // Evitar que el coche ruede solo
        _rb.constraints = RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        // No mover si la carrera no está activa
        if (RaceManager.Instance != null && !RaceManager.Instance.IsRacing) return;
        if (RaceManager.Instance != null &&  RaceManager.Instance.IsPaused)  return;

        var input = ControllerInput.Instance;
        if (input == null) return;

        HandleTurbo(input);
        HandleMovement(input);
        HandleWheels(input);
    }

    // ── Turbo ───────────────────────────────────────────
    private void HandleTurbo(ControllerInput input)
    {
        // Tap activa el turbo solo si la carga está llena
        if (input.TurboTap && _turboCharge >= 1f && !_turboActive)
        {
            _turboActive  = true;
            _turboCharge  = 0f;
            _turboTimer   = turboBurstDuration;
        }

        if (_turboActive)
        {
            _turboTimer -= Time.fixedDeltaTime;
            if (_turboTimer <= 0f) _turboActive = false;
        }
        else if (_turboCharge < 1f)
        {
            _turboCharge = Mathf.Clamp01(
                _turboCharge + Time.fixedDeltaTime / turboCooldown);
        }
    }

    // ── Movimiento ──────────────────────────────────────
    private void HandleMovement(ControllerInput input)
    {
        float multiplier  = _turboActive ? turboMultiplier : 1f;
        float targetSpeed = input.Speed * maxSpeed * multiplier;

        // Lerp suave para que el coche no cambie de velocidad instantáneamente
        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed,
                                    speedSmoothing * Time.fixedDeltaTime);

        // Avanzar
        Vector3 move = transform.forward * _currentSpeed * Time.fixedDeltaTime;
        _rb.MovePosition(_rb.position + move);

        // Girar (proporcional a la velocidad para evitar spin en parado)
        float speedFactor = Mathf.Clamp01(_currentSpeed / maxSpeed);
        float turnDelta   = input.Steering * maxSteerAngle * speedFactor
                            * Time.fixedDeltaTime * 60f;
        Quaternion rot = Quaternion.Euler(0f, turnDelta, 0f);
        _rb.MoveRotation(_rb.rotation * rot);
    }

    // ── Rotación visual de ruedas ───────────────────────
    private void HandleWheels(ControllerInput input)
    {
        // Grados por frame según velocidad y radio
        float spinDeg = _currentSpeed * Time.fixedDeltaTime
                        * (360f / (2f * Mathf.PI * wheelRadius));

        foreach (var w in new[] { wheelFL, wheelFR, wheelRL, wheelRR })
            if (w != null)
                w.Rotate(spinDeg, 0f, 0f, Space.Self);

        // Ángulo de dirección en ruedas delanteras
        float steerVis = input.Steering * maxSteerAngle;
        SetWheelSteer(wheelFL, steerVis);
        SetWheelSteer(wheelFR, steerVis);
    }

    private static void SetWheelSteer(Transform wheel, float angle)
    {
        if (wheel == null) return;
        Vector3 e = wheel.localEulerAngles;
        e.y = angle;
        wheel.localEulerAngles = e;
    }
}

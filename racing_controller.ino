using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed       = 10f;
    public float steerAngle     = 35f;
    public float speedSmoothing = 4f;

    [Header("Turbo")]
    public float turboMultiplier = 1.6f;
    public float turboDuration   = 1.5f;
    public float turboCooldown   = 3f;

    private Rigidbody _rb;
    private float _currentSpeed;
    private float _turboCharge  = 1f;
    private bool  _turboActive  = false;
    private float _turboTimer   = 0f;

    public float CurrentSpeedKmh => _currentSpeed * 3.6f;
    public float TurboCharge     => _turboCharge;
    public bool  TurboIsActive   => _turboActive;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.linearDamping = 5f;
        _rb.angularDamping = 10f;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        var input = ControllerInput.Instance;
        if (input == null) return;

        // ── Turbo ─────────────────────────────────────────
        if (input.TurboTap && _turboCharge >= 1f && !_turboActive)
        {
            _turboActive = true;
            _turboCharge = 0f;
            _turboTimer  = turboDuration;
        }

        if (_turboActive)
        {
            _turboTimer -= Time.fixedDeltaTime;
            if (_turboTimer <= 0f) _turboActive = false;
        }
        else if (_turboCharge < 1f)
        {
            _turboCharge += Time.fixedDeltaTime / turboCooldown;
            _turboCharge  = Mathf.Clamp01(_turboCharge);
        }

        // ── Velocidad ─────────────────────────────────────
        float mult        = _turboActive ? turboMultiplier : 1f;
        float targetSpeed = input.Speed * maxSpeed * mult;
        _currentSpeed     = Mathf.Lerp(_currentSpeed, targetSpeed, speedSmoothing * Time.fixedDeltaTime);

        // ── Movimiento via velocity (respeta física) ──────
        Vector3 vel    = transform.forward * _currentSpeed;
        vel.y          = _rb.linearVelocity.y;
        _rb.linearVelocity = vel;

        // ── Steering ──────────────────────────────────────
        if (_currentSpeed > 0.1f)
        {
            float turn = input.Steering * steerAngle * Time.fixedDeltaTime;
            Quaternion rot = Quaternion.Euler(0f, turn, 0f);
            _rb.MoveRotation(_rb.rotation * rot);
        }

        Debug.Log($"Steering: {Steering} | Speed: {Speed} | Turbo: {TurboTap} | Pause: {PauseTap}");
    }
}
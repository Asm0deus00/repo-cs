using UnityEngine;

/// <summary>
/// Parsea el CSV que llega del Arduino cada frame y expone
/// valores normalizados al resto del juego.
/// Persiste entre escenas junto a SerialManager.
/// </summary>
public class ControllerInput : MonoBehaviour
{
    public static ControllerInput Instance { get; private set; }

    // ── Valores públicos (actualizados cada frame) ──────

    /// -1 = máximo izquierda, 0 = centro, +1 = máximo derecha
    public float Steering { get; private set; }

    /// 0 = parado, 1 = velocidad máxima
    public float Speed { get; private set; }

    /// 0 = primer opción (Resume), 1 = segunda opción (Restart)
    public float MenuNav { get; private set; }

    /// true SOLO el frame en que se detectó el tap de turbo
    public bool TurboTap { get; private set; }

    /// true SOLO el frame en que se detectó el tap de pausa
    public bool PauseTap { get; private set; }

    // ── Zona muerta para el steering ───────────────────
    [Tooltip("Fracción de recorrido del pot ignorada en el centro (0–1)")]
    [Range(0f, 0.2f)]
    public float steeringDeadzone = 0.06f;

    // ── Lifecycle ──────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // Resetear taps antes de parsear
        TurboTap = false;
        PauseTap = false;

        if (SerialManager.Instance == null) return;

        string line = SerialManager.Instance.GetLatestLine();
        if (string.IsNullOrEmpty(line)) return;

        string[] parts = line.Split(',');
        if (parts.Length < 5) return;

        if (!int.TryParse(parts[0].Trim(), out int p1)) return;
        if (!int.TryParse(parts[1].Trim(), out int p2)) return;
        if (!int.TryParse(parts[2].Trim(), out int p3)) return;
        if (!int.TryParse(parts[3].Trim(), out int tb)) return;
        if (!int.TryParse(parts[4].Trim(), out int pb)) return;

        // Steering: 0–1023 → -1..+1, con zona muerta central
        float rawSteer = (p1 - 512f) / 512f;
        Steering = Mathf.Abs(rawSteer) < steeringDeadzone
                   ? 0f
                   : Mathf.Clamp(rawSteer, -1f, 1f);

        // Speed: 0–1023 → 0..1
        Speed = Mathf.Clamp01(p2 / 1023f);

        // MenuNav: 0–1023 → 0..1  (< 0.5 = Resume, >= 0.5 = Restart)
        MenuNav = Mathf.Clamp01(p3 / 1023f);

        // Taps (el Arduino ya envía flanco de subida)
        TurboTap = tb == 1;
        PauseTap = pb == 1;
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Actualiza todos los elementos del HUD durante la carrera.
/// Todos los textos deben usar TextMeshPro con fuente custom (no la default de Unity).
/// </summary>
public class HUD : MonoBehaviour
{
    [Header("Speed")]
    public TextMeshProUGUI speedText;       // Ej: "127 km/h"

    [Header("Lap info")]
    public TextMeshProUGUI lapText;         // Ej: "LAP 2 / 3"
    public TextMeshProUGUI lapTimeText;     // Ej: "01:23.45"
    public TextMeshProUGUI bestLapText;     // Ej: "BEST 01:20.10"

    [Header("Turbo")]
    [Tooltip("Image con Image Type = Filled, Fill Method = Horizontal")]
    public Image  turboBarFill;
    [Tooltip("GameObject con glow/indicador que se activa cuando turbo está lleno")]
    public GameObject turboReadyIndicator;
    [Tooltip("Texto o icono 'TURBO' que parpadea durante el burst")]
    public GameObject turboActiveIndicator;

    [Header("Pause overlay")]
    [Tooltip("Panel raíz del menú de pausa (activar/desactivar)")]
    public GameObject pauseOverlay;

    // Cache de referencias
    private CarController _car;
    private RaceManager   _race;

    void Update()
    {
        if (_car  == null) _car  = FindObjectOfType<CarController>();
        if (_race == null) _race = RaceManager.Instance;

        UpdateSpeed();
        UpdateLapInfo();
        UpdateTurbo();
        UpdatePause();
    }

    // ── Speed ───────────────────────────────────────────
    private void UpdateSpeed()
    {
        if (_car == null || speedText == null) return;
        int kmh = Mathf.Max(0, Mathf.RoundToInt(_car.CurrentSpeedKmh));
        speedText.text = kmh.ToString() + " <size=60%>km/h</size>";
    }

    // ── Lap info ────────────────────────────────────────
    private void UpdateLapInfo()
    {
        if (_race == null) return;

        if (lapText != null)
            lapText.text = $"LAP {_race.CurrentLap} / {_race.TotalLaps}";

        if (lapTimeText != null)
            lapTimeText.text = FormatTime(_race.CurrentLapTime);

        if (bestLapText != null)
        {
            bestLapText.text = _race.BestLap < float.MaxValue
                ? "BEST  " + FormatTime(_race.BestLap)
                : "BEST  --:--.--";
        }
    }

    // ── Turbo ───────────────────────────────────────────
    private void UpdateTurbo()
    {
        if (_car == null) return;

        if (turboBarFill != null)
            turboBarFill.fillAmount = _car.TurboCharge;

        if (turboReadyIndicator != null)
            turboReadyIndicator.SetActive(_car.TurboCharge >= 1f && !_car.TurboIsActive);

        if (turboActiveIndicator != null)
            turboActiveIndicator.SetActive(_car.TurboIsActive);
    }

    // ── Pause overlay ───────────────────────────────────
    private void UpdatePause()
    {
        if (pauseOverlay == null || _race == null) return;
        pauseOverlay.SetActive(_race.IsPaused);
    }

    // ── Helpers ─────────────────────────────────────────
    public static string FormatTime(float t)
    {
        t = Mathf.Max(0f, t);
        int m  = Mathf.FloorToInt(t / 60f);
        int s  = Mathf.FloorToInt(t % 60f);
        int ms = Mathf.FloorToInt((t * 100f) % 100f);
        return $"{m:00}:{s:00}.{ms:00}";
    }
}

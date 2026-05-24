using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestiona el estado global de la carrera:
/// vueltas, tiempos, pausa y transición a la pantalla final.
/// Persiste entre escenas.
/// </summary>
public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }

    // ── Config ──────────────────────────────────────────
    [Tooltip("Número de vueltas para completar la carrera")]
    public int totalLaps = 3;

    // ── Estado ──────────────────────────────────────────
    public bool IsRacing { get; private set; }
    public bool IsPaused { get; private set; }
    public int  CurrentLap { get; private set; } = 1;

    private float[]  _lapTimes;
    private float    _lapStart;
    private bool     _raceStarted;

    // ── Propiedades públicas ────────────────────────────
    public float   CurrentLapTime => _raceStarted ? Time.time - _lapStart : 0f;
    public float[] LapTimes       => _lapTimes;
    public float   BestLap        { get; private set; } = float.MaxValue;
    public int     TotalLaps      => totalLaps;

    // ── Lifecycle ──────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Si ya existe una instancia (ej. al reiniciar), la reemplazamos
            Destroy(Instance.gameObject);
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _lapTimes = new float[totalLaps];
    }

    void Start()
    {
        StartRace();
    }

    void Update()
    {
        if (!IsRacing) return;

        var input = ControllerInput.Instance;
        if (input == null) return;

        if (input.PauseTap)
        {
            if (IsPaused) ConfirmPauseSelection();
            else          Pause();
        }
    }

    // ── Race flow ───────────────────────────────────────

    public void StartRace()
    {
        CurrentLap   = 1;
        BestLap      = float.MaxValue;
        _lapTimes    = new float[totalLaps];
        _lapStart    = Time.time;
        _raceStarted = true;
        IsRacing     = true;
        IsPaused     = false;
        Time.timeScale = 1f;
    }

    /// Llamado por LapTrigger cuando el coche cruza la meta.
    public void CompleteLap()
    {
        if (!IsRacing || IsPaused) return;

        float lapTime = CurrentLapTime;
        _lapTimes[CurrentLap - 1] = lapTime;

        if (lapTime < BestLap) BestLap = lapTime;

        if (CurrentLap >= totalLaps)
        {
            // Carrera terminada
            IsRacing = false;
            SceneManager.LoadScene("EndScreen");
        }
        else
        {
            CurrentLap++;
            _lapStart = Time.time;
        }
    }

    // ── Pausa ───────────────────────────────────────────

    public void Pause()
    {
        IsPaused       = true;
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        IsPaused       = false;
        Time.timeScale = 1f;
    }

    /// Confirma la opción seleccionada con pot3 en el menú de pausa.
    private void ConfirmPauseSelection()
    {
        float nav = ControllerInput.Instance?.MenuNav ?? 0f;

        if (nav < 0.5f)
        {
            // Resume
            Resume();
        }
        else
        {
            // Restart: destruir esta instancia para que la escena cree una nueva
            Time.timeScale = 1f;
            Destroy(gameObject);
            SceneManager.LoadScene("Race");
        }
    }
}

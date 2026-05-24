using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Pantalla final que muestra los tiempos de cada vuelta,
/// el tiempo total y la mejor vuelta.
/// </summary>
public class EndScreenUI : MonoBehaviour
{
    [Header("Lap time labels (assign 3 elements)")]
    public TextMeshProUGUI[] lapTimeLabels;   // tamaño 3

    [Header("Summary")]
    public TextMeshProUGUI totalTimeLabel;
    public TextMeshProUGUI bestLapLabel;

    [Header("Buttons")]
    public Button restartButton;
    public Button quitButton;

    void Start()
    {
        PopulateResults();

        restartButton.onClick.AddListener(() =>
        {
            // Destruir el RaceManager viejo para que la escena Race cree uno fresco
            if (RaceManager.Instance != null)
                Destroy(RaceManager.Instance.gameObject);

            SceneManager.LoadScene("Race");
        });

        if (quitButton != null)
            quitButton.onClick.AddListener(Application.Quit);
    }

    private void PopulateResults()
    {
        var race = RaceManager.Instance;
        if (race == null)
        {
            // Fallback si no hay datos
            if (totalTimeLabel) totalTimeLabel.text = "TOTAL  --:--.--";
            if (bestLapLabel)   bestLapLabel.text   = "BEST   --:--.--";
            return;
        }

        float total = 0f;

        for (int i = 0; i < race.LapTimes.Length; i++)
        {
            float t = race.LapTimes[i];
            total  += t;

            if (lapTimeLabels != null && i < lapTimeLabels.Length && lapTimeLabels[i] != null)
                lapTimeLabels[i].text = $"LAP {i + 1}   {HUD.FormatTime(t)}";
        }

        if (totalTimeLabel != null)
            totalTimeLabel.text = "TOTAL   " + HUD.FormatTime(total);

        if (bestLapLabel != null)
        {
            bestLapLabel.text = race.BestLap < float.MaxValue
                ? "BEST    " + HUD.FormatTime(race.BestLap)
                : "BEST    --:--.--";
        }
    }
}

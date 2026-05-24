using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Primera pantalla del juego.
/// Muestra los puertos COM disponibles y permite conectar antes de entrar a la carrera.
/// </summary>
public class PortSelectUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown        portDropdown;
    public Button              connectButton;
    public Button              refreshButton;
    public TextMeshProUGUI     statusText;

    [Header("Scene to load after connecting")]
    public string raceSceneName = "Race";

    void Start()
    {
        connectButton.onClick.AddListener(OnConnect);
        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshPorts);

        RefreshPorts();
    }

    private void RefreshPorts()
    {
        SerialManager.Instance?.RefreshPorts();
        string[] ports = SerialManager.Instance?.AvailablePorts;

        portDropdown.ClearOptions();

        if (ports == null || ports.Length == 0)
        {
            portDropdown.AddOptions(new List<string> { "No ports found" });
            connectButton.interactable = false;
            SetStatus("No serial ports detected. Connect the Arduino and refresh.", false);
            return;
        }

        portDropdown.AddOptions(new List<string>(ports));
        connectButton.interactable = true;
        SetStatus($"{ports.Length} port(s) found. Select and connect.", true);
    }

    private void OnConnect()
    {
        string selectedPort = portDropdown.options[portDropdown.value].text;

        try
        {
            SerialManager.Instance.Connect(selectedPort);
            SetStatus($"Connected to {selectedPort}. Loading race...", true);
            connectButton.interactable = false;

            // Pequeño delay antes de cargar para que el Arduino se inicialice
            Invoke(nameof(LoadRace), 1.5f);
        }
        catch (System.Exception e)
        {
            SetStatus($"Connection failed: {e.Message}", false);
        }
    }

    private void LoadRace()
    {
        SceneManager.LoadScene(raceSceneName);
    }

    private void SetStatus(string msg, bool ok)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = ok ? Color.white : new Color(1f, 0.4f, 0.4f);
    }
}

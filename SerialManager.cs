using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

/// <summary>
/// Singleton que gestiona la conexión Serial con el Arduino.
/// Corre la lectura en un hilo secundario para no bloquear el juego.
/// Persiste entre escenas (DontDestroyOnLoad).
/// </summary>
public class SerialManager : MonoBehaviour
{
    public static SerialManager Instance { get; private set; }

    [HideInInspector] public string[] AvailablePorts;

    private SerialPort _port;
    private Thread     _readThread;
    private volatile bool _running;

    // Último paquete recibido — protegido con lock
    private readonly object _lock = new object();
    private string _latestLine = "";

    // Cola de strings a enviar al Arduino
    private readonly System.Collections.Generic.Queue<string> _sendQueue
        = new System.Collections.Generic.Queue<string>();

    // ── Lifecycle ──────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        RefreshPorts();
    }

    void OnDestroy() => Disconnect();

    // ── Public API ─────────────────────────────────────

    /// Refresca la lista de puertos disponibles.
    public void RefreshPorts()
    {
        AvailablePorts = SerialPort.GetPortNames();
    }

    /// Abre la conexión con el puerto indicado.
    public void Connect(string portName, int baud = 115200)
    {
        Disconnect();
        _port = new SerialPort(portName, baud)
        {
            ReadTimeout  = 50,
            WriteTimeout = 50,
            NewLine      = "\n"
        };
        _port.Open();
        _running = true;
        _readThread = new Thread(ReadLoop) { IsBackground = true };
        _readThread.Start();
        Debug.Log($"[SerialManager] Connected to {portName} @ {baud}");
    }

    /// Cierra la conexión limpiamente.
    public void Disconnect()
    {
        _running = false;
        _readThread?.Join(300);
        if (_port != null && _port.IsOpen) _port.Close();
        _port = null;
    }

    public bool IsConnected => _port != null && _port.IsOpen;

    /// Devuelve la última línea recibida del Arduino.
    public string GetLatestLine()
    {
        lock (_lock) { return _latestLine; }
    }

    /// Encola un string para enviarlo al Arduino en el próximo Update.
    public void SendLine(string data)
    {
        lock (_sendQueue) { _sendQueue.Enqueue(data); }
    }

    // ── Update: vaciar cola de envío en el hilo principal ──
    void Update()
    {
        if (_port == null || !_port.IsOpen) return;
        lock (_sendQueue)
        {
            while (_sendQueue.Count > 0)
            {
                try   { _port.WriteLine(_sendQueue.Dequeue()); }
                catch (Exception e) { Debug.LogWarning($"[SerialManager] Send error: {e.Message}"); }
            }
        }
    }

    // ── Hilo de lectura ────────────────────────────────
    private void ReadLoop()
    {
        while (_running)
        {
            try
            {
                string line = _port.ReadLine();
                if (!string.IsNullOrWhiteSpace(line))
                    lock (_lock) { _latestLine = line.Trim(); }
            }
            catch (TimeoutException) { /* normal, continúa */ }
            catch (Exception e)
            {
                Debug.LogWarning($"[SerialManager] Read error: {e.Message}");
                _running = false;
            }
        }
    }
}

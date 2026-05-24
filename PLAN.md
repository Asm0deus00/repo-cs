# Proyecto Final — Racing Game Físico-Digital
**Realidad Mixta · Arduino + Unity · USB Serial**

---

## Índice

1. [Resumen del proyecto](#1-resumen-del-proyecto)
2. [Componentes de hardware](#2-componentes-de-hardware)
3. [Esquema de cableado](#3-esquema-de-cableado)
4. [Protocolo de comunicación Serial](#4-protocolo-de-comunicación-serial)
5. [Sketch de Arduino](#5-sketch-de-arduino)
6. [Proyecto Unity — estructura de escenas](#6-proyecto-unity--estructura-de-escenas)
7. [Scripts de Unity](#7-scripts-de-unity)
8. [Assets GLB](#8-assets-glb)
9. [HUD y UI personalizada](#9-hud-y-ui-personalizada)
10. [Flujo de pantallas](#10-flujo-de-pantallas)
11. [Sistema de juego](#11-sistema-de-juego)
12. [Build y entregables](#12-build-y-entregables)
13. [Repositorio GitHub](#13-repositorio-github)
14. [Checklist de rúbrica](#14-checklist-de-rúbrica)

---

## 1. Resumen del proyecto

Un juego de carreras de un solo jugador donde el controlador físico (Arduino) reemplaza completamente el teclado/mouse. El objetivo es completar 3 vueltas al circuito en el menor tiempo posible, usando el turbo estratégicamente para mejorar tiempos de vuelta.

| Ítem | Detalle |
|------|---------|
| Motor de juego | Unity 2022.3 LTS |
| Microcontrolador | Arduino Uno / Nano / ESP32 |
| Comunicación | USB Serial (115200 baud) |
| Circuito | 1 pista, 3 vueltas |
| Objetivo | Mejor tiempo total y mejor vuelta |
| Entregable principal | Build ejecutable + repositorio GitHub |

---

## 2. Componentes de hardware

### Entradas

| Componente | Valor | Pin Arduino | Función |
|------------|-------|-------------|---------|
| Potenciómetro 1 | 10kΩ | A0 | Dirección (steering) |
| Potenciómetro 2 | 5kΩ | A1 | Velocidad directa (speed) |
| Potenciómetro 3 | 1kΩ | A2 | Navegación menú pausa |
| Botón A | — | D2 | Turbo (tap = consumir carga) |
| Botón B | — | D3 | Pausa / confirmar selección |

### Salidas

| Componente | Modelo sugerido | Pines Arduino | Función |
|------------|-----------------|---------------|---------|
| Display 7 segmentos | TM1637 4-dígitos | D5 (CLK), D6 (DIO) | Muestra velocidad actual (0–999) |

### Otros materiales

- Resistencias 10kΩ × 2 (pull-down para botones)
- Protoboard + carcasa (requerido por rúbrica — no protoboard expuesta)
- Cable USB tipo B / micro-USB (según placa)
- Cables dupont macho-macho y macho-hembra

---

## 3. Esquema de cableado

```
Arduino
│
├── A0 ──── [POT 10kΩ] ──── 5V / GND        (steering)
├── A1 ──── [POT  5kΩ] ──── 5V / GND        (speed)
├── A2 ──── [POT  1kΩ] ──── 5V / GND        (menu nav)
│
├── D2 ──── [BTN_A] ──── GND  + 10kΩ pull-down  (turbo)
├── D3 ──── [BTN_B] ──── GND  + 10kΩ pull-down  (pause)
│
├── D5 (CLK) ──── TM1637 CLK
├── D6 (DIO) ──── TM1637 DIO
├── 5V ──────────── TM1637 VCC
└── GND ─────────── TM1637 GND
```

Notas:
- Los tres potenciómetros comparten el rail de 5V y GND de la protoboard.
- Los botones usan configuración INPUT con resistencia pull-down externa de 10kΩ.
- El TM1637 opera a 3.3–5V; compatible directo con Arduino 5V.

---

## 4. Protocolo de comunicación Serial

**Baud rate:** 115200

### Arduino → Unity (cada ~20 ms)

Formato CSV terminado en newline:

```
pot1,pot2,pot3,turbo,pause\n
```

| Campo | Rango | Descripción |
|-------|-------|-------------|
| pot1 | 0–1023 | Steering (512 = centro) |
| pot2 | 0–1023 | Speed (0 = parado, 1023 = máxima) |
| pot3 | 0–1023 | Menú nav (< 512 = Resume, >= 512 = Restart) |
| turbo | 0 / 1 | 1 = tap detectado este frame |
| pause | 0 / 1 | 1 = tap detectado este frame |

### Unity → Arduino (cada ~100 ms)

```
speed\n
```

| Campo | Rango | Descripción |
|-------|-------|-------------|
| speed | 0–999 | Velocidad actual del coche, clamped ≥ 0 |

---

## 5. Sketch de Arduino

Archivo: `arduino/racing_controller/racing_controller.ino`

```cpp
#include <TM1637Display.h>

// ── Pines ──────────────────────────────────────────────
#define PIN_POT_STEER   A0
#define PIN_POT_SPEED   A1
#define PIN_POT_MENU    A2
#define PIN_BTN_TURBO   2
#define PIN_BTN_PAUSE   3
#define CLK_DISPLAY     5
#define DIO_DISPLAY     6

// ── Display ────────────────────────────────────────────
TM1637Display display(CLK_DISPLAY, DIO_DISPLAY);

// ── Estado botones (detección de tap) ──────────────────
bool lastTurbo = false;
bool lastPause = false;

// ── Envío Serial ───────────────────────────────────────
unsigned long lastSendTime = 0;
const unsigned long SEND_INTERVAL = 20;       // ms

// ── Recepción Serial ───────────────────────────────────
String serialBuffer = "";
unsigned long lastDisplayUpdate = 0;
const unsigned long DISPLAY_INTERVAL = 100;   // ms
int displaySpeed = 0;

// ── Setup ──────────────────────────────────────────────
void setup() {
  Serial.begin(115200);

  pinMode(PIN_BTN_TURBO, INPUT);
  pinMode(PIN_BTN_PAUSE, INPUT);

  display.setBrightness(7);
  display.showNumberDec(0, true);
}

// ── Loop ───────────────────────────────────────────────
void loop() {
  unsigned long now = millis();

  // — Leer entradas —
  int pot1 = analogRead(PIN_POT_STEER);
  int pot2 = analogRead(PIN_POT_SPEED);
  int pot3 = analogRead(PIN_POT_MENU);

  bool turboNow = digitalRead(PIN_BTN_TURBO) == HIGH;
  bool pauseNow = digitalRead(PIN_BTN_PAUSE) == HIGH;

  // Detectar tap (flanco de subida)
  int turboTap = (!lastTurbo && turboNow) ? 1 : 0;
  int pauseTap = (!lastPause && pauseNow) ? 1 : 0;

  lastTurbo = turboNow;
  lastPause = pauseNow;

  // — Enviar datos a Unity cada SEND_INTERVAL ms —
  if (now - lastSendTime >= SEND_INTERVAL) {
    lastSendTime = now;
    Serial.print(pot1);   Serial.print(',');
    Serial.print(pot2);   Serial.print(',');
    Serial.print(pot3);   Serial.print(',');
    Serial.print(turboTap); Serial.print(',');
    Serial.println(pauseTap);
  }

  // — Recibir velocidad desde Unity —
  while (Serial.available() > 0) {
    char c = Serial.read();
    if (c == '\n') {
      int speed = serialBuffer.toInt();
      speed = max(0, min(999, speed));   // clamp 0–999
      displaySpeed = speed;
      serialBuffer = "";
    } else {
      serialBuffer += c;
    }
  }

  // — Actualizar display cada DISPLAY_INTERVAL ms —
  if (now - lastDisplayUpdate >= DISPLAY_INTERVAL) {
    lastDisplayUpdate = now;
    display.showNumberDec(displaySpeed, false);
  }
}
```

**Dependencia Arduino:** Instalar `TM1637Display` by Avishay Orpaz desde el Library Manager.

---

## 6. Proyecto Unity — estructura de escenas

```
Assets/
├── Scenes/
│   ├── PortSelect.unity        ← primera escena en Build Settings
│   ├── Race.unity              ← escena principal de carrera
│   └── EndScreen.unity         ← pantalla de resultados
├── Scripts/
│   ├── Serial/
│   │   ├── SerialManager.cs    ← lectura/escritura hilo secundario
│   │   └── ControllerInput.cs  ← expone valores parseados al resto del juego
│   ├── Car/
│   │   ├── CarController.cs    ← movimiento, steering, turbo
│   │   └── WheelSpin.cs        ← rotación visual de ruedas GLB
│   ├── Race/
│   │   ├── RaceManager.cs      ← vueltas, tiempos, estado de carrera
│   │   └── LapTrigger.cs       ← detección de vuelta por trigger collider
│   ├── UI/
│   │   ├── PortSelectUI.cs     ← lista de puertos, botón conectar
│   │   ├── HUD.cs              ← velocidad, vuelta, timer, barra turbo
│   │   ├── PauseMenuUI.cs      ← overlay pausa, navegación por pot3
│   │   └── EndScreenUI.cs      ← tiempos de vuelta, total, botón reiniciar
│   └── Feedback/
│       └── DisplaySender.cs    ← envía velocidad al Arduino cada 100ms
├── Models/
│   ├── car_body.glb
│   ├── wheel.glb
│   └── circuit.glb
├── Materials/                  ← materiales personalizados (sin defaults Unity)
├── Fonts/                      ← fuente custom para toda la UI
└── Audio/ (opcional)
```

---

## 7. Scripts de Unity

### SerialManager.cs

```csharp
using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class SerialManager : MonoBehaviour
{
    public static SerialManager Instance { get; private set; }

    [HideInInspector] public string[] AvailablePorts;
    private SerialPort _port;
    private Thread _readThread;
    private volatile bool _running;

    // Último paquete recibido (thread-safe via lock)
    private readonly object _lock = new();
    private string _latestLine = "";

    // Cola de mensajes a enviar
    private readonly System.Collections.Generic.Queue<string> _sendQueue = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        AvailablePorts = SerialPort.GetPortNames();
    }

    public void Connect(string portName, int baud = 115200)
    {
        Disconnect();
        _port = new SerialPort(portName, baud) { ReadTimeout = 50, WriteTimeout = 50 };
        _port.Open();
        _running = true;
        _readThread = new Thread(ReadLoop) { IsBackground = true };
        _readThread.Start();
    }

    public void Disconnect()
    {
        _running = false;
        _readThread?.Join(200);
        if (_port != null && _port.IsOpen) _port.Close();
        _port = null;
    }

    public string GetLatestLine()
    {
        lock (_lock) { return _latestLine; }
    }

    public void SendLine(string data)
    {
        lock (_sendQueue) { _sendQueue.Enqueue(data); }
    }

    void Update()
    {
        if (_port == null || !_port.IsOpen) return;
        lock (_sendQueue)
        {
            while (_sendQueue.Count > 0)
            {
                try { _port.WriteLine(_sendQueue.Dequeue()); }
                catch { }
            }
        }
    }

    private void ReadLoop()
    {
        while (_running)
        {
            try
            {
                string line = _port.ReadLine();
                lock (_lock) { _latestLine = line.Trim(); }
            }
            catch (TimeoutException) { }
            catch { _running = false; }
        }
    }

    void OnDestroy() => Disconnect();
}
```

---

### ControllerInput.cs

```csharp
using UnityEngine;

/// Parsea el CSV del Arduino y expone valores normalizados al juego.
public class ControllerInput : MonoBehaviour
{
    public static ControllerInput Instance { get; private set; }

    // Valores normalizados (actualizados cada frame)
    public float Steering   { get; private set; }   // -1 izq .. +1 der
    public float Speed      { get; private set; }   //  0 .. 1
    public float MenuNav    { get; private set; }   //  0 .. 1
    public bool  TurboTap   { get; private set; }   //  true un solo frame
    public bool  PauseTap   { get; private set; }   //  true un solo frame

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // Reset taps
        TurboTap = false;
        PauseTap = false;

        string line = SerialManager.Instance?.GetLatestLine();
        if (string.IsNullOrEmpty(line)) return;

        string[] parts = line.Split(',');
        if (parts.Length < 5) return;

        if (!int.TryParse(parts[0], out int p1)) return;
        if (!int.TryParse(parts[1], out int p2)) return;
        if (!int.TryParse(parts[2], out int p3)) return;
        if (!int.TryParse(parts[3], out int tb)) return;
        if (!int.TryParse(parts[4], out int pb)) return;

        // Normalizar steering: 0–1023 → -1..+1, muerto central ±30 cuentas
        float raw = (p1 - 512f) / 512f;
        Steering = Mathf.Abs(raw) < 0.06f ? 0f : Mathf.Clamp(raw, -1f, 1f);

        Speed   = Mathf.Clamp01(p2 / 1023f);
        MenuNav = Mathf.Clamp01(p3 / 1023f);

        TurboTap = tb == 1;
        PauseTap = pb == 1;
    }
}
```

---

### CarController.cs

```csharp
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed       = 30f;   // m/s
    public float steerAngle     = 35f;   // grados max
    public float speedSmoothing = 4f;    // lerp factor

    [Header("Turbo")]
    public float turboMultiplier  = 1.6f;
    public float turboDuration    = 1.5f;   // segundos de burst
    public float turboCooldown    = 3f;     // segundos de recarga

    [Header("Wheels (transforms visuales)")]
    public Transform wheelFL;
    public Transform wheelFR;
    public Transform wheelRL;
    public Transform wheelRR;

    private Rigidbody _rb;
    private float _currentSpeed;
    private float _turboCharge   = 1f;    // 0..1
    private bool  _turboActive   = false;
    private float _turboTimer    = 0f;

    public float CurrentSpeedKmh => _currentSpeed * 3.6f;
    public float TurboCharge     => _turboCharge;

    void Awake() => _rb = GetComponent<Rigidbody>();

    void FixedUpdate()
    {
        if (RaceManager.Instance != null && !RaceManager.Instance.IsRacing) return;

        var input = ControllerInput.Instance;
        if (input == null) return;

        // ── Turbo ────────────────────────────────────────
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

        // ── Velocidad objetivo ───────────────────────────
        float mult        = _turboActive ? turboMultiplier : 1f;
        float targetSpeed = input.Speed * maxSpeed * mult;
        _currentSpeed     = Mathf.Lerp(_currentSpeed, targetSpeed,
                                        speedSmoothing * Time.fixedDeltaTime);

        // ── Movimiento ───────────────────────────────────
        Vector3 forward = transform.forward * _currentSpeed * Time.fixedDeltaTime;
        _rb.MovePosition(_rb.position + forward);

        // ── Steering ─────────────────────────────────────
        float turn = input.Steering * steerAngle * (_currentSpeed / maxSpeed);
        Quaternion rot = Quaternion.Euler(0f, turn * Time.fixedDeltaTime * 60f, 0f);
        _rb.MoveRotation(_rb.rotation * rot);

        // ── Ruedas visuales ──────────────────────────────
        SpinWheels(_currentSpeed);
    }

    void SpinWheels(float speed)
    {
        float spinDeg = speed * Time.fixedDeltaTime * (360f / (2f * Mathf.PI * 0.33f));
        foreach (var w in new[] { wheelFL, wheelFR, wheelRL, wheelRR })
            if (w != null)
                w.Rotate(spinDeg, 0f, 0f, Space.Self);

        // Steering visual ruedas delanteras
        float steerVis = ControllerInput.Instance.Steering * steerAngle;
        if (wheelFL) { Vector3 e = wheelFL.localEulerAngles; e.y = steerVis; wheelFL.localEulerAngles = e; }
        if (wheelFR) { Vector3 e = wheelFR.localEulerAngles; e.y = steerVis; wheelFR.localEulerAngles = e; }
    }
}
```

---

### RaceManager.cs

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }

    public int  TotalLaps  = 3;
    public bool IsRacing   { get; private set; }
    public bool IsPaused   { get; private set; }
    public int  CurrentLap { get; private set; } = 1;

    private float[] _lapTimes;
    private float   _lapStart;
    public  float   CurrentLapTime => Time.time - _lapStart;
    public  float[] LapTimes       => _lapTimes;
    public  float   BestLap        { get; private set; } = float.MaxValue;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _lapTimes = new float[TotalLaps];
    }

    void Start()
    {
        IsRacing = true;
        _lapStart = Time.time;
    }

    void Update()
    {
        if (!IsRacing) return;

        if (ControllerInput.Instance != null && ControllerInput.Instance.PauseTap)
        {
            if (IsPaused) ConfirmPauseSelection();
            else          Pause();
        }
    }

    public void CompleteLap()
    {
        if (!IsRacing || IsPaused) return;

        float t = CurrentLapTime;
        _lapTimes[CurrentLap - 1] = t;
        if (t < BestLap) BestLap = t;

        if (CurrentLap >= TotalLaps)
        {
            IsRacing = false;
            SceneManager.LoadScene("EndScreen");
        }
        else
        {
            CurrentLap++;
            _lapStart = Time.time;
        }
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
    }

    private void ConfirmPauseSelection()
    {
        // pot3 < 0.5 = Resume, >= 0.5 = Restart
        float nav = ControllerInput.Instance?.MenuNav ?? 0f;
        if (nav < 0.5f) Resume();
        else            { Time.timeScale = 1f; SceneManager.LoadScene("Race"); }
    }
}
```

---

### LapTrigger.cs

```csharp
using UnityEngine;

/// Colócalo en un GameObject con Box Collider (IsTrigger = true)
/// atravesando la línea de meta, orientado perpendicular al circuito.
public class LapTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            RaceManager.Instance?.CompleteLap();
    }
}
```

---

### HUD.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUD : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI lapText;
    public TextMeshProUGUI lapTimeText;
    public TextMeshProUGUI bestLapText;
    public Image           turboBar;        // Image type Filled
    public GameObject      turboReadyGlow;  // indicador visual "listo"
    public GameObject      pauseOverlay;

    void Update()
    {
        var car  = FindObjectOfType<CarController>();
        var race = RaceManager.Instance;
        var inp  = ControllerInput.Instance;

        if (car != null)
        {
            int kmh = Mathf.Max(0, Mathf.RoundToInt(car.CurrentSpeedKmh));
            speedText.text = kmh.ToString("000") + " km/h";
            turboBar.fillAmount = car.TurboCharge;
            turboReadyGlow.SetActive(car.TurboCharge >= 1f);
        }

        if (race != null)
        {
            lapText.text     = $"LAP {race.CurrentLap} / {race.TotalLaps}";
            lapTimeText.text = FormatTime(race.CurrentLapTime);
            bestLapText.text = race.BestLap < float.MaxValue
                               ? "BEST " + FormatTime(race.BestLap)
                               : "BEST --:--.--";
        }

        pauseOverlay.SetActive(race != null && race.IsPaused);
    }

    static string FormatTime(float t)
    {
        int m  = Mathf.FloorToInt(t / 60f);
        int s  = Mathf.FloorToInt(t % 60f);
        int ms = Mathf.FloorToInt((t * 100f) % 100f);
        return $"{m:00}:{s:00}.{ms:00}";
    }
}
```

---

### PauseMenuUI.cs

```csharp
using UnityEngine;
using TMPro;

public class PauseMenuUI : MonoBehaviour
{
    public TextMeshProUGUI resumeText;
    public TextMeshProUGUI restartText;
    public Color           selectedColor   = Color.white;
    public Color           unselectedColor = new Color(1,1,1,0.4f);

    void Update()
    {
        if (!RaceManager.Instance.IsPaused) return;

        float nav = ControllerInput.Instance?.MenuNav ?? 0f;
        bool resumeSelected = nav < 0.5f;

        resumeText.color  = resumeSelected ? selectedColor : unselectedColor;
        restartText.color = resumeSelected ? unselectedColor : selectedColor;
    }
}
```

---

### DisplaySender.cs

```csharp
using UnityEngine;

/// Envía la velocidad actual al Arduino cada 100ms para el display 7-seg.
public class DisplaySender : MonoBehaviour
{
    private float _timer;
    private const float INTERVAL = 0.1f;

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < INTERVAL) return;
        _timer = 0f;

        var car = FindObjectOfType<CarController>();
        if (car == null) return;

        int speed = Mathf.Max(0, Mathf.RoundToInt(car.CurrentSpeedKmh));
        speed = Mathf.Min(999, speed);
        SerialManager.Instance?.SendLine(speed.ToString());
    }
}
```

---

### PortSelectUI.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class PortSelectUI : MonoBehaviour
{
    public TMP_Dropdown portDropdown;
    public Button       connectButton;
    public TextMeshProUGUI statusText;

    void Start()
    {
        portDropdown.ClearOptions();
        var ports = SerialManager.Instance?.AvailablePorts;
        if (ports == null || ports.Length == 0)
        {
            statusText.text = "No serial ports found.";
            connectButton.interactable = false;
            return;
        }
        portDropdown.AddOptions(new System.Collections.Generic.List<string>(ports));
        connectButton.onClick.AddListener(OnConnect);
    }

    void OnConnect()
    {
        string port = portDropdown.options[portDropdown.value].text;
        try
        {
            SerialManager.Instance.Connect(port);
            statusText.text = $"Connected to {port}";
            SceneManager.LoadScene("Race");
        }
        catch (System.Exception e)
        {
            statusText.text = $"Error: {e.Message}";
        }
    }
}
```

---

### EndScreenUI.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class EndScreenUI : MonoBehaviour
{
    public TextMeshProUGUI[] lapLabels;   // 3 elementos
    public TextMeshProUGUI   totalLabel;
    public TextMeshProUGUI   bestLabel;
    public Button            restartButton;

    void Start()
    {
        var race = RaceManager.Instance;
        if (race == null) return;

        float total = 0f;
        for (int i = 0; i < race.LapTimes.Length; i++)
        {
            float t = race.LapTimes[i];
            total += t;
            if (i < lapLabels.Length)
                lapLabels[i].text = $"LAP {i+1}   {FormatTime(t)}";
        }
        totalLabel.text = "TOTAL   " + FormatTime(total);
        bestLabel.text  = "BEST    " + FormatTime(race.BestLap);

        restartButton.onClick.AddListener(() => SceneManager.LoadScene("Race"));
    }

    static string FormatTime(float t)
    {
        int m  = Mathf.FloorToInt(t / 60f);
        int s  = Mathf.FloorToInt(t % 60f);
        int ms = Mathf.FloorToInt((t * 100f) % 100f);
        return $"{m:00}:{s:00}.{ms:00}";
    }
}
```

---

## 8. Assets GLB

### Dónde descargar

| Asset | Fuente recomendada | Búsqueda sugerida |
|-------|-------------------|-------------------|
| Coche (body) | Sketchfab (free) | "low poly race car glb free" |
| Ruedas (4 iguales) | mismo pack del coche | ruedas como objetos separados |
| Circuito / pista | Sketchfab / itch.io | "race track glb" o "road circuit low poly" |

### Importación en Unity

1. Colocar archivos `.glb` en `Assets/Models/`.
2. Unity 2022+ los importa automáticamente vía GLTF importer.
3. **Coche:** arrastrar a escena, crear estructura de jerarquía:
   ```
   Car (GameObject vacío con Rigidbody + CarController)
   ├── CarBody (mesh GLB)
   ├── WheelFL (mesh rueda)
   ├── WheelFR
   ├── WheelRL
   └── WheelRR
   ```
4. **Circuito:** agregar `MeshCollider` al mesh de la pista. Marcar `Convex = false` para pistas complejas; usar `Static` para optimización.
5. **Tag del coche:** asignar tag `"Player"` al GameObject raíz del coche.

---

## 9. HUD y UI personalizada

Requisito de rúbrica: **ningún elemento UI por defecto de Unity**. Todo debe ser diseño propio.

### Elementos requeridos

| Elemento | Descripción |
|----------|-------------|
| Velocímetro | Texto grande, fuente custom, actualizado cada frame |
| Contador de vueltas | LAP X / 3, posición superior |
| Cronómetro vuelta actual | mm:ss.cc en tiempo real |
| Mejor vuelta | Fijo hasta que se mejore |
| Barra de turbo | Image tipo Filled, animada, con glow cuando está llena |
| Overlay de pausa | Fondo semitransparente, opciones Resume/Restart |
| Pantalla puerto serial | Dropdown + botón Conectar, pantalla de inicio |
| Pantalla final | Tiempos por vuelta, total, mejor vuelta |

### Fuentes y colores

- Importar una fuente `.ttf` o `.otf` custom (ej. Orbitron, Rajdhani — Google Fonts, libre).
- Crear un paleta de 2–3 colores consistente en todas las pantallas.
- No usar el font Arial ni botones/paneles por defecto de Unity UI.

---

## 10. Flujo de pantallas

```
[Inicio] → PortSelect.unity
             │
             │ (usuario elige COM port, click Conectar)
             ▼
           Race.unity  ←──────────────────────────────┐
             │  (3 vueltas completadas)                │
             │  (pausa → pot3 → Restart)               │
             ▼                                         │
           EndScreen.unity                             │
             │  (botón Restart)                        │
             └─────────────────────────────────────────┘
```

---

## 11. Sistema de juego

### Lógica de velocidad

- `pot2` (0–1023) → normalizado 0–1 → multiplicado por `maxSpeed` (m/s).
- Lerp suave con factor ~4 para evitar cambios bruscos.
- Turbo: multiplica velocidad objetivo × 1.6 durante ~1.5s.
- Velocidad enviada al display: `Mathf.Max(0, Mathf.RoundToInt(speed_kmh))`.

### Lógica de turbo

```
Estado inicial: carga = 1.0 (lleno)

[Tap botón turbo]
  └── si carga == 1.0 y no activo:
        carga = 0.0
        activar burst (1.5s)
        iniciar recarga (3s)

Durante recarga:
  carga += deltaTime / 3.0
  clamp 0–1

HUD: barra fill = carga
     glow activo si carga >= 1.0
```

### Detección de vuelta

- Un `BoxCollider` con `IsTrigger = true` cruza la línea de meta.
- `LapTrigger.cs` registra el paso del coche (tag `"Player"`).
- Primera vuelta inicia en el `Start()` de `RaceManager`.
- Al completar 3 vueltas → `LoadScene("EndScreen")`.

### Métricas registradas

- Tiempo de cada vuelta (float, segundos).
- Mejor vuelta (float, actualizado en tiempo real).
- Tiempo total = suma de las 3 vueltas.

---

## 12. Build y entregables

### Configuración Build Settings (Unity)

1. File → Build Settings
2. Agregar escenas en orden: `PortSelect` (0), `Race` (1), `EndScreen` (2).
3. Platform: PC, Mac & Linux Standalone.
4. Architecture: x86_64.
5. Build → carpeta `Build/`.

### Checklist pre-build

- [ ] `SerialManager` persiste entre escenas (`DontDestroyOnLoad`).
- [ ] `RaceManager` persiste entre escenas (o se recrea en `Race`).
- [ ] No hay referencias a `Input.GetKey` (todo viene del Arduino).
- [ ] La escena `PortSelect` es la primera en Build Settings.
- [ ] El dropdown de puertos funciona sin conectar el Arduino (muestra lista vacía sin crashear).
- [ ] El display 7-seg recibe valores correctamente en runtime.

### Archivos a entregar

```
/Build
  └── RacingGame.exe  (+ carpeta _Data)
README.md
```

---

## 13. Repositorio GitHub

### Estructura recomendada

```
/
├── Unity/                    ← proyecto Unity completo
│   ├── Assets/
│   ├── Packages/
│   └── ProjectSettings/
├── Arduino/
│   └── racing_controller/
│       └── racing_controller.ino
├── Build/                    ← build ejecutable (o link a release)
├── Docs/
│   └── wiring_diagram.png    ← foto/diagrama del cableado
└── README.md
```

### .gitignore para Unity

```
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
*.pidb
*.suo
*.user
*.userprefs
*.unityproj
.DS_Store
```

### README mínimo del repo

- Descripción del proyecto (1 párrafo).
- Lista de componentes hardware.
- Instrucciones de conexión (qué pin va a qué).
- Cómo abrir el proyecto Unity.
- Cómo cargar el sketch en el Arduino.
- Cómo ejecutar el build.

---

## 14. Checklist de rúbrica

| Criterio | Peso | Requisito | Estado |
|----------|------|-----------|--------|
| Comunicación bidireccional | 20% | Arduino ↔ Unity via Serial | pot+btn → Unity; speed → display |
| Componente físico — funcionamiento | 10% | ≥3 entradas + actuador funcional | 3 pots + 2 btns + 7-seg ✓ |
| Componente físico — apariencia | 10% | Carcasa, no protoboard expuesta | Diseñar carcasa |
| Experiencia Unity — funcionamiento | 20% | Build ejecutable, selector de puerto, sin crashes | Implementar y testear |
| Experiencia Unity — UI/UX | 10% | Sin defaults Unity, diseño propio, coherencia visual | Fuente + paleta custom |
| Coherencia física-digital | 10% | Hardware con rol claro en el juego | Controlador = volante de carrera ✓ |
| Presentación en clase | 10% | Demo en vivo funcional | Practicar demo completa |
| Seguimiento de instrucciones | 10% | Build + repositorio GitHub entregados | Entregar antes del cierre |

---

*Documento generado como guía de implementación. Actualizar conforme avance el desarrollo.*

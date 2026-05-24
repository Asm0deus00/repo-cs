using UnityEngine;

/// <summary>
/// Envía la velocidad actual del coche al Arduino cada 100ms
/// para que el display 7 segmentos la muestre.
/// Valor siempre clampeado a 0–999 y nunca negativo.
/// </summary>
public class DisplaySender : MonoBehaviour
{
    [Tooltip("Intervalo de envío en segundos (0.1 = 100ms)")]
    public float sendInterval = 0.1f;

    private float         _timer;
    private CarController _car;

    void Update()
    {
        if (_car == null) _car = FindObjectOfType<CarController>();

        _timer += Time.deltaTime;
        if (_timer < sendInterval) return;
        _timer = 0f;

        if (_car == null || SerialManager.Instance == null) return;

        int speed = Mathf.Clamp(Mathf.RoundToInt(_car.CurrentSpeedKmh), 0, 999);
        SerialManager.Instance.SendLine(speed.ToString());
    }
}

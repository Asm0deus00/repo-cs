using UnityEngine;

/// <summary>
/// Colócalo SOLO en la escena PortSelect (primera escena).
/// Crea los singletons SerialManager y ControllerInput si no existen aún.
/// Como ambos tienen DontDestroyOnLoad, persisten en todas las escenas siguientes.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    void Awake()
    {
        // SerialManager
        if (SerialManager.Instance == null)
        {
            var go = new GameObject("SerialManager");
            go.AddComponent<SerialManager>();
        }

        // ControllerInput
        if (ControllerInput.Instance == null)
        {
            var go = new GameObject("ControllerInput");
            go.AddComponent<ControllerInput>();
        }
    }
}

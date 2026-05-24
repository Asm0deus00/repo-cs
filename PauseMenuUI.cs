using UnityEngine;
using TMPro;

/// <summary>
/// Actualiza el menú de pausa según la posición del potenciómetro 3.
/// pot3 < 0.5  → Resume  seleccionado
/// pot3 >= 0.5 → Restart seleccionado
/// La confirmación la maneja RaceManager al detectar el tap de pausa.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Header("Option texts")]
    public TextMeshProUGUI resumeText;
    public TextMeshProUGUI restartText;

    [Header("Selection visuals")]
    public Color selectedColor   = Color.white;
    public Color unselectedColor = new Color(1f, 1f, 1f, 0.35f);

    [Tooltip("Flecha / cursor que se mueve entre las opciones")]
    public RectTransform selectionCursor;
    public RectTransform resumePosition;
    public RectTransform restartPosition;

    void Update()
    {
        var race = RaceManager.Instance;
        if (race == null || !race.IsPaused) return;

        float nav = ControllerInput.Instance?.MenuNav ?? 0f;
        bool resumeSelected = nav < 0.5f;

        // Colores
        if (resumeText  != null) resumeText.color  = resumeSelected ? selectedColor : unselectedColor;
        if (restartText != null) restartText.color  = resumeSelected ? unselectedColor : selectedColor;

        // Mover cursor si existe
        if (selectionCursor != null)
        {
            Vector3 target = resumeSelected
                ? (resumePosition  != null ? resumePosition.position  : resumeText.transform.position)
                : (restartPosition != null ? restartPosition.position : restartText.transform.position);

            selectionCursor.position = Vector3.Lerp(
                selectionCursor.position, target, Time.unscaledDeltaTime * 12f);
        }
    }
}

using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WildcardManager : MonoBehaviour
{
    public static WildcardManager Instance;

    [Header("Estado")]
    public int wildcardsDisponibles = 2; // Inicia con 2 comodines para demostrar reestructuración
    public bool comodinActivo = false;

    [Header("UI")]
    public TextMeshProUGUI textoContador;
    public Button botonComodin;

    private Image buttonImage;
    private TextMeshProUGUI buttonLabel;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (botonComodin != null)
        {
            buttonImage = botonComodin.GetComponent<Image>();
            buttonLabel = botonComodin.GetComponentInChildren<TextMeshProUGUI>();
            botonComodin.onClick.RemoveAllListeners();
            botonComodin.onClick.AddListener(ToggleComodin);
        }
        ActualizarUI();
    }

    public void ToggleComodin()
    {
        if (wildcardsDisponibles > 0)
        {
            comodinActivo = !comodinActivo;
            ActualizarUI();

            if (FeedbackManager.Instance != null)
            {
                string msg = comodinActivo
                    ? "Comodín de Reestructuración ACTIVO: ahora podés mover o desconectar un mando con toda su rama a cargo."
                    : "Modo Comodín desactivado.";
                Color c = comodinActivo ? new Color(0.75f, 0.52f, 0.08f, 0.95f) : new Color(0.2f, 0.25f, 0.32f, 0.9f);
                FeedbackManager.Instance.MostrarMensajeEstado(msg, c, 2.8f);
            }
        }
        else
        {
            // Para la demostración académica recargamos 1 comodín de consultoría externa cuando se agota
            RecompensarComodin(1);
            if (FeedbackManager.Instance != null)
            {
                FeedbackManager.Instance.MostrarMensajeEstado(
                    "Consultoría Organizacional: +1 Comodín de Reestructuración otorgado.",
                    new Color(0.18f, 0.42f, 0.68f, 0.95f),
                    2.6f
                );
            }
        }
    }

    public void ConsumirComodin()
    {
        wildcardsDisponibles = Mathf.Max(0, wildcardsDisponibles - 1);
        comodinActivo = false;
        ActualizarUI();
        Debug.Log("Comodín consumido en reestructuración.");
    }

    public void RecompensarComodin(int cantidad)
    {
        wildcardsDisponibles += cantidad;
        ActualizarUI();
    }

    private void ActualizarUI()
    {
        if (textoContador != null)
        {
            textoContador.text = $"x{wildcardsDisponibles}";
        }

        if (buttonImage != null)
        {
            buttonImage.color = comodinActivo
                ? new Color(1.0f, 0.84f, 0.25f, 1.0f) // Dorado activo
                : Color.white;
        }

        if (buttonLabel != null)
        {
            buttonLabel.text = comodinActivo ? "Reestruct. [ON]" : "Comodín";
        }
    }
}
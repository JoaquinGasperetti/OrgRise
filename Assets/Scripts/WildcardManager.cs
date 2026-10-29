using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WildcardManager : MonoBehaviour
{
    public static WildcardManager Instance;

    [Header("Estado")]
    public int wildcardsDisponibles = 2;
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
        NormalizarBotonComodinVertical();

        if (botonComodin != null)
        {
            buttonImage = botonComodin.GetComponent<Image>();
            buttonLabel = BuscarTextoPrincipalBoton();
            botonComodin.onClick.RemoveAllListeners();
            botonComodin.onClick.AddListener(ToggleComodin);
        }
        ActualizarUI();
    }

    /// <summary>
    /// Corrige la escala 2.96x y la posición desplazada (x: 234) que traía ComodinButton en SampleScene.unity
    /// para que quede centrado y prolijo en la barra inferior del móvil vertical.
    /// </summary>
    private void NormalizarBotonComodinVertical()
    {
        if (botonComodin == null) return;

        RectTransform btnRt = botonComodin.GetComponent<RectTransform>();
        if (btnRt != null)
        {
            btnRt.localScale = Vector3.one;
            btnRt.anchorMin = new Vector2(0.18f, 0.085f);
            btnRt.anchorMax = new Vector2(0.82f, 0.145f);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;
        }

        TextMeshProUGUI mainLabel = BuscarTextoPrincipalBoton();
        if (mainLabel != null)
        {
            RectTransform lblRt = mainLabel.GetComponent<RectTransform>();
            if (lblRt != null)
            {
                lblRt.localScale = Vector3.one;
                lblRt.anchorMin = new Vector2(0.04f, 0.08f);
                lblRt.anchorMax = new Vector2(0.74f, 0.92f);
                lblRt.offsetMin = Vector2.zero;
                lblRt.offsetMax = Vector2.zero;
            }
            mainLabel.margin = Vector4.zero;
            mainLabel.alignment = TextAlignmentOptions.Center;
            mainLabel.enableAutoSizing = true;
            mainLabel.fontSizeMin = 16f;
            mainLabel.fontSizeMax = 28f;
            mainLabel.fontStyle = FontStyles.Bold;
        }

        if (textoContador != null)
        {
            RectTransform countRt = textoContador.GetComponent<RectTransform>();
            if (countRt != null)
            {
                countRt.localScale = Vector3.one;
                countRt.anchorMin = new Vector2(0.74f, 0.08f);
                countRt.anchorMax = new Vector2(0.96f, 0.92f);
                countRt.offsetMin = Vector2.zero;
                countRt.offsetMax = Vector2.zero;
            }
            textoContador.margin = Vector4.zero;
            textoContador.color = new Color(0.12f, 0.15f, 0.22f, 1f);
            textoContador.alignment = TextAlignmentOptions.Center;
            textoContador.enableAutoSizing = true;
            textoContador.fontSizeMin = 16f;
            textoContador.fontSizeMax = 28f;
            textoContador.fontStyle = FontStyles.Bold;
        }
    }

    private TextMeshProUGUI BuscarTextoPrincipalBoton()
    {
        if (botonComodin == null) return null;
        TextMeshProUGUI[] textos = botonComodin.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in textos)
        {
            if (t != textoContador) return t;
        }
        return null;
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
                    ? "Comodín ACTIVO: ahora podés mover o desconectar un mando con toda su rama a cargo."
                    : "Modo Comodín desactivado.";
                Color c = comodinActivo ? new Color(0.75f, 0.52f, 0.08f, 0.95f) : new Color(0.2f, 0.25f, 0.32f, 0.9f);
                FeedbackManager.Instance.MostrarMensajeEstado(msg, c, 2.8f);
            }
        }
        else
        {
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
                ? new Color(1.0f, 0.84f, 0.25f, 1.0f)
                : new Color(0.92f, 0.95f, 0.99f, 1.0f);
        }

        if (buttonLabel != null)
        {
            buttonLabel.text = comodinActivo ? "⚡ Reestructuración [ON]" : "⚡ Comodín Reestructurar";
        }
    }
}
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
    private Button botonVerAnuncioBonificado;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        NormalizarBotonComodinVertical();
        CrearBotonBonificadoEnTablero();

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
    /// Coloca el botón Comodín en la franja 13.8% a 19.2% de la pantalla (a la izquierda)
    /// dejando espacio a su derecha para el botón de Anuncio Bonificado (+1 ⚡) y
    /// respetando la separación de seguridad sobre el Banner inferior de AdMob (0% a 6.5%).
    /// </summary>
    private void NormalizarBotonComodinVertical()
    {
        if (botonComodin == null) return;

        RectTransform btnRt = botonComodin.GetComponent<RectTransform>();
        if (btnRt != null)
        {
            btnRt.localScale = Vector3.one;
            btnRt.anchorMin = new Vector2(0.04f, 0.138f);
            btnRt.anchorMax = new Vector2(0.62f, 0.192f);
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
                lblRt.anchorMax = new Vector2(0.75f, 0.92f);
                lblRt.offsetMin = Vector2.zero;
                lblRt.offsetMax = Vector2.zero;
            }
            mainLabel.margin = Vector4.zero;
            mainLabel.alignment = TextAlignmentOptions.Center;
            mainLabel.enableAutoSizing = true;
            mainLabel.fontSizeMin = 13f;
            mainLabel.fontSizeMax = 24f;
            mainLabel.fontStyle = FontStyles.Bold;
        }

        if (textoContador != null)
        {
            RectTransform countRt = textoContador.GetComponent<RectTransform>();
            if (countRt != null)
            {
                countRt.localScale = Vector3.one;
                countRt.anchorMin = new Vector2(0.75f, 0.08f);
                countRt.anchorMax = new Vector2(0.96f, 0.92f);
                countRt.offsetMin = Vector2.zero;
                countRt.offsetMax = Vector2.zero;
            }
            textoContador.margin = Vector4.zero;
            textoContador.color = new Color(0.12f, 0.15f, 0.22f, 1f);
            textoContador.alignment = TextAlignmentOptions.Center;
            textoContador.enableAutoSizing = true;
            textoContador.fontSizeMin = 14f;
            textoContador.fontSizeMax = 25f;
            textoContador.fontStyle = FontStyles.Bold;
        }
    }

    /// <summary>
    /// Crea un botón voluntario (Opt-In) de Anuncio Bonificado de AdMob junto al botón de Comodín
    /// indicando explícitamente la acción y la recompensa (+1 ⚡ Comodín por ver anuncio).
    /// </summary>
    private void CrearBotonBonificadoEnTablero()
    {
        if (botonVerAnuncioBonificado != null || botonComodin == null) return;

        Transform parent = botonComodin.transform.parent;
        if (parent == null) return;

        GameObject btnObj = new GameObject("BotonAnuncioBonificadoComodin", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.anchorMin = new Vector2(0.64f, 0.138f);
        rt.anchorMax = new Vector2(0.96f, 0.192f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(0.42f, 0.22f, 0.58f, 0.98f);

        botonVerAnuncioBonificado = btnObj.GetComponent<Button>();
        botonVerAnuncioBonificado.onClick.AddListener(SolicitarComodinConAnuncioBonificado);

        GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.localScale = Vector3.one;
        txtRt.anchorMin = new Vector2(0.05f, 0.08f);
        txtRt.anchorMax = new Vector2(0.95f, 0.92f);
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = "🎬 +1 ⚡ (Video)";
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 12f;
        tmp.fontSizeMax = 22f;
        tmp.raycastTarget = false;
    }

    public void SolicitarComodinConAnuncioBonificado()
    {
        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.MostrarAnuncioBonificado(1, ActualizarUI);
        }
        else
        {
            RecompensarComodin(1);
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
            // Si no le quedan comodines y presiona el botón, iniciamos el anuncio Bonificado de AdMob por +1 Comodín
            if (AdMobManager.Instance != null)
            {
                AdMobManager.Instance.MostrarAnuncioBonificado(1, ActualizarUI);
            }
            else
            {
                RecompensarComodin(1);
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
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.ActualizarBadgeMenu();
        }
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
            if (wildcardsDisponibles <= 0 && !comodinActivo)
            {
                buttonLabel.text = "🎬 Obtener Comodín (+1 ⚡)";
            }
            else
            {
                buttonLabel.text = comodinActivo ? "⚡ Reestructuración [ON]" : "⚡ Comodín Reestructurar";
            }
        }
    }
}
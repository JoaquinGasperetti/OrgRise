using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Gestiona los Comodines de Reestructuración (⚡) y el sistema de Auto-Pista / Headhunter (🎯)
/// con alta conversión de anuncios bonificados (Rewarded Ads) para maximizar la rentabilidad.
/// </summary>
public class WildcardManager : MonoBehaviour
{
    public static WildcardManager Instance;

    private const string PrefsKeyWildcards = "OrgRise_WildcardsDisponibles";

    [Header("Estado")]
    public int wildcardsDisponibles = 2;
    public bool comodinActivo = false;

    [Header("UI")]
    public TextMeshProUGUI textoContador;
    public Button botonComodin;

    private Image buttonImage;
    private TextMeshProUGUI buttonLabel;
    private Button botonHeadhunterHint;
    private TextMeshProUGUI headhunterLabel;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            CargarPersistenciaComodines();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CargarPersistenciaComodines()
    {
        wildcardsDisponibles = PlayerPrefs.GetInt(PrefsKeyWildcards, 2);
    }

    private void GuardarPersistenciaComodines()
    {
        PlayerPrefs.SetInt(PrefsKeyWildcards, wildcardsDisponibles);
        PlayerPrefs.Save();
    }

    void Start()
    {
        NormalizarBotonComodinVertical();
        CrearBotonHeadhunterEnTablero();

        if (botonComodin != null)
        {
            buttonImage = botonComodin.GetComponent<Image>();
            buttonLabel = BuscarTextoPrincipalBoton();
            botonComodin.onClick.RemoveAllListeners();
            botonComodin.onClick.AddListener(ToggleComodin);
        }
        ActualizarUI();
    }

    private void NormalizarBotonComodinVertical()
    {
        if (botonComodin == null) return;

        RectTransform btnRt = botonComodin.GetComponent<RectTransform>();
        if (btnRt != null)
        {
            btnRt.localScale = Vector3.one;
            btnRt.anchorMin = new Vector2(0.03f, 0.138f);
            btnRt.anchorMax = new Vector2(0.51f, 0.194f);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;
        }

        Image bgImg = botonComodin.GetComponent<Image>();
        if (bgImg != null)
        {
            VectorUIKit.ApplyRoundedStyle(bgImg, new Color(0.14f, 0.22f, 0.34f, 0.98f), pill: true, addShadow: true);
            VectorUIKit.ApplyOutline(bgImg, new Color(1f, 1f, 1f, 0.22f), 1.2f);
        }

        TextMeshProUGUI mainLabel = BuscarTextoPrincipalBoton();
        if (mainLabel != null)
        {
            RectTransform lblRt = mainLabel.GetComponent<RectTransform>();
            if (lblRt != null)
            {
                lblRt.localScale = Vector3.one;
                lblRt.anchorMin = new Vector2(0.05f, 0.08f);
                lblRt.anchorMax = new Vector2(0.76f, 0.92f);
                lblRt.offsetMin = Vector2.zero;
                lblRt.offsetMax = Vector2.zero;
            }
            mainLabel.margin = Vector4.zero;
            mainLabel.alignment = TextAlignmentOptions.Center;
            mainLabel.enableAutoSizing = true;
            mainLabel.fontSizeMin = 11f;
            mainLabel.fontSizeMax = 21f;
            mainLabel.fontStyle = FontStyles.Bold;
            mainLabel.color = Color.white;
        }

        if (textoContador != null)
        {
            RectTransform countRt = textoContador.GetComponent<RectTransform>();
            if (countRt != null)
            {
                countRt.localScale = Vector3.one;
                countRt.anchorMin = new Vector2(0.76f, 0.08f);
                countRt.anchorMax = new Vector2(0.96f, 0.92f);
                countRt.offsetMin = Vector2.zero;
                countRt.offsetMax = Vector2.zero;
            }
            textoContador.margin = Vector4.zero;
            textoContador.color = VectorUIKit.AccentGold;
            textoContador.alignment = TextAlignmentOptions.Center;
            textoContador.enableAutoSizing = true;
            textoContador.fontSizeMin = 13f;
            textoContador.fontSizeMax = 23f;
            textoContador.fontStyle = FontStyles.Bold;
        }
    }

    /// <summary>
    /// Crea el botón comercial "🎯 Auto-Pista / Headhunter" que conecta automáticamente un puesto pendiente
    /// al mirar un video bonificado (o consumir comodín si tiene más de 3).
    /// </summary>
    private void CrearBotonHeadhunterEnTablero()
    {
        if (botonHeadhunterHint != null || botonComodin == null) return;

        Transform parent = botonComodin.transform.parent;
        if (parent == null) return;

        GameObject btnObj = new GameObject("BotonHeadhunterAutoPista", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.anchorMin = new Vector2(0.53f, 0.138f);
        rt.anchorMax = new Vector2(0.97f, 0.194f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = btnObj.GetComponent<Image>();
        VectorUIKit.ApplyRoundedStyle(img, new Color(0.44f, 0.22f, 0.68f, 0.98f), pill: true, addShadow: true);
        VectorUIKit.ApplyOutline(img, new Color(0.85f, 0.70f, 1f, 0.35f), 1.2f);

        botonHeadhunterHint = btnObj.GetComponent<Button>();
        botonHeadhunterHint.onClick.AddListener(AlPresionarBotonHeadhunter);

        GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.localScale = Vector3.one;
        txtRt.anchorMin = new Vector2(0.05f, 0.08f);
        txtRt.anchorMax = new Vector2(0.95f, 0.92f);
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        headhunterLabel = txtObj.GetComponent<TextMeshProUGUI>();
        headhunterLabel.text = "🎯 Auto-Pista (🎬 Video)";
        headhunterLabel.color = Color.white;
        headhunterLabel.fontStyle = FontStyles.Bold;
        headhunterLabel.alignment = TextAlignmentOptions.Center;
        headhunterLabel.enableAutoSizing = true;
        headhunterLabel.fontSizeMin = 11f;
        headhunterLabel.fontSizeMax = 20f;
        headhunterLabel.raycastTarget = false;
    }

    public void AlPresionarBotonHeadhunter()
    {
        if (LevelManager.Instance == null) return;

        // Si el jugador decide usar el Headhunter mediante anuncio bonificado, se conecta 1 nodo automáticamente y además suma +1 comodín
        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.MostrarAnuncioBonificado(1, () =>
            {
                LevelManager.Instance.AplicarAutoPistaHeadhunter();
                ActualizarUI();
            });
        }
        else
        {
            LevelManager.Instance.AplicarAutoPistaHeadhunter();
        }
    }

    public void SolicitarComodinConAnuncioBonificado()
    {
        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.MostrarAnuncioBonificado(2, ActualizarUI);
        }
        else
        {
            RecompensarComodin(2);
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
                    ? "⚡ Comodín ACTIVO: podés mover o desconectar cualquier líder con equipo a cargo."
                    : "Modo Comodín desactivado.";
                Color c = comodinActivo ? new Color(0.78f, 0.52f, 0.08f, 0.96f) : new Color(0.18f, 0.24f, 0.34f, 0.94f);
                FeedbackManager.Instance.MostrarMensajeEstado(msg, c, 2.5f);
            }
        }
        else
        {
            SolicitarComodinConAnuncioBonificado();
        }
    }

    public void ConsumirComodin()
    {
        wildcardsDisponibles = Mathf.Max(0, wildcardsDisponibles - 1);
        comodinActivo = false;
        GuardarPersistenciaComodines();
        ActualizarUI();
    }

    public void RecompensarComodin(int cantidad)
    {
        wildcardsDisponibles += cantidad;
        GuardarPersistenciaComodines();
        ActualizarUI();
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.ActualizarBadgeMenu();
        }
    }

    public void RestablecerComodinesPorDefecto()
    {
        wildcardsDisponibles = 2;
        comodinActivo = false;
        GuardarPersistenciaComodines();
        ActualizarUI();
    }

    private void ActualizarUI()
    {
        if (textoContador != null)
        {
            textoContador.text = $"x{wildcardsDisponibles}";
            textoContador.color = comodinActivo ? new Color(0.10f, 0.14f, 0.22f, 1f) : VectorUIKit.AccentGold;
        }

        if (buttonImage != null)
        {
            buttonImage.color = comodinActivo
                ? VectorUIKit.AccentGold
                : new Color(0.14f, 0.22f, 0.34f, 0.98f);
        }

        if (buttonLabel != null)
        {
            buttonLabel.color = comodinActivo ? new Color(0.08f, 0.12f, 0.20f, 1f) : Color.white;
            if (wildcardsDisponibles <= 0 && !comodinActivo)
            {
                buttonLabel.text = "🎬 +2 ⚡ Comodines";
            }
            else
            {
                buttonLabel.text = comodinActivo ? "⚡ Reestructurar [ON]" : "⚡ Reestructurar";
            }
        }
    }
}
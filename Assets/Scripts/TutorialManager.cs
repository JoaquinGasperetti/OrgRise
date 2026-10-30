using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Referencias UI")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI tutorialText;
    public Button botonContinuar;

    private Button botonIrAlMenuEnVictoria;
    private Button botonBonificadoEnVictoria;

    private int pasoActual = 0;
    private bool tutorialActivo = false;
    private bool enPantallaVictoria = false;
    private bool tutorialYaMostradoEnSesion = false;

    private readonly string[] instrucciones = new string[]
    {
        "<b>🏢 Bienvenido a OrgRise</b>\n\nTu objetivo es diseñar estructuras organizacionales eficientes aplicando los principios de <b>Gestión de la Organización</b>.",
        "<b>1. Cadena y Unidad de Mando</b>\n\nArrastrá una línea entre un subordinado y su jefe inmediato (Empleado → Gerente → Director → CEO).\n\nPodés cerrar este aviso y hacer tu primera conexión.",
        "<b>2. Tramo de Control</b>\n\nCada superior tiene un límite máximo de reportes directos (<b>Tramo: X/Max</b>). Si lo superás, se produce un cuello de botella.",
        "<b>3. Departamentalización y Reestructuración</b>\n\nRespetá el área/color de cada unidad. Hacé <b>doble toque</b> sobre un nodo para desconectarlo, o usá el <b>Comodín</b> para mover una rama completa."
    };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ConfigurarEsteticaPanelVertical();

        if (botonContinuar != null)
        {
            botonContinuar.onClick.RemoveAllListeners();
            botonContinuar.onClick.AddListener(AlPresionarBotonPrincipal);
        }

        // Si no hay LevelManager (escena aislada), iniciamos el tutorial; de lo contrario, LevelManager abre primero el Menú Principal
        if (LevelManager.Instance == null && !tutorialActivo && !enPantallaVictoria && !tutorialYaMostradoEnSesion)
        {
            IniciarTutorial();
        }
    }

    public void OcultarPanelInmediato()
    {
        tutorialActivo = false;
        enPantallaVictoria = false;
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }

    private void ConfigurarEsteticaPanelVertical()
    {
        if (tutorialPanel != null)
        {
            RectTransform panelRt = tutorialPanel.GetComponent<RectTransform>();
            if (panelRt != null)
            {
                panelRt.localScale = Vector3.one;
                // Dejamos libre 0.00 a 0.065 abajo para no tapar ni tocar el Banner de AdMob
                panelRt.anchorMin = new Vector2(0f, 0.065f);
                panelRt.anchorMax = new Vector2(1f, 1f);
                panelRt.offsetMin = Vector2.zero;
                panelRt.offsetMax = Vector2.zero;
            }

            Image panelImg = tutorialPanel.GetComponent<Image>();
            if (panelImg != null)
            {
                panelImg.color = new Color(0.04f, 0.07f, 0.12f, 0.94f);
                panelImg.raycastTarget = true;
            }
        }

        if (tutorialText != null)
        {
            RectTransform txtRt = tutorialText.GetComponent<RectTransform>();
            if (txtRt != null)
            {
                txtRt.localScale = Vector3.one;
                txtRt.anchorMin = new Vector2(0.08f, 0.39f);
                txtRt.anchorMax = new Vector2(0.92f, 0.86f);
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;
            }

            tutorialText.margin = Vector4.zero;
            tutorialText.enableAutoSizing = true;
            tutorialText.fontSizeMin = 18f;
            tutorialText.fontSizeMax = 34f;
            tutorialText.alignment = TextAlignmentOptions.Center;
        }

        if (botonContinuar != null)
        {
            RectTransform btnRt = botonContinuar.GetComponent<RectTransform>();
            if (btnRt != null)
            {
                btnRt.localScale = Vector3.one;
                btnRt.anchorMin = new Vector2(0.14f, 0.28f);
                btnRt.anchorMax = new Vector2(0.86f, 0.36f);
                btnRt.offsetMin = Vector2.zero;
                btnRt.offsetMax = Vector2.zero;
            }

            TextMeshProUGUI btnTxt = botonContinuar.GetComponentInChildren<TextMeshProUGUI>();
            if (btnTxt != null)
            {
                RectTransform btnTxtRt = btnTxt.GetComponent<RectTransform>();
                if (btnTxtRt != null)
                {
                    btnTxtRt.localScale = Vector3.one;
                    btnTxtRt.anchorMin = Vector2.zero;
                    btnTxtRt.anchorMax = Vector2.one;
                    btnTxtRt.offsetMin = new Vector2(12f, 6f);
                    btnTxtRt.offsetMax = new Vector2(-12f, -6f);
                }
                btnTxt.margin = Vector4.zero;
                btnTxt.enableAutoSizing = true;
                btnTxt.fontSizeMin = 17f;
                btnTxt.fontSizeMax = 30f;
                btnTxt.fontStyle = FontStyles.Bold;
                btnTxt.alignment = TextAlignmentOptions.Center;
            }
        }

        AsegurarBotonesEnModal();
    }

    private void AsegurarBotonesEnModal()
    {
        if (tutorialPanel == null) return;

        // Botón voluntario de Anuncio Bonificado en pantalla de victoria (+2 Comodines)
        if (botonBonificadoEnVictoria == null)
        {
            GameObject rewObj = new GameObject("BotonBonificadoVictoria", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            rewObj.transform.SetParent(tutorialPanel.transform, false);

            RectTransform rewRt = rewObj.GetComponent<RectTransform>();
            rewRt.localScale = Vector3.one;
            rewRt.anchorMin = new Vector2(0.14f, 0.185f);
            rewRt.anchorMax = new Vector2(0.86f, 0.26f);
            rewRt.offsetMin = Vector2.zero;
            rewRt.offsetMax = Vector2.zero;

            Image rewImg = rewObj.GetComponent<Image>();
            rewImg.color = new Color(0.42f, 0.22f, 0.58f, 1f);

            botonBonificadoEnVictoria = rewObj.GetComponent<Button>();
            botonBonificadoEnVictoria.onClick.AddListener(() =>
            {
                if (AdMobManager.Instance != null)
                {
                    AdMobManager.Instance.MostrarAnuncioBonificado(2);
                }
            });

            GameObject rewTxtObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            rewTxtObj.transform.SetParent(rewObj.transform, false);
            RectTransform rewTxtRt = rewTxtObj.GetComponent<RectTransform>();
            rewTxtRt.localScale = Vector3.one;
            rewTxtRt.anchorMin = Vector2.zero;
            rewTxtRt.anchorMax = Vector2.one;
            rewTxtRt.offsetMin = new Vector2(10f, 4f);
            rewTxtRt.offsetMax = new Vector2(-10f, -4f);

            TextMeshProUGUI rewTmp = rewTxtObj.GetComponent<TextMeshProUGUI>();
            rewTmp.text = "🎬 Bono Extra: +2 ⚡ Comodines (Ver Video)";
            rewTmp.color = Color.white;
            rewTmp.fontStyle = FontStyles.Bold;
            rewTmp.alignment = TextAlignmentOptions.Center;
            rewTmp.enableAutoSizing = true;
            rewTmp.fontSizeMin = 14f;
            rewTmp.fontSizeMax = 24f;
            rewTmp.raycastTarget = false;

            rewObj.SetActive(false);
        }

        // Botón Volver al Menú Principal
        if (botonIrAlMenuEnVictoria == null)
        {
            GameObject btnObj = new GameObject("BotonVolverAlMenu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(tutorialPanel.transform, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.localScale = Vector3.one;
            rt.anchorMin = new Vector2(0.14f, 0.09f);
            rt.anchorMax = new Vector2(0.86f, 0.165f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = btnObj.GetComponent<Image>();
            img.color = new Color(0.20f, 0.27f, 0.38f, 1f);

            botonIrAlMenuEnVictoria = btnObj.GetComponent<Button>();
            botonIrAlMenuEnVictoria.onClick.AddListener(() =>
            {
                OcultarPanelInmediato();
                if (LevelManager.Instance != null)
                {
                    LevelManager.Instance.MostrarMenuPrincipal();
                }
            });

            GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.localScale = Vector3.one;
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(10f, 4f);
            txtRt.offsetMax = new Vector2(-10f, -4f);

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "☰ Selector de Niveles / Menú";
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 15f;
            tmp.fontSizeMax = 25f;
            tmp.raycastTarget = false;

            btnObj.SetActive(false);
        }
    }

    public void IniciarTutorial()
    {
        if (tutorialYaMostradoEnSesion)
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            return;
        }

        ConfigurarEsteticaPanelVertical();

        tutorialYaMostradoEnSesion = true;
        enPantallaVictoria = false;
        tutorialActivo = true;
        pasoActual = 0;

        if (botonIrAlMenuEnVictoria != null)
        {
            botonIrAlMenuEnVictoria.gameObject.SetActive(false);
        }
        if (botonBonificadoEnVictoria != null)
        {
            botonBonificadoEnVictoria.gameObject.SetActive(false);
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            tutorialPanel.transform.SetAsLastSibling();
        }
        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.CrearFranjaSeguraBannerUI();
        }

        ActualizarTextoBoton("Continuar");
        MostrarPaso(pasoActual);
    }

    private void MostrarPaso(int indice)
    {
        if (indice >= instrucciones.Length)
        {
            FinalizarTutorial();
            return;
        }

        if (tutorialText != null)
        {
            tutorialText.text = instrucciones[indice];
        }

        if (botonContinuar != null)
        {
            botonContinuar.gameObject.SetActive(true);
            ActualizarTextoBoton(indice == instrucciones.Length - 1 ? "¡Empezar a Jugar!" : "Continuar");
        }
    }

    private void AlPresionarBotonPrincipal()
    {
        if (enPantallaVictoria)
        {
            enPantallaVictoria = false;
            if (tutorialPanel != null) tutorialPanel.SetActive(false);

            if (LevelManager.Instance != null)
            {
                if (AdMobManager.Instance != null)
                {
                    // En la transición natural de fin de nivel activamos el Intersticial Bonificado (con prompt opt-out reglamentario)
                    // o el Intersticial estándar si corresponde por conteo de acciones.
                    AdMobManager.Instance.IntentarTransicionVictoriaSiguienteNivel(
                        () => LevelManager.Instance.AvanzarSiguienteNivel(),
                        2
                    );
                }
                else
                {
                    LevelManager.Instance.AvanzarSiguienteNivel();
                }
            }
            return;
        }

        AvanzarPasoLectura();
    }

    public void AvanzarPasoLectura()
    {
        if (!tutorialActivo)
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            return;
        }

        pasoActual++;
        MostrarPaso(pasoActual);
    }

    public void NotificarConexionExitosa()
    {
        if (tutorialActivo && pasoActual == 1)
        {
            pasoActual++;
            MostrarPaso(pasoActual);
        }
    }

    private void FinalizarTutorial()
    {
        tutorialActivo = false;
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    public void MostrarPantallaNivelCompletado(string resumenAcademico)
    {
        ConfigurarEsteticaPanelVertical();

        tutorialActivo = false;
        enPantallaVictoria = true;

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            tutorialPanel.transform.SetAsLastSibling();
        }
        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.CrearFranjaSeguraBannerUI();
        }

        if (tutorialText != null)
        {
            tutorialText.text = $"<color=#69F0AE><b>¡ORGANIGRAMA VALIDADO!</b></color>\n\n{resumenAcademico}";
        }

        if (botonContinuar != null)
        {
            botonContinuar.gameObject.SetActive(true);
            ActualizarTextoBoton("Siguiente Nivel ➔");
        }

        if (botonBonificadoEnVictoria != null)
        {
            botonBonificadoEnVictoria.gameObject.SetActive(true);
        }

        if (botonIrAlMenuEnVictoria != null)
        {
            botonIrAlMenuEnVictoria.gameObject.SetActive(true);
        }
    }

    private void ActualizarTextoBoton(string texto)
    {
        if (botonContinuar == null) return;
        TextMeshProUGUI btnTxt = botonContinuar.GetComponentInChildren<TextMeshProUGUI>();
        if (btnTxt != null)
        {
            btnTxt.text = texto;
        }
    }
}
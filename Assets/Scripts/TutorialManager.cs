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
                panelRt.anchorMin = Vector2.zero;
                panelRt.anchorMax = Vector2.one;
                panelRt.offsetMin = Vector2.zero;
                panelRt.offsetMax = Vector2.zero;
            }

            Image panelImg = tutorialPanel.GetComponent<Image>();
            if (panelImg != null)
            {
                panelImg.color = new Color(0.04f, 0.07f, 0.12f, 0.92f);
                panelImg.raycastTarget = true;
            }
        }

        if (tutorialText != null)
        {
            RectTransform txtRt = tutorialText.GetComponent<RectTransform>();
            if (txtRt != null)
            {
                txtRt.localScale = Vector3.one;
                txtRt.anchorMin = new Vector2(0.08f, 0.36f);
                txtRt.anchorMax = new Vector2(0.92f, 0.80f);
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;
            }

            tutorialText.margin = Vector4.zero;
            tutorialText.enableAutoSizing = true;
            tutorialText.fontSizeMin = 20f;
            tutorialText.fontSizeMax = 36f;
            tutorialText.alignment = TextAlignmentOptions.Center;
        }

        if (botonContinuar != null)
        {
            RectTransform btnRt = botonContinuar.GetComponent<RectTransform>();
            if (btnRt != null)
            {
                btnRt.localScale = Vector3.one;
                btnRt.anchorMin = new Vector2(0.18f, 0.22f);
                btnRt.anchorMax = new Vector2(0.82f, 0.30f);
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
                btnTxt.fontSizeMin = 18f;
                btnTxt.fontSizeMax = 32f;
                btnTxt.fontStyle = FontStyles.Bold;
                btnTxt.alignment = TextAlignmentOptions.Center;
            }
        }

        AsegurarBotonMenuEnModal();
    }

    private void AsegurarBotonMenuEnModal()
    {
        if (botonIrAlMenuEnVictoria != null || tutorialPanel == null) return;

        GameObject btnObj = new GameObject("BotonVolverAlMenu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(tutorialPanel.transform, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.anchorMin = new Vector2(0.18f, 0.12f);
        rt.anchorMax = new Vector2(0.82f, 0.195f);
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
        tmp.fontSizeMin = 16f;
        tmp.fontSizeMax = 26f;

        btnObj.SetActive(false);
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

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            tutorialPanel.transform.SetAsLastSibling();
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
                LevelManager.Instance.AvanzarSiguienteNivel();
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

        if (tutorialText != null)
        {
            tutorialText.text = $"<color=#69F0AE><b>¡ORGANIGRAMA VALIDADO!</b></color>\n\n{resumenAcademico}";
        }

        if (botonContinuar != null)
        {
            botonContinuar.gameObject.SetActive(true);
            ActualizarTextoBoton("Siguiente Nivel ➔");
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
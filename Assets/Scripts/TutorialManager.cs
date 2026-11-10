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

    private GameObject modalCardContainer;
    private Button botonIrAlMenuEnVictoria;
    private Button botonBonificadoEnVictoria;
    private TextMeshProUGUI labelBotonBonificado;

    private int pasoActual = 0;
    private bool tutorialActivo = false;
    private bool enPantallaVictoria = false;
    private bool tutorialYaMostradoEnSesion = false;
    private int estrellasUltimaVictoria = 3;

    private readonly string[] instrucciones = new string[]
    {
        "<b>🏢 ¡Bienvenido a OrgRise!</b>\n\nConvertite en Arquitecto Corporativo: diseñá la estructura de startups y multinacionales para llevarlas al éxito.",
        "<b>1. Conectá la Jerarquía</b>\n\nArrastrá una línea desde un puesto hacia su líder superior (<b>👤 Especialista → 📊 Líder → 💼 VP → 👑 CEO</b>).\n\nLa línea brillará en <color=#10B981><b>VERDE</b></color> si la conexión es válida.",
        "<b>2. Cuidá el Cupo de Mando</b>\n\nCada líder tiene un límite de supervisión (<b>Cupo: X/Max</b>). Si se llena, deberás delegar en otro mando de la misma división.",
        "<b>3. Herramientas Ejecutivas</b>\n\n• <b>Doble toque</b> sobre una tarjeta para desconectarla.\n• Usá <b>⚡ Reestructurar</b> para mover líderes con equipo a cargo.\n• Usá <b>🎯 Auto-Pista</b> si necesitás que un Headhunter conecte un puesto clave."
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
                panelRt.anchorMin = new Vector2(0f, 0.065f);
                panelRt.anchorMax = new Vector2(1f, 1f);
                panelRt.offsetMin = Vector2.zero;
                panelRt.offsetMax = Vector2.zero;
            }

            Image panelImg = tutorialPanel.GetComponent<Image>();
            if (panelImg != null)
            {
                panelImg.color = new Color(0.03f, 0.06f, 0.12f, 0.94f);
                panelImg.raycastTarget = true;
            }

            // Tarjeta vectorial interior para el modal
            if (modalCardContainer == null)
            {
                Transform existing = tutorialPanel.transform.Find("VectorModalCardBg");
                if (existing != null)
                {
                    modalCardContainer = existing.gameObject;
                }
                else
                {
                    modalCardContainer = new GameObject("VectorModalCardBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    modalCardContainer.transform.SetParent(tutorialPanel.transform, false);
                    modalCardContainer.transform.SetAsFirstSibling();

                    RectTransform cardRt = modalCardContainer.GetComponent<RectTransform>();
                    cardRt.localScale = Vector3.one;
                    cardRt.anchorMin = new Vector2(0.05f, 0.06f);
                    cardRt.anchorMax = new Vector2(0.95f, 0.92f);
                    cardRt.offsetMin = Vector2.zero;
                    cardRt.offsetMax = Vector2.zero;

                    Image cardImg = modalCardContainer.GetComponent<Image>();
                    VectorUIKit.ApplyRoundedStyle(cardImg, new Color(0.09f, 0.14f, 0.23f, 0.99f), pill: false, addShadow: true);
                    VectorUIKit.ApplyOutline(cardImg, new Color(0.35f, 0.58f, 0.95f, 0.35f), 2f);
                    cardImg.raycastTarget = false;
                }
            }
        }

        if (tutorialText != null)
        {
            RectTransform txtRt = tutorialText.GetComponent<RectTransform>();
            if (txtRt != null)
            {
                txtRt.localScale = Vector3.one;
                txtRt.anchorMin = new Vector2(0.09f, 0.39f);
                txtRt.anchorMax = new Vector2(0.91f, 0.88f);
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;
            }

            tutorialText.margin = Vector4.zero;
            tutorialText.enableAutoSizing = true;
            tutorialText.fontSizeMin = 16f;
            tutorialText.fontSizeMax = 32f;
            tutorialText.alignment = TextAlignmentOptions.Center;
        }

        if (botonContinuar != null)
        {
            RectTransform btnRt = botonContinuar.GetComponent<RectTransform>();
            if (btnRt != null)
            {
                btnRt.localScale = Vector3.one;
                btnRt.anchorMin = new Vector2(0.12f, 0.285f);
                btnRt.anchorMax = new Vector2(0.88f, 0.365f);
                btnRt.offsetMin = Vector2.zero;
                btnRt.offsetMax = Vector2.zero;
            }

            Image btnImg = botonContinuar.GetComponent<Image>();
            if (btnImg != null)
            {
                VectorUIKit.ApplyRoundedStyle(btnImg, VectorUIKit.AccentEmerald, pill: true, addShadow: true);
                VectorUIKit.ApplyOutline(btnImg, new Color(1f, 1f, 1f, 0.30f), 1.5f);
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
                btnTxt.color = Color.white;
                btnTxt.enableAutoSizing = true;
                btnTxt.fontSizeMin = 17f;
                btnTxt.fontSizeMax = 28f;
                btnTxt.fontStyle = FontStyles.Bold;
                btnTxt.alignment = TextAlignmentOptions.Center;
            }
        }

        AsegurarBotonesEnModal();
    }

    private void AsegurarBotonesEnModal()
    {
        if (tutorialPanel == null) return;

        if (botonBonificadoEnVictoria == null)
        {
            GameObject rewObj = new GameObject("BotonBonificadoVictoria", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            rewObj.transform.SetParent(tutorialPanel.transform, false);

            RectTransform rewRt = rewObj.GetComponent<RectTransform>();
            rewRt.localScale = Vector3.one;
            rewRt.anchorMin = new Vector2(0.12f, 0.188f);
            rewRt.anchorMax = new Vector2(0.88f, 0.265f);
            rewRt.offsetMin = Vector2.zero;
            rewRt.offsetMax = Vector2.zero;

            Image rewImg = rewObj.GetComponent<Image>();
            VectorUIKit.ApplyRoundedStyle(rewImg, new Color(0.48f, 0.22f, 0.72f, 1f), pill: true, addShadow: true);
            VectorUIKit.ApplyOutline(rewImg, VectorUIKit.AccentGold, 1.5f);

            botonBonificadoEnVictoria = rewObj.GetComponent<Button>();
            botonBonificadoEnVictoria.onClick.AddListener(AlPresionarBonoVictoria);

            GameObject rewTxtObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            rewTxtObj.transform.SetParent(rewObj.transform, false);
            RectTransform rewTxtRt = rewTxtObj.GetComponent<RectTransform>();
            rewTxtRt.localScale = Vector3.one;
            rewTxtRt.anchorMin = Vector2.zero;
            rewTxtRt.anchorMax = Vector2.one;
            rewTxtRt.offsetMin = new Vector2(12f, 4f);
            rewTxtRt.offsetMax = new Vector2(-12f, -4f);

            labelBotonBonificado = rewTxtObj.GetComponent<TextMeshProUGUI>();
            labelBotonBonificado.text = "🎬 Bono Ejecutivo: +3 ⚡ Comodines (Ver Video)";
            labelBotonBonificado.color = Color.white;
            labelBotonBonificado.fontStyle = FontStyles.Bold;
            labelBotonBonificado.alignment = TextAlignmentOptions.Center;
            labelBotonBonificado.enableAutoSizing = true;
            labelBotonBonificado.fontSizeMin = 13f;
            labelBotonBonificado.fontSizeMax = 23f;
            labelBotonBonificado.raycastTarget = false;

            rewObj.SetActive(false);
        }

        if (botonIrAlMenuEnVictoria == null)
        {
            GameObject btnObj = new GameObject("BotonVolverAlMenu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(tutorialPanel.transform, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.localScale = Vector3.one;
            rt.anchorMin = new Vector2(0.12f, 0.095f);
            rt.anchorMax = new Vector2(0.88f, 0.168f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = btnObj.GetComponent<Image>();
            VectorUIKit.ApplyRoundedStyle(img, new Color(0.18f, 0.25f, 0.36f, 1f), pill: true, addShadow: true);
            VectorUIKit.ApplyOutline(img, new Color(1f, 1f, 1f, 0.20f), 1.2f);

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
            tmp.text = "☰ Mapa de Niveles / Menú";
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 14f;
            tmp.fontSizeMax = 24f;
            tmp.raycastTarget = false;

            btnObj.SetActive(false);
        }
    }

    private void AlPresionarBonoVictoria()
    {
        int recompensaComodines = estrellasUltimaVictoria < 3 ? 2 : 3;
        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.MostrarAnuncioBonificado(recompensaComodines, () =>
            {
                if (estrellasUltimaVictoria < 3 && LevelManager.Instance != null)
                {
                    LevelManager.Instance.MejorarNivelActualATresEstrellas();
                    estrellasUltimaVictoria = 3;
                }
                if (botonBonificadoEnVictoria != null)
                {
                    botonBonificadoEnVictoria.interactable = false;
                }
                if (labelBotonBonificado != null)
                {
                    labelBotonBonificado.text = "✓ ¡Bono 3⭐ + Comodines Acreditados!";
                }
            });
        }
        else
        {
            if (WildcardManager.Instance != null) WildcardManager.Instance.RecompensarComodin(recompensaComodines);
            if (estrellasUltimaVictoria < 3 && LevelManager.Instance != null)
            {
                LevelManager.Instance.MejorarNivelActualATresEstrellas();
            }
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

        ActualizarTextoBoton("Siguiente ➔");
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
            ActualizarTextoBoton(indice == instrucciones.Length - 1 ? "¡Comenzar Operación!" : "Siguiente ➔");
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

    public void MostrarPantallaNivelCompletado(string resumenComercial, int estrellasObtenidas = 3)
    {
        ConfigurarEsteticaPanelVertical();

        tutorialActivo = false;
        enPantallaVictoria = true;
        estrellasUltimaVictoria = estrellasObtenidas;

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
            tutorialText.text = $"<color=#34D399><b>¡EMPRESA OPTIMIZADA!</b></color>\n\n{resumenComercial}";
        }

        if (botonContinuar != null)
        {
            botonContinuar.gameObject.SetActive(true);
            ActualizarTextoBoton("Siguiente Nivel ➔");
        }

        if (botonBonificadoEnVictoria != null)
        {
            botonBonificadoEnVictoria.gameObject.SetActive(true);
            botonBonificadoEnVictoria.interactable = true;
            if (labelBotonBonificado != null)
            {
                labelBotonBonificado.text = estrellasObtenidas < 3
                    ? "🎬 Subir a 3⭐ + Bono +2 ⚡ (Ver Video)"
                    : "🎬 Bono Ejecutivo: +3 ⚡ Comodines (Ver Video)";
            }
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
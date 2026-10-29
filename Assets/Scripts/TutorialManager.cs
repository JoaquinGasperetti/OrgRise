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

    private int pasoActual = 0;
    private bool tutorialActivo = false;
    private bool enPantallaVictoria = false;
    private bool tutorialYaMostradoEnSesion = false;

    private readonly string[] instrucciones = new string[]
    {
        "<b>🏢 Bienvenido a OrgRise</b>\n\nTu objetivo es diseñar estructuras organizacionales eficientes aplicando los principios de <b>Gestión de la Organización</b>.",
        "<b>1. Cadena y Unidad de Mando</b>\n\nArrastrá una línea entre un subordinado y su jefe inmediato (Gerente → Director → CEO). Ahora hacé tu primera conexión válida en el tablero.",
        "<b>2. Tramo de Control (Span of Control)</b>\n\n¡Bien hecho! Cada superior tiene un límite máximo de reportes directos (<b>Tramo: X/Max</b>). Si lo superás, se produce un cuello de botella.",
        "<b>3. Departamentalización y Reestructuración</b>\n\nRespetá el color/área de cada unidad. Podés hacer <b>doble clic</b> sobre un nodo para desconectarlo, o usar el <b>Comodín</b> para mover una rama completa.\n\n¡Completá el organigrama!"
    };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ConfigurarEsteticaPanel();

        if (botonContinuar != null)
        {
            botonContinuar.onClick.RemoveAllListeners();
            botonContinuar.onClick.AddListener(AlPresionarBotonPrincipal);
        }

        // Si LevelManager aún no llamó a IniciarTutorial, lo iniciamos automáticamente
        if (!tutorialActivo && !enPantallaVictoria && !tutorialYaMostradoEnSesion)
        {
            IniciarTutorial();
        }
    }

    private void ConfigurarEsteticaPanel()
    {
        if (tutorialText != null)
        {
            tutorialText.enableAutoSizing = true;
            tutorialText.fontSizeMin = 18f;
            tutorialText.fontSizeMax = 30f;
            tutorialText.alignment = TextAlignmentOptions.Center;
        }

        // Permitimos que el fondo oscuro no bloquee el arrastre de los nodos cuando estamos en el paso práctico
        if (tutorialPanel != null)
        {
            Image panelImg = tutorialPanel.GetComponent<Image>();
            if (panelImg != null)
            {
                panelImg.raycastTarget = false;
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

        tutorialYaMostradoEnSesion = true;
        enPantallaVictoria = false;
        tutorialActivo = true;
        pasoActual = 0;

        if (tutorialPanel != null) tutorialPanel.SetActive(true);
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
            // En el paso 1 invitamos a conectar un nodo, pero también dejamos el botón como "Entendido / Saltar"
            // para que nunca se bloquee la partida si el usuario prefiere cerrar el cartel primero
            botonContinuar.gameObject.SetActive(true);
            ActualizarTextoBoton(indice == instrucciones.Length - 1 ? "¡Empezar!" : "Continuar");
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

    /// <summary>
    /// Se llama desde UINodeInteraction cuando el jugador hace una conexión exitosa.
    /// </summary>
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

    /// <summary>
    /// Muestra el resumen académico al completar un nivel y permite avanzar al siguiente desafío.
    /// </summary>
    public void MostrarPantallaNivelCompletado(string resumenAcademico)
    {
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
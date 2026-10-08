using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Referencias UI")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI tutorialText;
    public Button botonContinuar; // Opcional, para pasos que solo son de lectura

    private int pasoActual = 0;
    private bool tutorialActivo = false;

    // Los textos del tutorial
    private string[] instrucciones = new string[]
    {
        "¡Bienvenido a la empresa! Tu objetivo es armar el organigrama perfecto.",
        "Mecánica base: Arrastrá una línea desde el <b>Analista</b> hasta el <b>CEO</b> para establecer la cadena de mando.",
        "¡Excelente! Pero cuidado: los jefes tienen un <b>límite de reportes</b> (Tramo de control).",
        "Por último, respetá las áreas. Los nodos <b>Verdes (Finanzas)</b> solo pueden reportar a jefes de Finanzas o al CEO.",
        "¡Todo listo! Resolvé este puzzle para empezar a trabajar."
    };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        botonContinuar.onClick.AddListener(AvanzarPasoLectura);
    }

    public void IniciarTutorial()
    {
        tutorialActivo = true;
        pasoActual = 0;
        tutorialPanel.SetActive(true);
        MostrarPaso(pasoActual);
    }

    private void MostrarPaso(int indice)
    {
        if (indice >= instrucciones.Length)
        {
            FinalizarTutorial();
            return;
        }

        tutorialText.text = instrucciones[indice];

        // Lógica de qué pasos requieren botón y cuáles requieren que el jugador juegue
        if (indice == 1)
        {
            // El paso 1 requiere arrastrar la línea, ocultamos el botón de continuar
            botonContinuar.gameObject.SetActive(false);
        }
        else
        {
            // Los demás son informativos, mostramos el botón
            botonContinuar.gameObject.SetActive(true);
        }
    }

    public void AvanzarPasoLectura()
    {
        if (!tutorialActivo) return;
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
            // El jugador cumplió la tarea práctica, avanzamos
            pasoActual++;
            MostrarPaso(pasoActual);
        }
    }

    private void FinalizarTutorial()
    {
        tutorialActivo = false;
        tutorialPanel.SetActive(false);
    }
}
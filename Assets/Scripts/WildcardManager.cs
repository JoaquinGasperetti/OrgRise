using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WildcardManager : MonoBehaviour
{
    public static WildcardManager Instance;

    [Header("Estado")]
    public int wildcardsDisponibles = 1; // Empieza con 1 para probar
    public bool comodinActivo = false;

    [Header("UI")]
    public TextMeshProUGUI textoContador;
    public Button botonComodin;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ActualizarUI();
        botonComodin.onClick.AddListener(ToggleComodin);
    }

    public void ToggleComodin()
    {
        if (wildcardsDisponibles > 0)
        {
            comodinActivo = !comodinActivo;
            Debug.Log(comodinActivo ? "Modo Comodín ACTIVADO" : "Modo Comodín DESACTIVADO");
            // Aquí puedes agregar un cambio de color visual al botón
        }
        else
        {
            Debug.Log("No hay comodines. Mostrar popup de Video Recompensado.");
            // Lógica futura para AdMob Rewarded Video
        }
    }

    public void ConsumirComodin()
    {
        wildcardsDisponibles--;
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
        if (textoContador != null) textoContador.text = $"x{wildcardsDisponibles}";
    }
}
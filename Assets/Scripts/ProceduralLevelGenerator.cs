using UnityEngine;
using TMPro; // Necesario para modificar el texto de la UI

public class ProceduralLevelGenerator : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject nodePrefab;
    public RectTransform spawnArea; // Un panel invisible que define dónde pueden aparecer

    [Header("Configuración de Generación")]
    public int seed = 12345; // Cambiando este número, cambia el puzzle entero
    public int totalNodos = 8;

    // Diccionarios o arreglos de nombres automáticos
    private string[] nombresCEO = { "CEO", "Dir. General", "Presidente" };
    private string[] nombresDirector = { "Dir. Finanzas", "Dir. Marketing", "Dir. Operaciones", "Dir. RRHH" };
    private string[] nombresGerente = { "Gte. Ventas", "Gte. Sistemas", "Gte. Logística", "Gte. Soporte" };
    private string[] nombresEmpleado = { "Analista", "Diseñador", "Desarrollador", "Asistente", "Técnico" };

    void Start()
    {
        // En un juego final, la semilla podría ser la fecha actual:
        // int semillaDiaria = System.DateTime.Now.DayOfYear + System.DateTime.Now.Year;
        GenerarNivel(seed, totalNodos);
    }

    public void GenerarNivel(int semilla, int cantidad)
    {
        // 1. Fijar la semilla matemática
        Random.InitState(semilla);

        // 2. Limpiar la lista del LevelManager por si estamos reiniciando
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.nodosActivos.Clear();
        }

        // 3. Calcular la distribución piramidal de jerarquías
        int numDirectores = Mathf.Clamp(cantidad / 4, 1, 3);
        int numGerentes = Mathf.Clamp(cantidad / 3, 1, 4);
        int numEmpleados = cantidad - 1 - numDirectores - numGerentes; // El -1 es por el CEO

        // 4. Instanciar nodos en orden jerárquico
        // CEO (Raíz)
        CrearNodoUI(NivelJerarquico.CEO, nombresCEO[Random.Range(0, nombresCEO.Length)], true, Random.Range(2, 4));

        // Directores
        for (int i = 0; i < numDirectores; i++)
            CrearNodoUI(NivelJerarquico.Director, nombresDirector[Random.Range(0, nombresDirector.Length)], false, Random.Range(2, 4));

        // Gerentes
        for (int i = 0; i < numGerentes; i++)
            CrearNodoUI(NivelJerarquico.Gerente, nombresGerente[Random.Range(0, nombresGerente.Length)], false, Random.Range(2, 4));

        // Empleados Base (No pueden tener subordinados, su límite es 0)
        for (int i = 0; i < numEmpleados; i++)
            CrearNodoUI(NivelJerarquico.EmpleadoBase, nombresEmpleado[Random.Range(0, nombresEmpleado.Length)], false, 0);
    }

    private void CrearNodoUI(NivelJerarquico nivel, string nombre, bool esRaiz, int maxReportes)
    {
        // Instanciar el prefab en el área designada
        GameObject nuevoNodoObj = Instantiate(nodePrefab, spawnArea);

        // Posicionar aleatoriamente dentro del panel (dejando un margen)
        RectTransform rect = nuevoNodoObj.GetComponent<RectTransform>();
        float randomX = Random.Range(-spawnArea.rect.width / 2.5f, spawnArea.rect.width / 2.5f);
        float randomY = Random.Range(-spawnArea.rect.height / 2.5f, spawnArea.rect.height / 2.5f);
        rect.anchoredPosition = new Vector2(randomX, randomY);

        // Configurar los datos lógicos en el script
        EmployeeNode dataNodo = nuevoNodoObj.GetComponent<EmployeeNode>();
        dataNodo.roleName = nombre;
        dataNodo.nivel = nivel;
        dataNodo.isRootNode = esRaiz;
        dataNodo.maxDirectReports = maxReportes;

        // Actualizar el componente visual (Texto) para que el jugador sepa qué está conectando
        TextMeshProUGUI textoUI = nuevoNodoObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textoUI != null)
        {
            if (maxReportes > 0)
                textoUI.text = $"<b>{nombre}</b>\n<size=70%>Max Reportes: {maxReportes}</size>";
            else
                textoUI.text = $"<b>{nombre}</b>"; // Los empleados base no muestran límite
        }
    }
}
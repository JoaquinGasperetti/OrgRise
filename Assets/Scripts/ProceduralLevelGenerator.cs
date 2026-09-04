using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class ProceduralLevelGenerator : MonoBehaviour
{
    public GameObject nodePrefab;
    public RectTransform spawnArea;

    public int totalNodos = 8;

    private string[] nombresCEO = { "CEO" };
    private Departamento[] areasDisponibles = { Departamento.Finanzas, Departamento.Comercial, Departamento.Operaciones, Departamento.Tecnologia };

    void Start()
    {
        // Generar una semilla aleatoria única por partida
        int randomSeed = Random.Range(10000, 999999);
        GenerarNivel(randomSeed, totalNodos);
    }

    public void GenerarNivel(int semilla, int cantidad)
    {
        Random.InitState(semilla);
        if (LevelManager.Instance != null) LevelManager.Instance.nodosActivos.Clear();

        int numDirectores = Mathf.Clamp(cantidad / 4, 1, 4);
        int numGerentes = Mathf.Clamp(cantidad / 3, 1, 4);
        int numEmpleados = cantidad - 1 - numDirectores - numGerentes;

        // Seleccionar qué departamentos jugarán en esta partida
        List<Departamento> areasActivas = new List<Departamento>();
        for (int i = 0; i < numDirectores; i++) areasActivas.Add(areasDisponibles[i]);

        // Fila 0: CEO
        CrearNodoUI(NivelJerarquico.CEO, Departamento.General, "CEO", true, Random.Range(2, 4), 0, 1);

        // Fila 1: Directores (Uno por cada área activa)
        for (int i = 0; i < numDirectores; i++)
            CrearNodoUI(NivelJerarquico.Director, areasActivas[i], $"Dir. {areasActivas[i]}", false, Random.Range(2, 4), i, numDirectores);

        // Fila 2: Gerentes (Distribuidos equitativamente en las áreas activas)
        for (int i = 0; i < numGerentes; i++)
        {
            Departamento areaAsignada = areasActivas[i % areasActivas.Count];
            CrearNodoUI(NivelJerarquico.Gerente, areaAsignada, $"Gte. {areaAsignada}", false, Random.Range(2, 4), i, numGerentes);
        }

        // Fila 3: Empleados Base
        for (int i = 0; i < numEmpleados; i++)
        {
            Departamento areaAsignada = areasActivas[i % areasActivas.Count];
            CrearNodoUI(NivelJerarquico.EmpleadoBase, areaAsignada, $"Analista {areaAsignada}", false, 0, i, numEmpleados);
        }
    }

    private void CrearNodoUI(NivelJerarquico nivel, Departamento depto, string nombre, bool esRaiz, int maxReportes, int indexEnFila, int totalEnFila)
    {
        GameObject nuevoNodoObj = Instantiate(nodePrefab, spawnArea);
        RectTransform rect = nuevoNodoObj.GetComponent<RectTransform>();

        float rowHeight = spawnArea.rect.height / 4;
        float yPos = ((spawnArea.rect.height / 2f) - (rowHeight / 2f)) - ((int)nivel * rowHeight);
        float colWidth = spawnArea.rect.width / (totalEnFila + 1);
        float xPos = -(spawnArea.rect.width / 2f) + (colWidth * (indexEnFila + 1));

        rect.anchoredPosition = new Vector2(xPos, yPos);

        EmployeeNode dataNodo = nuevoNodoObj.GetComponent<EmployeeNode>();
        dataNodo.roleName = nombre;
        dataNodo.nivel = nivel;
        dataNodo.departamento = depto;
        dataNodo.isRootNode = esRaiz;
        dataNodo.maxDirectReports = maxReportes;

        // Asignar color según el departamento
        Image fondo = nuevoNodoObj.GetComponent<Image>();
        fondo.color = ObtenerColorArea(depto);

        TextMeshProUGUI textoUI = nuevoNodoObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textoUI != null)
        {
            string textoLimite = maxReportes > 0 ? $"\n<size=70%>Max: {maxReportes}</size>" : "";
            textoUI.text = $"<color=black><b>{nombre}</b>{textoLimite}</color>";
        }
    }

    private Color ObtenerColorArea(Departamento depto)
    {
        switch (depto)
        {
            case Departamento.General: return Color.white;
            case Departamento.Finanzas: return new Color(0.6f, 0.9f, 0.6f); // Verde pastel
            case Departamento.Comercial: return new Color(0.6f, 0.8f, 1f);   // Azul pastel
            case Departamento.Operaciones: return new Color(1f, 0.8f, 0.6f); // Naranja pastel
            case Departamento.Tecnologia: return new Color(0.9f, 0.7f, 1f);  // Violeta pastel
            default: return Color.white;
        }
    }
}
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class ProceduralLevelGenerator : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject nodePrefab;
    public RectTransform spawnArea;

    [Header("Configuración")]
    public int totalNodos = 12; // Aumentado para ver la variedad de puestos

    // Ampliamos los prefijos para asegurar un pool grande de combinaciones únicas
    private string[] nombresCEO = { "CEO", "Presidente", "Director General" };
    private Departamento[] areasDisponibles = { Departamento.Finanzas, Departamento.Comercial, Departamento.Operaciones, Departamento.Tecnologia };

    private string[] prefijosDirector = { "Dir.", "VP", "Head of" };
    private string[] prefijosGerente = { "Gte.", "Coord.", "Jefe", "Líder", "Subgte." };
    private string[] prefijosEmpleado = { "Analista", "Especialista", "Asistente", "Técnico", "Consultor", "Auditor", "Operador", "Diseñador", "Desarrollador", "Ejecutivo", "Supervisor" };

    void Start()
    {
        int randomSeed = Random.Range(10000, 999999);
        GenerarNivel(randomSeed, totalNodos);
    }

    public void GenerarNivel(int semilla, int cantidad)
    {
        Random.InitState(semilla);
        if (LevelManager.Instance != null) LevelManager.Instance.nodosActivos.Clear();

        int numDirectores = Mathf.Clamp(cantidad / 4, 1, 4);
        int numGerentes = Mathf.Clamp(cantidad / 3, 1, 6);
        int numEmpleados = cantidad - 1 - numDirectores - numGerentes;

        List<Departamento> areasActivas = new List<Departamento>();
        for (int i = 0; i < numDirectores; i++) areasActivas.Add(areasDisponibles[i]);

        // Registro para evitar duplicados exactos en la misma partida
        HashSet<string> nombresUsados = new HashSet<string>();

        // Fila 0: CEO
        string nombreCEO = ObtenerNombreUnico(new List<string>(nombresCEO), "", nombresUsados);
        CrearNodoUI(NivelJerarquico.CEO, Departamento.General, nombreCEO, true, Random.Range(2, 4), 0, 1);

        // Fila 1: Directores
        for (int i = 0; i < numDirectores; i++)
        {
            string nombreDir = ObtenerNombreUnico(new List<string>(prefijosDirector), areasActivas[i].ToString(), nombresUsados);
            CrearNodoUI(NivelJerarquico.Director, areasActivas[i], nombreDir, false, Random.Range(2, 4), i, numDirectores);
        }

        // Fila 2: Gerentes
        for (int i = 0; i < numGerentes; i++)
        {
            Departamento area = areasActivas[i % areasActivas.Count];
            string nombreGte = ObtenerNombreUnico(new List<string>(prefijosGerente), area.ToString(), nombresUsados);
            CrearNodoUI(NivelJerarquico.Gerente, area, nombreGte, false, Random.Range(2, 4), i, numGerentes);
        }

        // Fila 3: Empleados Base
        for (int i = 0; i < numEmpleados; i++)
        {
            Departamento area = areasActivas[i % areasActivas.Count];
            string nombreEmp = ObtenerNombreUnico(new List<string>(prefijosEmpleado), area.ToString(), nombresUsados);
            CrearNodoUI(NivelJerarquico.EmpleadoBase, area, nombreEmp, false, 0, i, numEmpleados);
        }
    }

    private string ObtenerNombreUnico(List<string> poolPrefijos, string sufijoArea, HashSet<string> usados)
    {
        // Mezclamos la lista de prefijos disponibles
        for (int i = 0; i < poolPrefijos.Count; i++)
        {
            string temp = poolPrefijos[i];
            int randomIndex = Random.Range(i, poolPrefijos.Count);
            poolPrefijos[i] = poolPrefijos[randomIndex];
            poolPrefijos[randomIndex] = temp;
        }

        // Buscamos una combinación que no esté en el HashSet
        foreach (string prefijo in poolPrefijos)
        {
            // Usamos \n para que el departamento quede en la línea de abajo
            string candidato = string.IsNullOrEmpty(sufijoArea) ? prefijo : $"{prefijo}\n{sufijoArea}";

            if (!usados.Contains(candidato))
            {
                usados.Add(candidato);
                return candidato;
            }
        }

        // Sistema de seguridad: si se agotan todos los prefijos posibles, añade un número (ej. "Analista Finanzas 2")
        string fallback = string.IsNullOrEmpty(sufijoArea) ? poolPrefijos[0] : $"{poolPrefijos[0]}\n{sufijoArea}";
        int contador = 2;
        while (usados.Contains($"{fallback} {contador}")) contador++;

        usados.Add($"{fallback} {contador}");
        return $"{fallback} {contador}";
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
        dataNodo.roleName = nombre.Replace("\n", " "); // Guardamos el nombre plano en la lógica
        dataNodo.nivel = nivel;
        dataNodo.departamento = depto;
        dataNodo.isRootNode = esRaiz;
        dataNodo.maxDirectReports = maxReportes;

        Image fondo = nuevoNodoObj.GetComponent<Image>();
        fondo.color = ObtenerColorArea(depto);

        TextMeshProUGUI textoUI = nuevoNodoObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textoUI != null)
        {
            // El texto visual mantiene el salto de línea para legibilidad
            string textoLimite = maxReportes > 0 ? $"\n<size=60%>Max: {maxReportes}</size>" : "";
            textoUI.text = $"<color=black><b>{nombre}</b>{textoLimite}</color>";
        }
    }

    private Color ObtenerColorArea(Departamento depto)
    {
        switch (depto)
        {
            case Departamento.General: return Color.white;
            case Departamento.Finanzas: return new Color(0.6f, 0.9f, 0.6f);
            case Departamento.Comercial: return new Color(0.6f, 0.8f, 1f);
            case Departamento.Operaciones: return new Color(1f, 0.8f, 0.6f);
            case Departamento.Tecnologia: return new Color(0.9f, 0.7f, 1f);
            default: return Color.white;
        }
    }
}
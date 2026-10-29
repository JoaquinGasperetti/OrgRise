using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class ProceduralLevelGenerator : MonoBehaviour
{
    public static ProceduralLevelGenerator Instance;

    [Header("Referencias UI")]
    public GameObject nodePrefab;
    public RectTransform spawnArea;

    [Header("Configuración")]
    public int totalNodos = 10;

    private string[] nombresCEO = { "CEO", "Dir. General", "Presidencia" };
    private Departamento[] areasDisponibles = { Departamento.Finanzas, Departamento.Comercial, Departamento.Operaciones, Departamento.Tecnologia };

    private string[] prefijosDirector = { "Dir.", "VP", "Head of" };
    private string[] prefijosGerente = { "Gte.", "Coord.", "Jefe", "Líder" };
    private string[] prefijosEmpleado = { "Analista", "Especialista", "Asistente", "Técnico", "Consultor", "Auditor", "Operador", "Diseñador", "Desarrollador", "Ejecutivo" };

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.IniciarJuego();
        }
        else
        {
            int seedDiaria = ObtenerSemillaDiaria();
            GenerarNivel(seedDiaria, totalNodos);
        }
    }

    public static int ObtenerSemillaDiaria()
    {
        System.DateTime hoy = System.DateTime.Today;
        return hoy.Year * 10000 + hoy.Month * 100 + hoy.Day;
    }

    public void LimpiarTablero()
    {
        if (spawnArea != null)
        {
            for (int i = spawnArea.childCount - 1; i >= 0; i--)
            {
                Destroy(spawnArea.GetChild(i).gameObject);
            }
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LimpiarNodosRegistrados();
        }
    }

    public void GenerarNivel(int semilla, int cantidad)
    {
        GenerarNivelAcademico(
            semilla,
            cantidad,
            CriterioDepartamentalizacion.Funcional,
            ModoAutoridad.Equilibrada
        );
    }

    public void GenerarNivelAcademico(
        int semilla,
        int cantidad,
        CriterioDepartamentalizacion criterio,
        ModoAutoridad autoridad)
    {
        LimpiarTablero();

        // Forzamos actualización de layout del Canvas antes de calcular posiciones en móvil vertical
        Canvas.ForceUpdateCanvases();

        Random.InitState(semilla);
        cantidad = Mathf.Clamp(cantidad, 4, 12);

        if (autoridad == ModoAutoridad.Centralizada)
        {
            GenerarEstructuraCentralizada(cantidad, criterio);
            return;
        }

        int numDirectores = Mathf.Clamp(cantidad / 4, 2, 3);
        int restantes = cantidad - 1 - numDirectores;

        int numGerentes = autoridad == ModoAutoridad.Descentralizada
            ? Mathf.Clamp(Mathf.CeilToInt(restantes * 0.45f), numDirectores, 4)
            : Mathf.Clamp(restantes / 2, numDirectores, 4);

        int numEmpleados = Mathf.Clamp(cantidad - 1 - numDirectores - numGerentes, numDirectores, 5);

        List<Departamento> areasActivas = new List<Departamento>();
        for (int i = 0; i < numDirectores; i++)
        {
            areasActivas.Add(areasDisponibles[i % areasDisponibles.Length]);
        }

        List<Departamento> deptosGerentes = new List<Departamento>();
        for (int i = 0; i < numGerentes; i++)
        {
            deptosGerentes.Add(areasActivas[i % areasActivas.Count]);
        }

        List<Departamento> deptosEmpleados = new List<Departamento>();
        for (int i = 0; i < numEmpleados; i++)
        {
            deptosEmpleados.Add(areasActivas[i % areasActivas.Count]);
        }

        Dictionary<Departamento, int> gerentesPorArea = new Dictionary<Departamento, int>();
        Dictionary<Departamento, int> empleadosPorArea = new Dictionary<Departamento, int>();
        foreach (var area in areasActivas)
        {
            gerentesPorArea[area] = 0;
            empleadosPorArea[area] = 0;
        }
        foreach (var d in deptosGerentes) gerentesPorArea[d]++;
        foreach (var d in deptosEmpleados) empleadosPorArea[d]++;

        HashSet<string> nombresUsados = new HashSet<string>();

        // FILA 0: CEO
        int extraCEO = autoridad == ModoAutoridad.Descentralizada ? 0 : 1;
        int cupoCEO = numDirectores + extraCEO;
        string nombreCEO = ObtenerNombreUnico(new List<string>(nombresCEO), "", nombresUsados);
        CrearNodoUI(NivelJerarquico.CEO, Departamento.General, nombreCEO, true, cupoCEO, 0, 1, 4);

        // FILA 1: Directores
        for (int i = 0; i < numDirectores; i++)
        {
            Departamento area = areasActivas[i];
            string etiquetaArea = ObtenerEtiquetaCriterio(area, criterio);
            string nombreDir = ObtenerNombreUnico(new List<string>(prefijosDirector), etiquetaArea, nombresUsados);

            int minNecesarioDir = Mathf.Max(1, gerentesPorArea[area]);
            int holguraDir = autoridad == ModoAutoridad.Descentralizada ? 0 : 1;
            int cupoDirector = minNecesarioDir + holguraDir;

            CrearNodoUI(NivelJerarquico.Director, area, nombreDir, false, cupoDirector, i, numDirectores, 4);
        }

        // FILA 2: Gerentes
        for (int i = 0; i < numGerentes; i++)
        {
            Departamento area = deptosGerentes[i];
            int totalGerentesArea = gerentesPorArea[area];
            int totalEmpleadosArea = empleadosPorArea[area];

            int cupoMinimoGerente = Mathf.Max(1, Mathf.CeilToInt((float)totalEmpleadosArea / totalGerentesArea));
            int holguraGte = (autoridad == ModoAutoridad.Descentralizada) ? 0 : (i % 2 == 0 ? 1 : 0);
            int cupoGerente = cupoMinimoGerente + holguraGte;

            string etiquetaArea = ObtenerEtiquetaCriterio(area, criterio);
            string nombreGte = ObtenerNombreUnico(new List<string>(prefijosGerente), etiquetaArea, nombresUsados);

            CrearNodoUI(NivelJerarquico.Gerente, area, nombreGte, false, cupoGerente, i, numGerentes, 4);
        }

        // FILA 3: Empleados Base
        for (int i = 0; i < numEmpleados; i++)
        {
            Departamento area = deptosEmpleados[i];
            string etiquetaArea = ObtenerEtiquetaCriterio(area, criterio);
            string nombreEmp = ObtenerNombreUnico(new List<string>(prefijosEmpleado), etiquetaArea, nombresUsados);
            CrearNodoUI(NivelJerarquico.EmpleadoBase, area, nombreEmp, false, 0, i, numEmpleados, 4);
        }
    }

    private void GenerarEstructuraCentralizada(int cantidad, CriterioDepartamentalizacion criterio)
    {
        int numMandos = Mathf.Clamp(cantidad / 3, 2, 3);
        int numEmpleados = Mathf.Clamp(cantidad - 1 - numMandos, 2, 5);

        List<Departamento> areasActivas = new List<Departamento>();
        for (int i = 0; i < numMandos; i++)
        {
            areasActivas.Add(areasDisponibles[i % areasDisponibles.Length]);
        }

        HashSet<string> nombresUsados = new HashSet<string>();

        string nombreCEO = ObtenerNombreUnico(new List<string>(nombresCEO), "Central", nombresUsados);
        CrearNodoUI(NivelJerarquico.CEO, Departamento.General, nombreCEO, true, numMandos + 1, 0, 1, 3);

        for (int i = 0; i < numMandos; i++)
        {
            Departamento area = areasActivas[i];
            string etiquetaArea = ObtenerEtiquetaCriterio(area, criterio);
            string nombreMando = ObtenerNombreUnico(new List<string>(prefijosDirector), etiquetaArea, nombresUsados);
            int empleadosDeEstaArea = Mathf.CeilToInt((float)numEmpleados / numMandos) + 1;
            CrearNodoUI(NivelJerarquico.Director, area, nombreMando, false, empleadosDeEstaArea, i, numMandos, 3);
        }

        for (int i = 0; i < numEmpleados; i++)
        {
            Departamento area = areasActivas[i % areasActivas.Count];
            string etiquetaArea = ObtenerEtiquetaCriterio(area, criterio);
            string nombreEmp = ObtenerNombreUnico(new List<string>(prefijosEmpleado), etiquetaArea, nombresUsados);
            CrearNodoUI(NivelJerarquico.EmpleadoBase, area, nombreEmp, false, 0, i, numEmpleados, 3, filaVisualOverride: 2);
        }
    }

    public string ObtenerEtiquetaCriterio(Departamento depto, CriterioDepartamentalizacion criterio)
    {
        if (depto == Departamento.General) return "General";

        switch (criterio)
        {
            case CriterioDepartamentalizacion.Funcional:
                switch (depto)
                {
                    case Departamento.Finanzas: return "Finanzas";
                    case Departamento.Comercial: return "Comercial";
                    case Departamento.Operaciones: return "Operaciones";
                    case Departamento.Tecnologia: return "Tecnología";
                }
                break;

            case CriterioDepartamentalizacion.PorProducto:
                switch (depto)
                {
                    case Departamento.Finanzas: return "Div. PyMEs";
                    case Departamento.Comercial: return "Div. Consumo";
                    case Departamento.Operaciones: return "Div. Industria";
                    case Departamento.Tecnologia: return "Div. Cloud";
                }
                break;

            case CriterioDepartamentalizacion.Geografica:
                switch (depto)
                {
                    case Departamento.Finanzas: return "Reg. Norte";
                    case Departamento.Comercial: return "Reg. Centro";
                    case Departamento.Operaciones: return "Reg. Sur";
                    case Departamento.Tecnologia: return "Reg. Exterior";
                }
                break;

            case CriterioDepartamentalizacion.Matricial:
                switch (depto)
                {
                    case Departamento.Finanzas: return "Proy. Alfa";
                    case Departamento.Comercial: return "Proy. Beta";
                    case Departamento.Operaciones: return "Proy. Gamma";
                    case Departamento.Tecnologia: return "Proy. Delta";
                }
                break;
        }
        return depto.ToString();
    }

    private string ObtenerNombreUnico(List<string> poolPrefijos, string sufijoArea, HashSet<string> usados)
    {
        for (int i = 0; i < poolPrefijos.Count; i++)
        {
            string temp = poolPrefijos[i];
            int randomIndex = Random.Range(i, poolPrefijos.Count);
            poolPrefijos[i] = poolPrefijos[randomIndex];
            poolPrefijos[randomIndex] = temp;
        }

        foreach (string prefijo in poolPrefijos)
        {
            string candidato = string.IsNullOrEmpty(sufijoArea) ? prefijo : $"{prefijo}\n{sufijoArea}";
            if (!usados.Contains(candidato))
            {
                usados.Add(candidato);
                return candidato;
            }
        }

        string fallback = string.IsNullOrEmpty(sufijoArea) ? poolPrefijos[0] : $"{poolPrefijos[0]}\n{sufijoArea}";
        int contador = 2;
        while (usados.Contains($"{fallback} {contador}")) contador++;

        usados.Add($"{fallback} {contador}");
        return $"{fallback} {contador}";
    }

    public EmployeeNode CrearNodoUI(
        NivelJerarquico nivel,
        Departamento depto,
        string nombre,
        bool esRaiz,
        int maxReportes,
        int indexEnFila,
        int totalEnFila,
        int totalFilas = 4,
        int filaVisualOverride = -1)
    {
        GameObject nuevoNodoObj = Instantiate(nodePrefab, spawnArea);
        RectTransform rect = nuevoNodoObj.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        float areaWidth = spawnArea.rect.width > 100f ? spawnArea.rect.width : 940f;
        float areaHeight = spawnArea.rect.height > 100f ? spawnArea.rect.height : 1200f;

        // Calculamos el ancho y alto seguros de cada tarjeta según cuántos nodos comparten la fila en vertical
        float espacioPorColumna = areaWidth / Mathf.Max(1, totalEnFila);
        float anchoTarjeta = Mathf.Clamp(espacioPorColumna * 0.88f, 130f, 220f);
        float altoTarjeta = Mathf.Clamp((areaHeight / Mathf.Max(2, totalFilas)) * 0.44f, 88f, 115f);
        rect.sizeDelta = new Vector2(anchoTarjeta, altoTarjeta);

        // Distribución horizontal equitativa dentro de los márgenes de SpawnArea (nunca se desborda a los lados)
        float xPos = 0f;
        if (totalEnFila == 1)
        {
            xPos = 0f;
        }
        else
        {
            float margenLateral = (anchoTarjeta * 0.5f) + 12f;
            float minX = -(areaWidth * 0.5f) + margenLateral;
            float maxX = (areaWidth * 0.5f) - margenLateral;
            float t = (float)indexEnFila / (totalEnFila - 1);
            xPos = Mathf.Lerp(minX, maxX, t);
        }

        // Distribución vertical equitativa dentro de los márgenes superior e inferior de SpawnArea
        int filaIndice = filaVisualOverride >= 0 ? filaVisualOverride : (int)nivel;
        float margenVertical = (altoTarjeta * 0.5f) + 18f;
        float topY = (areaHeight * 0.5f) - margenVertical;
        float bottomY = -(areaHeight * 0.5f) + margenVertical;
        float ty = totalFilas > 1 ? (float)filaIndice / (totalFilas - 1) : 0.5f;
        float yPos = Mathf.Lerp(topY, bottomY, ty);

        rect.anchoredPosition = new Vector2(xPos, yPos);

        TextMeshProUGUI textoUI = nuevoNodoObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textoUI != null)
        {
            RectTransform txtRect = textoUI.GetComponent<RectTransform>();
            txtRect.localScale = Vector3.one;
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(6f, 4f);
            txtRect.offsetMax = new Vector2(-6f, -4f);
            textoUI.margin = Vector4.zero;
            textoUI.enableAutoSizing = true;
            textoUI.fontSizeMin = 12f;
            textoUI.fontSizeMax = 24f;
        }

        EmployeeNode dataNodo = nuevoNodoObj.GetComponent<EmployeeNode>();
        dataNodo.roleName = nombre.Replace("\n", " ");
        dataNodo.nivel = nivel;
        dataNodo.departamento = depto;
        dataNodo.isRootNode = esRaiz;
        dataNodo.maxDirectReports = maxReportes;

        Color colorTarjeta = ObtenerColorArea(depto);
        dataNodo.ConfigureVisuals(nombre, colorTarjeta);

        return dataNodo;
    }

    public Color ObtenerColorArea(Departamento depto)
    {
        switch (depto)
        {
            case Departamento.General: return new Color(0.95f, 0.96f, 0.98f);
            case Departamento.Finanzas: return new Color(0.65f, 0.92f, 0.68f);
            case Departamento.Comercial: return new Color(0.64f, 0.82f, 1.0f);
            case Departamento.Operaciones: return new Color(1.0f, 0.82f, 0.60f);
            case Departamento.Tecnologia: return new Color(0.88f, 0.72f, 1.0f);
            default: return Color.white;
        }
    }
}
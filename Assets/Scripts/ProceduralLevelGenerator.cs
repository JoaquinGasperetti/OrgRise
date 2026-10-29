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

    private string[] nombresCEO = { "CEO", "Dirección General", "Presidencia" };
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
        // Si LevelManager está presente, coordina la carga de la campaña académica y puzzle diario
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

    /// <summary>
    /// Método original mejorado: garantiza que la suma de capacidades (Tramo de Control) de cada departamento
    /// y nivel jerárquico sea siempre suficiente para resolver el organigrama al 100%.
    /// </summary>
    public void GenerarNivel(int semilla, int cantidad)
    {
        GenerarNivelAcademico(
            semilla,
            cantidad,
            CriterioDepartamentalizacion.Funcional,
            ModoAutoridad.Equilibrada
        );
    }

    /// <summary>
    /// Genera un organigrama resoluble adaptando las reglas según el Criterio de Departamentalización
    /// y el grado de Centralización vs. Descentralización.
    /// </summary>
    public void GenerarNivelAcademico(
        int semilla,
        int cantidad,
        CriterioDepartamentalizacion criterio,
        ModoAutoridad autoridad)
    {
        LimpiarTablero();
        Random.InitState(semilla);

        cantidad = Mathf.Clamp(cantidad, 4, 14);

        // Si la estructura es Centralizada (ej. Startup temprana o estructura chata),
        // la cúpula concentra el control directo con menos niveles intermedios.
        if (autoridad == ModoAutoridad.Centralizada)
        {
            GenerarEstructuraCentralizada(cantidad, criterio);
            return;
        }

        // Determinamos cantidad de Directores, Gerentes y Empleados Base asegurando simetría por departamento
        int numDirectores = Mathf.Clamp(cantidad / 4, 2, 3);
        int restantes = cantidad - 1 - numDirectores;

        // En estructura Descentralizada hay más mandos medios delegando autoridad y tramos de control estrechos
        int numGerentes = autoridad == ModoAutoridad.Descentralizada
            ? Mathf.Clamp(Mathf.CeilToInt(restantes * 0.45f), numDirectores, 5)
            : Mathf.Clamp(restantes / 2, numDirectores, 4);

        int numEmpleados = Mathf.Max(numDirectores, cantidad - 1 - numDirectores - numGerentes);

        List<Departamento> areasActivas = new List<Departamento>();
        for (int i = 0; i < numDirectores; i++)
        {
            areasActivas.Add(areasDisponibles[i % areasDisponibles.Length]);
        }

        // Asignamos departamentos a Gerentes (al menos 1 por cada Director activo)
        List<Departamento> deptosGerentes = new List<Departamento>();
        for (int i = 0; i < numGerentes; i++)
        {
            deptosGerentes.Add(areasActivas[i % areasActivas.Count]);
        }

        // Asignamos departamentos a Empleados Base (distribuidos entre las áreas activas)
        List<Departamento> deptosEmpleados = new List<Departamento>();
        for (int i = 0; i < numEmpleados; i++)
        {
            deptosEmpleados.Add(areasActivas[i % areasActivas.Count]);
        }

        // Contamos cuántos Gerentes y Empleados hay en cada Departamento para garantizar solubilidad matemática
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

        // --- FILA 0: CEO (Nivel Estratégico) ---
        // El CEO necesita al menos tantos reportes directos como Directores existen
        int extraCEO = autoridad == ModoAutoridad.Descentralizada ? 0 : 1;
        int cupoCEO = numDirectores + extraCEO;
        string nombreCEO = ObtenerNombreUnico(new List<string>(nombresCEO), "", nombresUsados);
        CrearNodoUI(NivelJerarquico.CEO, Departamento.General, nombreCEO, true, cupoCEO, 0, 1, 4);

        // --- FILA 1: Directores (Nivel Ejecutivo) ---
        for (int i = 0; i < numDirectores; i++)
        {
            Departamento area = areasActivas[i];
            string etiquetaArea = ObtenerEtiquetaCriterio(area, criterio);
            string nombreDir = ObtenerNombreUnico(new List<string>(prefijosDirector), etiquetaArea, nombresUsados);

            // Cada Director debe poder recibir a todos los Gerentes de su área
            int minNecesarioDir = Mathf.Max(1, gerentesPorArea[area]);
            int holguraDir = autoridad == ModoAutoridad.Descentralizada ? 0 : 1;
            int cupoDirector = minNecesarioDir + holguraDir;

            CrearNodoUI(NivelJerarquico.Director, area, nombreDir, false, cupoDirector, i, numDirectores, 4);
        }

        // --- FILA 2: Gerentes (Nivel Táctico) ---
        // Calculamos cuántos empleados debe absorber cada gerente de su área
        Dictionary<Departamento, int> gerentesCreadosPorArea = new Dictionary<Departamento, int>();
        foreach (var area in areasActivas) gerentesCreadosPorArea[area] = 0;

        for (int i = 0; i < numGerentes; i++)
        {
            Departamento area = deptosGerentes[i];
            int totalGerentesArea = gerentesPorArea[area];
            int totalEmpleadosArea = empleadosPorArea[area];

            // Reparto garantizado: techo(empleadosDelArea / gerentesDelArea)
            int cupoMinimoGerente = Mathf.Max(1, Mathf.CeilToInt((float)totalEmpleadosArea / totalGerentesArea));
            int holguraGte = (autoridad == ModoAutoridad.Descentralizada) ? 0 : (i % 2 == 0 ? 1 : 0);
            int cupoGerente = cupoMinimoGerente + holguraGte;

            string etiquetaArea = ObtenerEtiquetaCriterio(area, criterio);
            string nombreGte = ObtenerNombreUnico(new List<string>(prefijosGerente), etiquetaArea, nombresUsados);

            CrearNodoUI(NivelJerarquico.Gerente, area, nombreGte, false, cupoGerente, i, numGerentes, 4);
            gerentesCreadosPorArea[area]++;
        }

        // --- FILA 3: Empleados Base (Nivel Operativo) ---
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
        // En una estructura centralizada, la autoridad se concentra arriba:
        // 1 CEO con tramo de control amplio + Gerentes/Líderes que reportan directo al CEO + Operativos
        int numMandos = Mathf.Clamp(cantidad / 3, 2, 3);
        int numEmpleados = Mathf.Max(2, cantidad - 1 - numMandos);

        List<Departamento> areasActivas = new List<Departamento>();
        for (int i = 0; i < numMandos; i++)
        {
            areasActivas.Add(areasDisponibles[i % areasDisponibles.Length]);
        }

        HashSet<string> nombresUsados = new HashSet<string>();

        // CEO con tramo de control amplio (puede recibir a todos los mandos e incluso supervisión directa)
        string nombreCEO = ObtenerNombreUnico(new List<string>(nombresCEO), "Central", nombresUsados);
        CrearNodoUI(NivelJerarquico.CEO, Departamento.General, nombreCEO, true, numMandos + 1, 0, 1, 3);

        // Fila intermedia: Directores de área con tramo amplio
        for (int i = 0; i < numMandos; i++)
        {
            Departamento area = areasActivas[i];
            string etiquetaArea = ObtenerEtiquetaCriterio(area, criterio);
            string nombreMando = ObtenerNombreUnico(new List<string>(prefijosDirector), etiquetaArea, nombresUsados);
            int empleadosDeEstaArea = Mathf.CeilToInt((float)numEmpleados / numMandos) + 1;
            CrearNodoUI(NivelJerarquico.Director, area, nombreMando, false, empleadosDeEstaArea, i, numMandos, 3);
        }

        // Fila operativa (en 3 filas visuales para que se note la estructura chata / horizontal)
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
                    case Departamento.Finanzas: return "Región Norte";
                    case Departamento.Comercial: return "Región Centro";
                    case Departamento.Operaciones: return "Región Sur";
                    case Departamento.Tecnologia: return "Región Exterior";
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

        float areaWidth = spawnArea.rect.width > 100f ? spawnArea.rect.width : 980f;
        float areaHeight = spawnArea.rect.height > 100f ? spawnArea.rect.height : 1250f;

        int filaIndice = filaVisualOverride >= 0 ? filaVisualOverride : (int)nivel;
        float rowHeight = areaHeight / Mathf.Max(2, totalFilas);
        float yPos = ((areaHeight / 2f) - (rowHeight / 2f)) - (filaIndice * rowHeight);

        float colWidth = areaWidth / (totalEnFila + 1);
        float xPos = -(areaWidth / 2f) + (colWidth * (indexEnFila + 1));

        rect.anchoredPosition = new Vector2(xPos, yPos);

        // Ajustamos la escala horizontal automáticamente si hay 4 o más nodos en la misma fila para que no se solapen
        float anchoDisponiblePorNodo = (areaWidth / Mathf.Max(1, totalEnFila)) * 0.88f;
        float anchoTarjeta = Mathf.Clamp(anchoDisponiblePorNodo, 165f, 230f);
        rect.sizeDelta = new Vector2(anchoTarjeta, 108f);

        TextMeshProUGUI textoUI = nuevoNodoObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textoUI != null)
        {
            RectTransform txtRect = textoUI.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(6f, 4f);
            txtRect.offsetMax = new Vector2(-6f, -4f);
            textoUI.enableAutoSizing = true;
            textoUI.fontSizeMin = 15f;
            textoUI.fontSizeMax = 25f;
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
            case Departamento.General: return new Color(0.95f, 0.96f, 0.98f);     // Blanco Perla (Cúpula)
            case Departamento.Finanzas: return new Color(0.65f, 0.92f, 0.68f);    // Verde Finanzas / Div. 1
            case Departamento.Comercial: return new Color(0.64f, 0.82f, 1.0f);    // Azul Comercial / Div. 2
            case Departamento.Operaciones: return new Color(1.0f, 0.82f, 0.60f);  // Naranja Operaciones / Div. 3
            case Departamento.Tecnologia: return new Color(0.88f, 0.72f, 1.0f);   // Violeta Tecnología / Div. 4
            default: return Color.white;
        }
    }
}
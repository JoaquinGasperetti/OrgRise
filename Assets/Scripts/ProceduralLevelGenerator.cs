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

    private string[] nombresCEO = { "CEO Global", "Presidencia", "Managing Partner", "Founder & CEO" };
    private Departamento[] areasDisponibles = { Departamento.Finanzas, Departamento.Comercial, Departamento.Operaciones, Departamento.Tecnologia };

    private string[] prefijosDirector = { "VP", "Director", "Head of", "Chief" };
    private string[] prefijosGerente = { "Líder", "Manager", "Coord.", "Jefe" };
    private string[] prefijosEmpleado = { "Analista", "Specialist", "Ejecutivo", "Ingeniero", "Consultor", "Auditor", "Productor", "Diseñador", "Developer", "Operador" };
    private string[] nombresStaff = { "Legal Counsel", "Auditoría VIP", "M&A Advisor", "Compliance" };

    private GameObject boardBackgroundObj;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        AsegurarFondoVectorialTablero();

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

    private void AsegurarFondoVectorialTablero()
    {
        if (spawnArea == null || boardBackgroundObj != null) return;

        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null) return;

        boardBackgroundObj = new GameObject("BoardBlueprintBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        boardBackgroundObj.transform.SetParent(mainCanvas.transform, false);
        boardBackgroundObj.transform.SetAsFirstSibling();

        RectTransform rt = boardBackgroundObj.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.anchorMin = new Vector2(0.025f, 0.20f);
        rt.anchorMax = new Vector2(0.975f, 0.822f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bgImg = boardBackgroundObj.GetComponent<Image>();
        VectorUIKit.ApplyRoundedStyle(bgImg, new Color(0.075f, 0.115f, 0.195f, 0.92f), pill: false, addShadow: true);
        VectorUIKit.ApplyOutline(bgImg, new Color(0.28f, 0.45f, 0.72f, 0.28f), 1.5f);
        bgImg.raycastTarget = false;

        GameObject gridObj = new GameObject("BlueprintGridOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gridObj.transform.SetParent(boardBackgroundObj.transform, false);
        RectTransform gridRt = gridObj.GetComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0.02f, 0.02f);
        gridRt.anchorMax = new Vector2(0.98f, 0.98f);
        gridRt.offsetMin = Vector2.zero;
        gridRt.offsetMax = Vector2.zero;

        Image gridImg = gridObj.GetComponent<Image>();
        gridImg.sprite = VectorUIKit.GetBlueprintGridSprite();
        gridImg.type = Image.Type.Tiled;
        gridImg.color = Color.white;
        gridImg.raycastTarget = false;
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
        GenerarNivelComercial(
            semilla,
            cantidad,
            CriterioDepartamentalizacion.Funcional,
            ModoAutoridad.Equilibrada,
            false,
            false
        );
    }

    public void GenerarNivelAcademico(
        int semilla,
        int cantidad,
        CriterioDepartamentalizacion criterio,
        ModoAutoridad autoridad)
    {
        GenerarNivelComercial(semilla, cantidad, criterio, autoridad, false, false);
    }

    /// <summary>
    /// Genera cualquier nivel de la campaña comercial de 15 niveles o del desafío diario infinito,
    /// incluyendo soporte completo para Órganos de Staff VIP, Doble Mando Matricial y Nodos en Crisis.
    /// </summary>
    public void GenerarNivelComercial(
        int semilla,
        int cantidad,
        CriterioDepartamentalizacion criterio,
        ModoAutoridad autoridad,
        bool incluirStaff,
        bool preconectarCrisis)
    {
        AsegurarFondoVectorialTablero();
        LimpiarTablero();
        Canvas.ForceUpdateCanvases();

        Random.InitState(semilla);
        cantidad = Mathf.Clamp(cantidad, 5, 12);

        if (autoridad == ModoAutoridad.Centralizada && !incluirStaff && criterio != CriterioDepartamentalizacion.Matricial)
        {
            GenerarEstructuraCentralizada(cantidad, criterio, preconectarCrisis);
            return;
        }

        int numDirectores = Mathf.Clamp(cantidad / 4, 2, 3);
        int reservadosStaff = incluirStaff ? 1 : 0;
        int restantes = cantidad - 1 - numDirectores - reservadosStaff;

        int numGerentes = autoridad == ModoAutoridad.Descentralizada
            ? Mathf.Clamp(Mathf.CeilToInt(restantes * 0.45f), numDirectores, 4)
            : Mathf.Clamp(restantes / 2, numDirectores, 4);

        int numEmpleados = Mathf.Clamp(restantes - numGerentes, numDirectores, 5);

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

        // FILA 0: CEO (y Staff VIP si aplica)
        int extraCEO = autoridad == ModoAutoridad.Descentralizada ? 0 : 1;
        int cupoCEO = numDirectores + extraCEO + reservadosStaff;
        string nombreCEO = ObtenerNombreUnico(new List<string>(nombresCEO), "", nombresUsados);

        int totalFila0 = incluirStaff ? 2 : 1;
        EmployeeNode ceoNode = CrearNodoUI(NivelJerarquico.CEO, Departamento.General, nombreCEO, true, cupoCEO, 0, totalFila0, 4, filaVisualOverride: 0);

        if (incluirStaff)
        {
            string nombreStaff = ObtenerNombreUnico(new List<string>(nombresStaff), "Staff VIP", nombresUsados);
            EmployeeNode staffNode = CrearNodoUI(NivelJerarquico.Director, Departamento.StaffAsesoria, nombreStaff, false, 0, 1, totalFila0, 4, filaVisualOverride: 0);
            staffNode.isStaffNode = true;
            staffNode.ConfigureVisuals(nombreStaff, ObtenerColorArea(Departamento.StaffAsesoria));
        }

        // FILA 1: Directores
        List<EmployeeNode> listaDirectores = new List<EmployeeNode>();
        for (int i = 0; i < numDirectores; i++)
        {
            Departamento area = areasActivas[i];
            string etiquetaArea = ObtenerEtiquetaCriterio(area, criterio);
            string nombreDir = ObtenerNombreUnico(new List<string>(prefijosDirector), etiquetaArea, nombresUsados);

            int minNecesarioDir = Mathf.Max(1, gerentesPorArea[area]);
            int holguraDir = autoridad == ModoAutoridad.Descentralizada ? 0 : 1;
            int cupoDirector = minNecesarioDir + holguraDir;

            EmployeeNode dirNode = CrearNodoUI(NivelJerarquico.Director, area, nombreDir, false, cupoDirector, i, numDirectores, 4);
            listaDirectores.Add(dirNode);
        }

        // FILA 2: Gerentes
        List<EmployeeNode> listaGerentes = new List<EmployeeNode>();
        for (int i = 0; i < numGerentes; i++)
        {
            Departamento area = deptosGerentes[i];
            int totalGerentesArea = gerentesPorArea[area];
            int totalEmpleadosArea = empleadosPorArea[area];

            int factorMatricial = (criterio == CriterioDepartamentalizacion.Matricial) ? 2 : 1;
            int cupoMinimoGerente = Mathf.Max(1, Mathf.CeilToInt((float)(totalEmpleadosArea * factorMatricial) / totalGerentesArea));
            int holguraGte = (autoridad == ModoAutoridad.Descentralizada) ? 0 : (i % 2 == 0 ? 1 : 0);
            int cupoGerente = cupoMinimoGerente + holguraGte;

            string etiquetaArea = ObtenerEtiquetaCriterio(area, criterio);
            string nombreGte = ObtenerNombreUnico(new List<string>(prefijosGerente), etiquetaArea, nombresUsados);

            EmployeeNode gteNode = CrearNodoUI(NivelJerarquico.Gerente, area, nombreGte, false, cupoGerente, i, numGerentes, 4);
            listaGerentes.Add(gteNode);
        }

        // FILA 3: Especialistas / Empleados Base
        List<EmployeeNode> listaEmpleados = new List<EmployeeNode>();
        for (int i = 0; i < numEmpleados; i++)
        {
            Departamento area = deptosEmpleados[i];
            string etiquetaArea = ObtenerEtiquetaCriterio(area, criterio);
            string nombreEmp = ObtenerNombreUnico(new List<string>(prefijosEmpleado), etiquetaArea, nombresUsados);
            EmployeeNode empNode = CrearNodoUI(NivelJerarquico.EmpleadoBase, area, nombreEmp, false, 0, i, numEmpleados, 4);
            listaEmpleados.Add(empNode);
        }

        // Si es un escenario de Crisis / Reestructuración, pre-conectamos una rama parcial para darle dinamismo al tablero inicial
        if (preconectarCrisis && listaDirectores.Count > 0 && listaGerentes.Count > 0 && ceoNode != null)
        {
            UINodeInteraction.ConectarDosNodos(listaDirectores[0], ceoNode, silencioso: true);
            foreach (var gte in listaGerentes)
            {
                if (gte.departamento == listaDirectores[0].departamento)
                {
                    UINodeInteraction.ConectarDosNodos(gte, listaDirectores[0], silencioso: true);
                    break;
                }
            }
        }
    }

    private void GenerarEstructuraCentralizada(int cantidad, CriterioDepartamentalizacion criterio, bool preconectarCrisis)
    {
        int numMandos = Mathf.Clamp(cantidad / 3, 2, 3);
        int numEmpleados = Mathf.Clamp(cantidad - 1 - numMandos, 2, 5);

        List<Departamento> areasActivas = new List<Departamento>();
        for (int i = 0; i < numMandos; i++)
        {
            areasActivas.Add(areasDisponibles[i % areasDisponibles.Length]);
        }

        HashSet<string> nombresUsados = new HashSet<string>();

        string nombreCEO = ObtenerNombreUnico(new List<string>(nombresCEO), "Holding", nombresUsados);
        EmployeeNode ceo = CrearNodoUI(NivelJerarquico.CEO, Departamento.General, nombreCEO, true, numMandos + 1, 0, 1, 3);

        List<EmployeeNode> mandos = new List<EmployeeNode>();
        for (int i = 0; i < numMandos; i++)
        {
            Departamento area = areasActivas[i];
            string etiquetaArea = ObtenerEtiquetaCriterio(area, criterio);
            string nombreMando = ObtenerNombreUnico(new List<string>(prefijosDirector), etiquetaArea, nombresUsados);

            int empleadosDeEstaArea = Mathf.CeilToInt((float)numEmpleados / numMandos) + 1;
            EmployeeNode m = CrearNodoUI(NivelJerarquico.Director, area, nombreMando, false, empleadosDeEstaArea, i, numMandos, 3);
            mandos.Add(m);
        }

        for (int i = 0; i < numEmpleados; i++)
        {
            Departamento area = areasActivas[i % areasActivas.Count];
            string etiquetaArea = ObtenerEtiquetaCriterio(area, criterio);
            string nombreEmp = ObtenerNombreUnico(new List<string>(prefijosEmpleado), etiquetaArea, nombresUsados);
            CrearNodoUI(NivelJerarquico.EmpleadoBase, area, nombreEmp, false, 0, i, numEmpleados, 3, filaVisualOverride: 2);
        }

        if (preconectarCrisis && mandos.Count > 0 && ceo != null)
        {
            UINodeInteraction.ConectarDosNodos(mandos[0], ceo, silencioso: true);
        }
    }

    public void CargarDesdeLevelData(LevelData data)
    {
        if (data == null || data.empleados == null || data.empleados.Count == 0) return;

        AsegurarFondoVectorialTablero();
        LimpiarTablero();
        Canvas.ForceUpdateCanvases();

        Dictionary<NivelJerarquico, List<NodeSetup>> nodosPorNivel = new Dictionary<NivelJerarquico, List<NodeSetup>>();
        foreach (NivelJerarquico nv in System.Enum.GetValues(typeof(NivelJerarquico)))
        {
            nodosPorNivel[nv] = new List<NodeSetup>();
        }

        foreach (var emp in data.empleados)
        {
            nodosPorNivel[emp.nivel].Add(emp);
        }

        int totalFilas = 4;
        foreach (NivelJerarquico nv in System.Enum.GetValues(typeof(NivelJerarquico)))
        {
            List<NodeSetup> lista = nodosPorNivel[nv];
            for (int i = 0; i < lista.Count; i++)
            {
                NodeSetup item = lista[i];
                string etiquetaArea = ObtenerEtiquetaCriterio(item.departamento, data.criterioDepartamentalizacion);
                string tituloUI = $"{item.nombreRol}\n{etiquetaArea}";

                EmployeeNode nodo = CrearNodoUI(
                    item.nivel,
                    item.departamento,
                    tituloUI,
                    item.esDirectorGeneral,
                    item.maxReportesDirectos,
                    i,
                    lista.Count,
                    totalFilas
                );

                if (item.esStaff)
                {
                    nodo.isStaffNode = true;
                    nodo.ConfigureVisuals(tituloUI, ObtenerColorArea(Departamento.StaffAsesoria));
                }
            }
        }
    }

    public string ObtenerEtiquetaCriterio(Departamento depto, CriterioDepartamentalizacion criterio)
    {
        if (depto == Departamento.General) return "Holding";
        if (depto == Departamento.StaffAsesoria) return "Staff VIP";

        switch (criterio)
        {
            case CriterioDepartamentalizacion.Funcional:
                switch (depto)
                {
                    case Departamento.Finanzas: return "Finanzas";
                    case Departamento.Comercial: return "Growth";
                    case Departamento.Operaciones: return "Operaciones";
                    case Departamento.Tecnologia: return "Tech & AI";
                }
                break;

            case CriterioDepartamentalizacion.PorProducto:
                switch (depto)
                {
                    case Departamento.Finanzas: return "Div. Fintech";
                    case Departamento.Comercial: return "Div. Retail";
                    case Departamento.Operaciones: return "Div. Logística";
                    case Departamento.Tecnologia: return "Div. Cloud";
                }
                break;

            case CriterioDepartamentalizacion.Geografica:
                switch (depto)
                {
                    case Departamento.Finanzas: return "Reg. Norte";
                    case Departamento.Comercial: return "Reg. Centro";
                    case Departamento.Operaciones: return "Reg. Sur";
                    case Departamento.Tecnologia: return "Reg. Global";
                }
                break;

            case CriterioDepartamentalizacion.Matricial:
                switch (depto)
                {
                    case Departamento.Finanzas: return "Squad Alfa";
                    case Departamento.Comercial: return "Squad Beta";
                    case Departamento.Operaciones: return "Squad Gamma";
                    case Departamento.Tecnologia: return "Squad Delta";
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
        float areaHeight = spawnArea.rect.height > 100f ? spawnArea.rect.height : 1180f;

        float espacioPorColumna = areaWidth / Mathf.Max(1, totalEnFila);
        float anchoTarjeta = Mathf.Clamp(espacioPorColumna * 0.88f, 134f, 228f);
        float altoTarjeta = Mathf.Clamp((areaHeight / Mathf.Max(2, totalFilas)) * 0.44f, 90f, 118f);
        rect.sizeDelta = new Vector2(anchoTarjeta, altoTarjeta);

        float xPos = 0f;
        if (totalEnFila == 1)
        {
            xPos = 0f;
        }
        else
        {
            float margenLateral = (anchoTarjeta * 0.5f) + 14f;
            float minX = -(areaWidth * 0.5f) + margenLateral;
            float maxX = (areaWidth * 0.5f) - margenLateral;
            float t = (float)indexEnFila / (totalEnFila - 1);
            xPos = Mathf.Lerp(minX, maxX, t);
        }

        int filaIndice = filaVisualOverride >= 0 ? filaVisualOverride : (int)nivel;
        float margenVertical = (altoTarjeta * 0.5f) + 22f;
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
            txtRect.anchorMin = new Vector2(0.05f, 0.06f);
            txtRect.anchorMax = new Vector2(0.95f, 0.82f);
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
            textoUI.margin = Vector4.zero;
            textoUI.enableAutoSizing = true;
            textoUI.fontSizeMin = 11f;
            textoUI.fontSizeMax = 22f;
        }

        EmployeeNode dataNodo = nuevoNodoObj.GetComponent<EmployeeNode>();
        dataNodo.roleName = nombre.Replace("\n", " ");
        dataNodo.nivel = nivel;
        dataNodo.departamento = depto;
        dataNodo.isRootNode = esRaiz;
        dataNodo.maxDirectReports = maxReportes;
        dataNodo.isStaffNode = (depto == Departamento.StaffAsesoria);

        Color colorTarjeta = ObtenerColorArea(depto);
        dataNodo.ConfigureVisuals(nombre, colorTarjeta);

        return dataNodo;
    }

    public Color ObtenerColorArea(Departamento depto)
    {
        switch (depto)
        {
            case Departamento.General: return new Color(0.96f, 0.93f, 0.78f);       // Warm Platinum Gold (CEO)
            case Departamento.Finanzas: return new Color(0.62f, 0.95f, 0.78f);      // Mint Vector Emerald
            case Departamento.Comercial: return new Color(0.64f, 0.85f, 1.00f);     // Electric Sky Blue
            case Departamento.Operaciones: return new Color(1.00f, 0.82f, 0.58f);   // Warm Coral Amber
            case Departamento.Tecnologia: return new Color(0.85f, 0.72f, 1.00f);    // Neon Lavender Purple
            case Departamento.StaffAsesoria: return new Color(1.00f, 0.88f, 0.36f); // VIP Gold
            default: return Color.white;
        }
    }
}
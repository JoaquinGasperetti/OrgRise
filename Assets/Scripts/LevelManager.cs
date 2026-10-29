using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public LevelData nivelActual;
    public List<EmployeeNode> nodosActivos = new List<EmployeeNode>();
    private EmployeeNode nodoRaiz;

    [Header("Estado Académico de la Partida")]
    public int indiceNivelCampana = 0;
    public CriterioDepartamentalizacion criterioActual = CriterioDepartamentalizacion.Funcional;
    public ModoAutoridad modoAutoridadActual = ModoAutoridad.Equilibrada;
    public bool ExigirCadenaEstricta = true;
    private bool nivelYaCompletado = false;

    // UI de Cabecera Académica (creada automáticamente en Canvas)
    private TextMeshProUGUI headerTitleText;
    private TextMeshProUGUI headerSubText;

    private struct ConfiguracionNivelAcademico
    {
        public string titulo;
        public string subtitulo;
        public string resumenVictoria;
        public int cantidadNodos;
        public CriterioDepartamentalizacion criterio;
        public ModoAutoridad autoridad;
        public int semillaBase;
    }

    private readonly ConfiguracionNivelAcademico[] nivelesCampana = new ConfiguracionNivelAcademico[]
    {
        new ConfiguracionNivelAcademico
        {
            titulo = "Nivel 1: Cadena de Mando y Tramo de Control",
            subtitulo = "Departamentalización Funcional | Conectá cada rol respetando área y límite de reportes",
            resumenVictoria = "<b>Concepto validado: Cadena de Mando y Tramo de Control</b>\n\nEstableciste la línea de autoridad formal desde el Nivel Operativo hasta el Nivel Estratégico (CEO) sin superar la capacidad de supervisión directa de ningún mando.",
            cantidadNodos = 8,
            criterio = CriterioDepartamentalizacion.Funcional,
            autoridad = ModoAutoridad.Equilibrada,
            semillaBase = 10101
        },
        new ConfiguracionNivelAcademico
        {
            titulo = "Nivel 2: Departamentalización por Producto",
            subtitulo = "Agrupamiento por División (PyMEs, Consumo, Industria) | Unidad de Mando por línea de negocio",
            resumenVictoria = "<b>Concepto validado: Departamentalización por Producto</b>\n\nAgrupaste los roles según la línea de producto/servicio que atienden, facilitando la coordinación autónoma de cada unidad de negocio bajo un responsable divisional.",
            cantidadNodos = 9,
            criterio = CriterioDepartamentalizacion.PorProducto,
            autoridad = ModoAutoridad.Equilibrada,
            semillaBase = 20202
        },
        new ConfiguracionNivelAcademico
        {
            titulo = "Nivel 3: Departamentalización Geográfica",
            subtitulo = "Agrupamiento Territorial (Norte, Centro, Sur) | Gestión regional descentralizada",
            resumenVictoria = "<b>Concepto validado: Departamentalización Geográfica</b>\n\nOrganizaste las operaciones por zonas territoriales para responder con agilidad a las necesidades de cada mercado regional.",
            cantidadNodos = 10,
            criterio = CriterioDepartamentalizacion.Geografica,
            autoridad = ModoAutoridad.Equilibrada,
            semillaBase = 30303
        },
        new ConfiguracionNivelAcademico
        {
            titulo = "Nivel 4: Estructura Centralizada (Horizontal / Chata)",
            subtitulo = "Tramo de control amplio en la cúpula | Menos niveles jerárquicos intermedios",
            resumenVictoria = "<b>Concepto validado: Centralización y Estructura Chata</b>\n\nCon un tramo de control amplio y menos eslabones intermedios, la Dirección retiene la supervisión cercana, acortando la distancia entre la estrategia y la operación.",
            cantidadNodos = 8,
            criterio = CriterioDepartamentalizacion.Funcional,
            autoridad = ModoAutoridad.Centralizada,
            semillaBase = 40404
        },
        new ConfiguracionNivelAcademico
        {
            titulo = "Nivel 5: Estructura Descentralizada (Vertical / Alta)",
            subtitulo = "Tramo de control estrecho | Delegación obligatoria en Directores y Gerentes",
            resumenVictoria = "<b>Concepto validado: Descentralización y Delegación</b>\n\nCon tramos de control ajustados, el CEO delega la autoridad táctica en Directores y Gerentes sin generar cuellos de botella en la cima estratégica.",
            cantidadNodos = 11,
            criterio = CriterioDepartamentalizacion.Funcional,
            autoridad = ModoAutoridad.Descentralizada,
            semillaBase = 50505
        }
    };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void IniciarJuego()
    {
        CrearCabeceraAcademicaUI();
        CargarNivelActual();

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.IniciarTutorial();
        }
    }

    public void LimpiarNodosRegistrados()
    {
        nodosActivos.Clear();
        nodoRaiz = null;
        nivelYaCompletado = false;
    }

    public void RegistrarNodo(EmployeeNode nodo, bool esRaiz)
    {
        if (nodo == null) return;
        if (!nodosActivos.Contains(nodo))
        {
            nodosActivos.Add(nodo);
        }
        if (esRaiz) nodoRaiz = nodo;
    }

    public bool ExisteNivelIntermedioParaArea(NivelJerarquico nivelSubordinado, Departamento depto)
    {
        NivelJerarquico nivelInmediatoSuperior = (NivelJerarquico)((int)nivelSubordinado - 1);
        foreach (var n in nodosActivos)
        {
            if (n != null && n.nivel == nivelInmediatoSuperior && (n.departamento == depto || n.departamento == Departamento.General))
            {
                return true;
            }
        }
        return false;
    }

    public string ObtenerNombreDepartamento(Departamento depto)
    {
        if (ProceduralLevelGenerator.Instance != null)
        {
            return ProceduralLevelGenerator.Instance.ObtenerEtiquetaCriterio(depto, criterioActual);
        }
        return depto.ToString();
    }

    public void CargarNivelActual()
    {
        if (ProceduralLevelGenerator.Instance == null) return;

        if (indiceNivelCampana < nivelesCampana.Length)
        {
            ConfiguracionNivelAcademico cfg = nivelesCampana[indiceNivelCampana];
            criterioActual = cfg.criterio;
            modoAutoridadActual = cfg.autoridad;
            ExigirCadenaEstricta = true;

            ActualizarCabeceraUI(cfg.titulo, cfg.subtitulo);
            ProceduralLevelGenerator.Instance.GenerarNivelAcademico(cfg.semillaBase, cfg.cantidadNodos, cfg.criterio, cfg.autoridad);
        }
        else
        {
            // Modo Puzzle Diario Procedural infinito tras completar los 5 niveles teóricos
            int numDia = indiceNivelCampana - nivelesCampana.Length + 1;
            int seed = ProceduralLevelGenerator.ObtenerSemillaDiaria() + (numDia * 37);

            CriterioDepartamentalizacion[] criterios = {
                CriterioDepartamentalizacion.Funcional,
                CriterioDepartamentalizacion.PorProducto,
                CriterioDepartamentalizacion.Geografica,
                CriterioDepartamentalizacion.Matricial
            };
            criterioActual = criterios[numDia % criterios.Length];
            modoAutoridadActual = (numDia % 3 == 0) ? ModoAutoridad.Descentralizada : ModoAutoridad.Equilibrada;
            ExigirCadenaEstricta = true;

            ActualizarCabeceraUI(
                $"Desafío Diario #{numDia}: {criterioActual}",
                $"Modo {modoAutoridadActual} | Semilla #{seed} | Doble clic en un nodo para desconectar"
            );

            ProceduralLevelGenerator.Instance.GenerarNivelAcademico(seed, 10, criterioActual, modoAutoridadActual);
        }
    }

    public void AvanzarSiguienteNivel()
    {
        indiceNivelCampana++;
        // Recompensamos 1 comodín cada 2 niveles superados para facilitar pruebas de reestructuración
        if (indiceNivelCampana % 2 == 0 && WildcardManager.Instance != null)
        {
            WildcardManager.Instance.RecompensarComodin(1);
        }
        CargarNivelActual();
    }

    public void ReiniciarNivelActual()
    {
        CargarNivelActual();
    }

    /// <summary>
    /// Se llama cada vez que el jugador hace una conexión exitosa.
    /// </summary>
    public void EvaluarVictoria()
    {
        if (nivelYaCompletado) return;

        if (OrgValidator.ValidateWinCondition(nodosActivos, nodoRaiz))
        {
            nivelYaCompletado = true;
            Debug.Log("¡Puzzle Resuelto! Organigrama válido.");
            EjecutarFeedbackVictoria();
        }
    }

    private void EjecutarFeedbackVictoria()
    {
        if (FeedbackManager.Instance != null)
            FeedbackManager.Instance.PlayVictoryFeedback(nodosActivos);

        if (DailyStreakManager.Instance != null)
            DailyStreakManager.Instance.RegisterWin();

        if (StreakUI.Instance != null)
            StreakUI.Instance.ActualizarPantalla();

        StartCoroutine(MostrarModalVictoriaConRetraso(0.85f));
    }

    private IEnumerator MostrarModalVictoriaConRetraso(float espera)
    {
        yield return new WaitForSeconds(espera);

        string resumen = indiceNivelCampana < nivelesCampana.Length
            ? nivelesCampana[indiceNivelCampana].resumenVictoria
            : $"<b>¡Desafío Organizacional Completado!</b>\n\nResolviste correctamente el organigrama bajo criterio <b>{criterioActual}</b> y autoridad <b>{modoAutoridadActual}</b>.";

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.MostrarPantallaNivelCompletado(resumen);
        }
        else
        {
            AvanzarSiguienteNivel();
        }
    }

    private void CrearCabeceraAcademicaUI()
    {
        if (headerTitleText != null) return;

        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null) return;

        // Ponemos un fondo de color sobrio corporativo a la cámara para mejorar el contraste visual
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = new Color(0.12f, 0.16f, 0.22f, 1f);
        }

        GameObject headerPanel = new GameObject("AcademicHeaderPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        headerPanel.transform.SetParent(mainCanvas.transform, false);
        headerPanel.transform.SetAsFirstSibling();

        RectTransform rt = headerPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -110f);
        rt.sizeDelta = new Vector2(-40f, 135f);

        Image bg = headerPanel.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.11f, 0.16f, 0.88f);
        bg.raycastTarget = false;

        // Título del nivel
        GameObject titleObj = new GameObject("HeaderTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(headerPanel.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.48f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.offsetMin = new Vector2(20f, 0f);
        titleRt.offsetMax = new Vector2(-20f, -8f);

        headerTitleText = titleObj.GetComponent<TextMeshProUGUI>();
        headerTitleText.fontSize = 30f;
        headerTitleText.fontStyle = FontStyles.Bold;
        headerTitleText.alignment = TextAlignmentOptions.Center;
        headerTitleText.color = new Color(0.95f, 0.85f, 0.35f);
        headerTitleText.enableAutoSizing = true;
        headerTitleText.fontSizeMin = 20f;
        headerTitleText.fontSizeMax = 32f;
        headerTitleText.raycastTarget = false;

        // Subtítulo de reglas
        GameObject subObj = new GameObject("HeaderSubtitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(headerPanel.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 0f);
        subRt.anchorMax = new Vector2(1f, 0.50f);
        subRt.offsetMin = new Vector2(20f, 8f);
        subRt.offsetMax = new Vector2(-20f, 0f);

        headerSubText = subObj.GetComponent<TextMeshProUGUI>();
        headerSubText.fontSize = 21f;
        headerSubText.alignment = TextAlignmentOptions.Center;
        headerSubText.color = new Color(0.85f, 0.90f, 0.96f);
        headerSubText.enableAutoSizing = true;
        headerSubText.fontSizeMin = 15f;
        headerSubText.fontSizeMax = 22f;
        headerSubText.raycastTarget = false;
    }

    private void ActualizarCabeceraUI(string titulo, string subtitulo)
    {
        if (headerTitleText != null) headerTitleText.text = titulo;
        if (headerSubText != null) headerSubText.text = subtitulo;
    }
}
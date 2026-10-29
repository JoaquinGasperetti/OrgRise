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

    // UI de Cabecera Académica
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
            subtitulo = "Departamentalización Funcional | Conectá cada rol respetando su área y cupo",
            resumenVictoria = "<b>Concepto validado: Cadena de Mando y Tramo de Control</b>\n\nEstableciste la línea de autoridad formal desde el Nivel Operativo hasta el Nivel Estratégico (CEO) sin superar la capacidad de supervisión directa de ningún mando.",
            cantidadNodos = 8,
            criterio = CriterioDepartamentalizacion.Funcional,
            autoridad = ModoAutoridad.Equilibrada,
            semillaBase = 10101
        },
        new ConfiguracionNivelAcademico
        {
            titulo = "Nivel 2: Departamentalización por Producto",
            subtitulo = "Agrupamiento por División (PyMEs, Consumo, Industria) | Unidad de Mando",
            resumenVictoria = "<b>Concepto validado: Departamentalización por Producto</b>\n\nAgrupaste los roles según la línea de producto/servicio que atienden, facilitando la coordinación autónoma de cada unidad de negocio.",
            cantidadNodos = 9,
            criterio = CriterioDepartamentalizacion.PorProducto,
            autoridad = ModoAutoridad.Equilibrada,
            semillaBase = 20202
        },
        new ConfiguracionNivelAcademico
        {
            titulo = "Nivel 3: Departamentalización Geográfica",
            subtitulo = "Agrupamiento Territorial (Norte, Centro, Sur) | Gestión regional",
            resumenVictoria = "<b>Concepto validado: Departamentalización Geográfica</b>\n\nOrganizaste las operaciones por zonas territoriales para responder con agilidad a las demandas de cada región.",
            cantidadNodos = 10,
            criterio = CriterioDepartamentalizacion.Geografica,
            autoridad = ModoAutoridad.Equilibrada,
            semillaBase = 30303
        },
        new ConfiguracionNivelAcademico
        {
            titulo = "Nivel 4: Estructura Centralizada (Chata)",
            subtitulo = "Tramo de control amplio en la cúpula | Menos niveles intermedios",
            resumenVictoria = "<b>Concepto validado: Centralización y Estructura Chata</b>\n\nCon un tramo de control amplio y menos eslabones intermedios, la Dirección retiene la supervisión cercana, acortando la cadena jerárquica.",
            cantidadNodos = 8,
            criterio = CriterioDepartamentalizacion.Funcional,
            autoridad = ModoAutoridad.Centralizada,
            semillaBase = 40404
        },
        new ConfiguracionNivelAcademico
        {
            titulo = "Nivel 5: Estructura Descentralizada (Vertical)",
            subtitulo = "Tramo de control estrecho | Delegación obligatoria en mandos medios",
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
        NormalizarLayoutVerticalMovil();
        CrearCabeceraAcademicaUI();
        CargarNivelActual();

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.IniciarTutorial();
        }
    }

    /// <summary>
    /// Corrige el CanvasScaler y todos los RectTransforms de la escena para que encajen siempre
    /// dentro de cualquier pantalla móvil vertical (9:16, 9:18, 9:20) sin desbordarse.
    /// </summary>
    private void NormalizarLayoutVerticalMovil()
    {
        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null) return;

        // 1. Ajustar CanvasScaler para que escale combinando ancho y alto (evita desbordes verticales y horizontales)
        CanvasScaler scaler = mainCanvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        // 2. Ajustar SpawnArea para dejar margen superior (cabecera + racha) e inferior (comodín + mensajes)
        if (ProceduralLevelGenerator.Instance != null && ProceduralLevelGenerator.Instance.spawnArea != null)
        {
            RectTransform spawnRt = ProceduralLevelGenerator.Instance.spawnArea;
            spawnRt.localScale = Vector3.one;
            spawnRt.anchorMin = new Vector2(0.03f, 0.16f);
            spawnRt.anchorMax = new Vector2(0.97f, 0.82f);
            spawnRt.offsetMin = Vector2.zero;
            spawnRt.offsetMax = Vector2.zero;
        }

        // 3. Ajustar TextRacha en la esquina superior izquierda (sin márgenes negativos que se salgan del Canvas)
        if (StreakUI.Instance != null && StreakUI.Instance.textoRacha != null)
        {
            TextMeshProUGUI txtRacha = StreakUI.Instance.textoRacha;
            RectTransform rachaRt = txtRacha.GetComponent<RectTransform>();
            rachaRt.localScale = Vector3.one;
            rachaRt.anchorMin = new Vector2(0.04f, 0.92f);
            rachaRt.anchorMax = new Vector2(0.45f, 0.985f);
            rachaRt.offsetMin = Vector2.zero;
            rachaRt.offsetMax = Vector2.zero;

            txtRacha.margin = Vector4.zero;
            txtRacha.alignment = TextAlignmentOptions.MidlineLeft;
            txtRacha.enableAutoSizing = true;
            txtRacha.fontSizeMin = 28f;
            txtRacha.fontSizeMax = 48f;
        }

        Canvas.ForceUpdateCanvases();
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
                $"Modo {modoAutoridadActual} | Doble toque en un nodo para desconectar"
            );

            ProceduralLevelGenerator.Instance.GenerarNivelAcademico(seed, 10, criterioActual, modoAutoridadActual);
        }
    }

    public void AvanzarSiguienteNivel()
    {
        indiceNivelCampana++;
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

        if (Camera.main != null)
        {
            Camera.main.backgroundColor = new Color(0.12f, 0.16f, 0.22f, 1f);
        }

        GameObject headerPanel = new GameObject("AcademicHeaderPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        headerPanel.transform.SetParent(mainCanvas.transform, false);
        headerPanel.transform.SetAsFirstSibling();

        // Anclado en porcentaje entre el 83% y el 91.5% de la altura del teléfono
        RectTransform rt = headerPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.03f, 0.83f);
        rt.anchorMax = new Vector2(0.97f, 0.915f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = headerPanel.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.11f, 0.16f, 0.92f);
        bg.raycastTarget = false;

        // Título del nivel
        GameObject titleObj = new GameObject("HeaderTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(headerPanel.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.03f, 0.48f);
        titleRt.anchorMax = new Vector2(0.97f, 0.96f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;

        headerTitleText = titleObj.GetComponent<TextMeshProUGUI>();
        headerTitleText.fontStyle = FontStyles.Bold;
        headerTitleText.alignment = TextAlignmentOptions.Center;
        headerTitleText.color = new Color(0.95f, 0.85f, 0.35f);
        headerTitleText.enableAutoSizing = true;
        headerTitleText.fontSizeMin = 16f;
        headerTitleText.fontSizeMax = 28f;
        headerTitleText.raycastTarget = false;

        // Subtítulo de reglas
        GameObject subObj = new GameObject("HeaderSubtitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(headerPanel.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.03f, 0.06f);
        subRt.anchorMax = new Vector2(0.97f, 0.48f);
        subRt.offsetMin = Vector2.zero;
        subRt.offsetMax = Vector2.zero;

        headerSubText = subObj.GetComponent<TextMeshProUGUI>();
        headerSubText.alignment = TextAlignmentOptions.Center;
        headerSubText.color = new Color(0.85f, 0.90f, 0.96f);
        headerSubText.enableAutoSizing = true;
        headerSubText.fontSizeMin = 13f;
        headerSubText.fontSizeMax = 21f;
        headerSubText.raycastTarget = false;

        // Botón rápido para reiniciar el nivel en la esquina superior derecha
        CrearBotonReiniciarUI(mainCanvas.transform);
    }

    private void CrearBotonReiniciarUI(Transform canvasTransform)
    {
        GameObject btnObj = new GameObject("BotonReiniciarNivel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(canvasTransform, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.68f, 0.925f);
        rt.anchorMax = new Vector2(0.96f, 0.98f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(0.22f, 0.28f, 0.38f, 0.95f);

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(ReiniciarNivelActual);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(8f, 4f);
        txtRt.offsetMax = new Vector2(-8f, -4f);

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = "↻ Reiniciar";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 14f;
        tmp.fontSizeMax = 24f;
    }

    private void ActualizarCabeceraUI(string titulo, string subtitulo)
    {
        if (headerTitleText != null) headerTitleText.text = titulo;
        if (headerSubText != null) headerSubText.text = subtitulo;
    }
}
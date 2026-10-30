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
    private bool enModoDiario = false;
    private int numeroDesafioDiario = 1;

    // UI de Cabecera Académica y Navegación
    private GameObject headerPanelObj;
    private TextMeshProUGUI headerTitleText;
    private TextMeshProUGUI headerSubText;
    private GameObject topButtonsContainer;

    // UI de Menú Principal, Selector de Nivel y Glosario
    private GameObject mainMenuPanel;
    private GameObject mainHomeView;
    private GameObject levelSelectorView;
    private GameObject glossaryView;
    private TextMeshProUGUI menuStreakBadgeText;

    public struct ConfiguracionNivelAcademico
    {
        public string nombreCorto;
        public string etiquetaConcepto;
        public string titulo;
        public string subtitulo;
        public string resumenVictoria;
        public int cantidadNodos;
        public CriterioDepartamentalizacion criterio;
        public ModoAutoridad autoridad;
        public int semillaBase;
    }

    public readonly ConfiguracionNivelAcademico[] nivelesCampana = new ConfiguracionNivelAcademico[]
    {
        new ConfiguracionNivelAcademico
        {
            nombreCorto = "Nivel 1: Cadena y Tramo de Control",
            etiquetaConcepto = "Fayol • Jerarquía Lineal-Funcional",
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
            nombreCorto = "Nivel 2: Departamentalización por Producto",
            etiquetaConcepto = "Estructura Divisional • Unidades de Negocio",
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
            nombreCorto = "Nivel 3: Departamentalización Geográfica",
            etiquetaConcepto = "Cobertura Territorial • Regiones Operativas",
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
            nombreCorto = "Nivel 4: Estructura Centralizada (Chata)",
            etiquetaConcepto = "Tramo Amplio • Supervisión Directa",
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
            nombreCorto = "Nivel 5: Estructura Descentralizada (Vertical)",
            etiquetaConcepto = "Tramo Estrecho • Delegación Táctica",
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
        CrearMenuPrincipalUI();

        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.CrearFranjaSeguraBannerUI();
        }

        // Al abrir la app mostramos el Menú Principal (ocultando cualquier modal abierto del tutorial hasta que empiece a jugar)
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OcultarPanelInmediato();
        }

        MostrarMenuPrincipalInterno();
    }

    public void RefrescarLayoutResponsivo()
    {
        NormalizarLayoutVerticalMovil();
        if (mainMenuPanel != null)
        {
            RectTransform rootRt = mainMenuPanel.GetComponent<RectTransform>();
            if (rootRt != null)
            {
                float bottomSafe = AdMobManager.Instance != null ? AdMobManager.Instance.ObtenerAlturaRelativaZonaSeguraBanner() : 0.068f;
                rootRt.anchorMin = new Vector2(0f, bottomSafe);
                rootRt.anchorMax = new Vector2(1f, 1f);
                rootRt.offsetMin = Vector2.zero;
                rootRt.offsetMax = Vector2.zero;
            }
        }
    }

    private void NormalizarLayoutVerticalMovil()
    {
        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null) return;

        CanvasScaler scaler = mainCanvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (ProceduralLevelGenerator.Instance != null && ProceduralLevelGenerator.Instance.spawnArea != null)
        {
            RectTransform spawnRt = ProceduralLevelGenerator.Instance.spawnArea;
            spawnRt.localScale = Vector3.one;
            // Reservamos de 0.20 a 0.82 para dejar espacio holgado a los botones de Comodín/Bonificado (0.138-0.192),
            // al Toast de estado (0.072-0.132) y al Banner inferior de AdMob (0.00-0.065)
            spawnRt.anchorMin = new Vector2(0.03f, 0.20f);
            spawnRt.anchorMax = new Vector2(0.97f, 0.82f);
            spawnRt.offsetMin = Vector2.zero;
            spawnRt.offsetMax = Vector2.zero;
        }

        if (StreakUI.Instance != null && StreakUI.Instance.textoRacha != null)
        {
            TextMeshProUGUI txtRacha = StreakUI.Instance.textoRacha;
            RectTransform rachaRt = txtRacha.GetComponent<RectTransform>();
            rachaRt.localScale = Vector3.one;
            rachaRt.anchorMin = new Vector2(0.04f, 0.925f);
            rachaRt.anchorMax = new Vector2(0.32f, 0.985f);
            rachaRt.offsetMin = Vector2.zero;
            rachaRt.offsetMax = Vector2.zero;

            txtRacha.margin = Vector4.zero;
            txtRacha.alignment = TextAlignmentOptions.MidlineLeft;
            txtRacha.enableAutoSizing = true;
            txtRacha.fontSizeMin = 24f;
            txtRacha.fontSizeMax = 44f;
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

    // ==================== NAVEGACIÓN DE MENÚ PRINCIPAL Y MODOS ====================

    public void MostrarMenuPrincipal()
    {
        bool veniaDePartida = mainMenuPanel != null && !mainMenuPanel.activeSelf && nodosActivos.Count > 0;

        if (veniaDePartida && AdMobManager.Instance != null)
        {
            AdMobManager.Instance.RegistrarTransicionYMostrarIntersticialSiCorresponde(MostrarMenuPrincipalInterno);
        }
        else
        {
            MostrarMenuPrincipalInterno();
        }
    }

    private void MostrarMenuPrincipalInterno()
    {
        if (mainMenuPanel == null) CrearMenuPrincipalUI();
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            mainMenuPanel.transform.SetAsLastSibling();
        }

        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.CrearFranjaSeguraBannerUI();
        }

        CambiarSubvistaMenu("home");
        ActualizarBadgeMenu();
    }

    public void ActualizarBadgeMenu()
    {
        if (menuStreakBadgeText != null && DailyStreakManager.Instance != null)
        {
            int racha = DailyStreakManager.Instance.currentStreak;
            string estadoHoy = DailyStreakManager.Instance.alreadyPlayedToday ? "Completado hoy ✓" : "Disponible hoy";
            int comodines = WildcardManager.Instance != null ? WildcardManager.Instance.wildcardsDisponibles : 2;
            menuStreakBadgeText.text = $"🔥 Racha: <b>{racha}d</b>  |  ⚡ Comodines: <b>{comodines}</b>  |  <color=#80CBC4>{estadoHoy}</color>";
        }
    }

    private void OcultarMenuPrincipal()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
    }

    private void CambiarSubvistaMenu(string vista)
    {
        if (mainHomeView != null) mainHomeView.SetActive(vista == "home");
        if (levelSelectorView != null) levelSelectorView.SetActive(vista == "selector");
        if (glossaryView != null) glossaryView.SetActive(vista == "glossary");
    }

    public void IniciarNivelCampanaDesdeMenu(int indice)
    {
        System.Action iniciarAccion = () =>
        {
            enModoDiario = false;
            indiceNivelCampana = Mathf.Clamp(indice, 0, nivelesCampana.Length - 1);
            OcultarMenuPrincipal();
            CargarNivelActual();

            // Si eligen el Nivel 1 y aún no vieron el tutorial en esta sesión, lo mostramos
            if (indiceNivelCampana == 0 && TutorialManager.Instance != null)
            {
                TutorialManager.Instance.IniciarTutorial();
            }
        };

        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.RegistrarTransicionYMostrarIntersticialSiCorresponde(iniciarAccion);
        }
        else
        {
            iniciarAccion.Invoke();
        }
    }

    public void IniciarDesafioDiarioDesdeMenu()
    {
        System.Action iniciarDiarioAccion = () =>
        {
            enModoDiario = true;
            OcultarMenuPrincipal();
            CargarNivelActual();

            if (FeedbackManager.Instance != null)
            {
                int seed = ProceduralLevelGenerator.ObtenerSemillaDiaria();
                FeedbackManager.Instance.MostrarMensajeEstado(
                    $"📅 Puzzle Diario iniciado (Semilla #{seed}). ¡Armá el organigrama para sumar a tu racha!",
                    new Color(0.14f, 0.36f, 0.54f, 0.95f),
                    3.2f
                );
            }
        };

        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.RegistrarTransicionYMostrarIntersticialSiCorresponde(iniciarDiarioAccion);
        }
        else
        {
            iniciarDiarioAccion.Invoke();
        }
    }

    public void CargarNivelActual()
    {
        if (ProceduralLevelGenerator.Instance == null) return;

        if (!enModoDiario && indiceNivelCampana < nivelesCampana.Length)
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
            enModoDiario = true;
            int seed = ProceduralLevelGenerator.ObtenerSemillaDiaria() + ((numeroDesafioDiario - 1) * 37);

            CriterioDepartamentalizacion[] criterios = {
                CriterioDepartamentalizacion.Funcional,
                CriterioDepartamentalizacion.PorProducto,
                CriterioDepartamentalizacion.Geografica,
                CriterioDepartamentalizacion.Matricial
            };
            criterioActual = criterios[(seed + numeroDesafioDiario) % criterios.Length];
            modoAutoridadActual = (numeroDesafioDiario % 3 == 0)
                ? ModoAutoridad.Descentralizada
                : ((numeroDesafioDiario % 2 == 0) ? ModoAutoridad.Centralizada : ModoAutoridad.Equilibrada);
            ExigirCadenaEstricta = true;

            ActualizarCabeceraUI(
                $"📅 Desafío Diario #{numeroDesafioDiario}: Criterio {criterioActual}",
                $"Autoridad {modoAutoridadActual} | Semilla #{seed} | Doble toque para desconectar"
            );

            ProceduralLevelGenerator.Instance.GenerarNivelAcademico(seed, 10, criterioActual, modoAutoridadActual);
        }
    }

    public void AvanzarSiguienteNivel()
    {
        if (enModoDiario)
        {
            numeroDesafioDiario++;
        }
        else
        {
            indiceNivelCampana++;
            if (indiceNivelCampana >= nivelesCampana.Length)
            {
                enModoDiario = true;
                numeroDesafioDiario = 1;
            }
        }

        if (WildcardManager.Instance != null && (indiceNivelCampana % 2 == 0 || enModoDiario))
        {
            WildcardManager.Instance.RecompensarComodin(1);
        }

        CargarNivelActual();
    }

    public void ReiniciarNivelActual()
    {
        System.Action reiniciarAccion = () =>
        {
            CargarNivelActual();
            if (FeedbackManager.Instance != null)
            {
                FeedbackManager.Instance.MostrarMensajeEstado("↻ Tablero reiniciado.", new Color(0.22f, 0.28f, 0.38f, 0.92f), 1.6f);
            }
        };

        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.RegistrarTransicionYMostrarIntersticialSiCorresponde(reiniciarAccion);
        }
        else
        {
            reiniciarAccion.Invoke();
        }
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

        string resumen = (!enModoDiario && indiceNivelCampana < nivelesCampana.Length)
            ? nivelesCampana[indiceNivelCampana].resumenVictoria
            : $"<b>¡Desafío Diario #{numeroDesafioDiario} Completado!</b>\n\nResolviste correctamente el organigrama bajo departamentalización <b>{criterioActual}</b> y autoridad <b>{modoAutoridadActual}</b>.";

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.MostrarPantallaNivelCompletado(resumen);
        }
        else
        {
            AvanzarSiguienteNivel();
        }
    }

    // ==================== CONSTRUCCIÓN DE CABECERA Y BOTONES DE JUEGO ====================

    private void CrearCabeceraAcademicaUI()
    {
        if (headerTitleText != null) return;

        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null) return;

        if (Camera.main != null)
        {
            Camera.main.backgroundColor = new Color(0.11f, 0.15f, 0.21f, 1f);
        }

        headerPanelObj = new GameObject("AcademicHeaderPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        headerPanelObj.transform.SetParent(mainCanvas.transform, false);
        headerPanelObj.transform.SetAsFirstSibling();

        RectTransform rt = headerPanelObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.03f, 0.83f);
        rt.anchorMax = new Vector2(0.97f, 0.915f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = headerPanelObj.GetComponent<Image>();
        bg.color = new Color(0.07f, 0.10f, 0.15f, 0.94f);
        bg.raycastTarget = false;

        GameObject titleObj = new GameObject("HeaderTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(headerPanelObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.03f, 0.48f);
        titleRt.anchorMax = new Vector2(0.97f, 0.96f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;

        headerTitleText = titleObj.GetComponent<TextMeshProUGUI>();
        headerTitleText.fontStyle = FontStyles.Bold;
        headerTitleText.alignment = TextAlignmentOptions.Center;
        headerTitleText.color = new Color(0.96f, 0.85f, 0.35f);
        headerTitleText.enableAutoSizing = true;
        headerTitleText.fontSizeMin = 15f;
        headerTitleText.fontSizeMax = 27f;
        headerTitleText.raycastTarget = false;

        GameObject subObj = new GameObject("HeaderSubtitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(headerPanelObj.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.03f, 0.06f);
        subRt.anchorMax = new Vector2(0.97f, 0.48f);
        subRt.offsetMin = Vector2.zero;
        subRt.offsetMax = Vector2.zero;

        headerSubText = subObj.GetComponent<TextMeshProUGUI>();
        headerSubText.alignment = TextAlignmentOptions.Center;
        headerSubText.color = new Color(0.85f, 0.90f, 0.96f);
        headerSubText.enableAutoSizing = true;
        headerSubText.fontSizeMin = 12f;
        headerSubText.fontSizeMax = 20f;
        headerSubText.raycastTarget = false;

        // Botones superiores de navegación en partida: [☰ Menú / Niveles] y [↻ Reiniciar]
        CrearBotonesBarraSuperior(mainCanvas.transform);
    }

    private void CrearBotonesBarraSuperior(Transform canvasTransform)
    {
        topButtonsContainer = new GameObject("TopNavigationButtons", typeof(RectTransform));
        topButtonsContainer.transform.SetParent(canvasTransform, false);
        RectTransform containerRt = topButtonsContainer.GetComponent<RectTransform>();
        containerRt.anchorMin = Vector2.zero;
        containerRt.anchorMax = Vector2.one;
        containerRt.offsetMin = Vector2.zero;
        containerRt.offsetMax = Vector2.zero;

        // Botón Menú / Selector de Niveles (centro-derecha superior)
        CrearBotonUI(
            topButtonsContainer.transform,
            "BotonAbrirMenu",
            "☰ Menú / Niveles",
            new Vector2(0.35f, 0.925f),
            new Vector2(0.67f, 0.98f),
            new Color(0.16f, 0.35f, 0.55f, 0.96f),
            MostrarMenuPrincipal
        );

        // Botón Reiniciar Nivel (derecha superior)
        CrearBotonUI(
            topButtonsContainer.transform,
            "BotonReiniciarNivel",
            "↻ Reiniciar",
            new Vector2(0.69f, 0.925f),
            new Vector2(0.96f, 0.98f),
            new Color(0.22f, 0.28f, 0.38f, 0.96f),
            ReiniciarNivelActual
        );
    }

    // ==================== CONSTRUCCIÓN DEL MENÚ PRINCIPAL, SELECTOR Y GLOSARIO ====================

    private void CrearMenuPrincipalUI()
    {
        if (mainMenuPanel != null) return;

        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null) return;

        mainMenuPanel = new GameObject("MainMenuOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        mainMenuPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform rootRt = mainMenuPanel.GetComponent<RectTransform>();
        rootRt.localScale = Vector3.one;
        // Dejamos libre la franja inferior 0.00 a 0.065 para el Banner de AdMob
        rootRt.anchorMin = new Vector2(0f, 0.065f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        Image rootBg = mainMenuPanel.GetComponent<Image>();
        rootBg.color = new Color(0.06f, 0.09f, 0.14f, 0.98f);
        rootBg.raycastTarget = true;

        ConstruirSubvistaHome(mainMenuPanel.transform);
        ConstruirSubvistaSelectorNiveles(mainMenuPanel.transform);
        ConstruirSubvistaGlosario(mainMenuPanel.transform);
    }

    private void ConstruirSubvistaHome(Transform parent)
    {
        mainHomeView = CrearContenedorVista(parent, "HomeView");

        // Badge superior de cátedra
        CrearTextoUI(
            mainHomeView.transform,
            "AcademicTag",
            "TRABAJO FINAL INTEGRADOR • GESTIÓN DE LA ORGANIZACIÓN",
            new Vector2(0.06f, 0.90f),
            new Vector2(0.94f, 0.95f),
            13f, 21f,
            new Color(0.50f, 0.80f, 0.77f),
            FontStyles.Bold
        );

        // Título principal
        CrearTextoUI(
            mainHomeView.transform,
            "GameTitle",
            "🏢 OrgRise",
            new Vector2(0.06f, 0.79f),
            new Vector2(0.94f, 0.90f),
            38f, 72f,
            Color.white,
            FontStyles.Bold
        );

        // Subtítulo descriptivo
        CrearTextoUI(
            mainHomeView.transform,
            "GameSubtitle",
            "Simulador y Puzzle de Diseño de Estructuras Organizacionales\n<size=75%><color=#B0BEC5>Cadena de Mando • Tramo de Control • Departamentalización • Centralización</color></size>",
            new Vector2(0.07f, 0.70f),
            new Vector2(0.93f, 0.79f),
            15f, 26f,
            new Color(0.95f, 0.85f, 0.35f),
            FontStyles.Normal
        );

        // Tarjeta de estado de Racha Diaria y Comodines
        GameObject streakCard = CrearPanelTarjeta(
            mainHomeView.transform,
            "StreakBadgeCard",
            new Vector2(0.08f, 0.62f),
            new Vector2(0.92f, 0.685f),
            new Color(0.11f, 0.16f, 0.24f, 1f)
        );
        menuStreakBadgeText = CrearTextoUI(
            streakCard.transform,
            "StreakBadgeText",
            "🔥 Racha: 0d  |  ⚡ Comodines: 2  |  Disponible hoy",
            new Vector2(0.04f, 0.1f),
            new Vector2(0.96f, 0.9f),
            14f, 24f,
            Color.white,
            FontStyles.Normal
        );

        // Botón 1: Jugar Campaña Académica (Nivel Actual)
        CrearBotonMenuGrande(
            mainHomeView.transform,
            "BtnPlayCampaign",
            "▶  MODO CAMPAÑA ACADÉMICA",
            "Recorrido guiado por los 5 modelos de estructura organizacional",
            new Vector2(0.08f, 0.495f),
            new Vector2(0.92f, 0.60f),
            new Color(0.15f, 0.52f, 0.34f, 1f),
            () => IniciarNivelCampanaDesdeMenu(indiceNivelCampana < nivelesCampana.Length ? indiceNivelCampana : 0)
        );

        // Botón 2: Selector de Nivel (Acceso directo para la defensa oral)
        CrearBotonMenuGrande(
            mainHomeView.transform,
            "BtnOpenLevelSelector",
            "🗂️  SELECTOR DE NIVELES (5 CASOS)",
            "Elegí directamente qué concepto o estructura demostrar",
            new Vector2(0.08f, 0.375f),
            new Vector2(0.92f, 0.48f),
            new Color(0.16f, 0.38f, 0.62f, 1f),
            () => CambiarSubvistaMenu("selector")
        );

        // Botón 3: Desafío Diario Procedural
        CrearBotonMenuGrande(
            mainHomeView.transform,
            "BtnDailyChallenge",
            "📅  DESAFÍO DIARIO PROCEDURAL",
            "Generación algorítmica con semilla diaria y racha de victorias",
            new Vector2(0.08f, 0.255f),
            new Vector2(0.92f, 0.36f),
            new Color(0.58f, 0.36f, 0.12f, 1f),
            IniciarDesafioDiarioDesdeMenu
        );

        // Botón 4: Anuncio Bonificado Voluntario (Política AdMob Rewarded Opt-In)
        CrearBotonMenuGrande(
            mainHomeView.transform,
            "BtnRewardedAdMenu",
            "🎬  CONSULTORÍA BONIFICADA (+2 ⚡ COMODINES)",
            "Mirar un anuncio de video voluntario para acreditar +2 Comodines",
            new Vector2(0.08f, 0.135f),
            new Vector2(0.92f, 0.24f),
            new Color(0.42f, 0.22f, 0.58f, 1f),
            () =>
            {
                if (AdMobManager.Instance != null)
                {
                    AdMobManager.Instance.MostrarAnuncioBonificado(2, ActualizarBadgeMenu);
                }
            }
        );

        // Botón 5: Guía Teórica / Conceptos de la Materia (separado con margen sobre el Banner inferior)
        CrearBotonUI(
            mainHomeView.transform,
            "BtnGlossary",
            "📚 Marco Teórico de Gestión de la Organización",
            new Vector2(0.08f, 0.035f),
            new Vector2(0.92f, 0.115f),
            new Color(0.20f, 0.25f, 0.34f, 1f),
            () => CambiarSubvistaMenu("glossary")
        );
    }

    private void ConstruirSubvistaSelectorNiveles(Transform parent)
    {
        levelSelectorView = CrearContenedorVista(parent, "LevelSelectorView");

        CrearTextoUI(
            levelSelectorView.transform,
            "SelectorHeader",
            "🗂️ Selector de Casos Organizacionales",
            new Vector2(0.06f, 0.89f),
            new Vector2(0.94f, 0.96f),
            24f, 42f,
            new Color(0.96f, 0.85f, 0.35f),
            FontStyles.Bold
        );

        CrearTextoUI(
            levelSelectorView.transform,
            "SelectorSub",
            "Tocá cualquier nivel para cargarlo en el acto durante la presentación:",
            new Vector2(0.06f, 0.84f),
            new Vector2(0.94f, 0.89f),
            14f, 23f,
            new Color(0.80f, 0.86f, 0.92f),
            FontStyles.Normal
        );

        Color[] coloresNiveles = new Color[]
        {
            new Color(0.15f, 0.44f, 0.32f, 1f),
            new Color(0.16f, 0.38f, 0.58f, 1f),
            new Color(0.48f, 0.32f, 0.14f, 1f),
            new Color(0.38f, 0.22f, 0.52f, 1f),
            new Color(0.14f, 0.42f, 0.48f, 1f)
        };

        float topStart = 0.82f;
        float cardHeight = 0.115f;
        float gap = 0.018f;

        for (int i = 0; i < nivelesCampana.Length; i++)
        {
            int idx = i;
            ConfiguracionNivelAcademico cfg = nivelesCampana[i];
            float yMax = topStart - (i * (cardHeight + gap));
            float yMin = yMax - cardHeight;

            CrearBotonMenuGrande(
                levelSelectorView.transform,
                $"BtnSelectLevel_{i + 1}",
                cfg.nombreCorto,
                $"{cfg.etiquetaConcepto}  •  {cfg.cantidadNodos} Roles",
                new Vector2(0.07f, yMin),
                new Vector2(0.93f, yMax),
                coloresNiveles[i % coloresNiveles.Length],
                () => IniciarNivelCampanaDesdeMenu(idx)
            );
        }

        // Botón Volver al Menú Principal (con margen respecto al Banner inferior)
        CrearBotonUI(
            levelSelectorView.transform,
            "BtnBackFromSelector",
            "⬅ Volver al Menú Principal",
            new Vector2(0.15f, 0.04f),
            new Vector2(0.85f, 0.12f),
            new Color(0.24f, 0.29f, 0.38f, 1f),
            () => CambiarSubvistaMenu("home")
        );
    }

    private void ConstruirSubvistaGlosario(Transform parent)
    {
        glossaryView = CrearContenedorVista(parent, "GlossaryView");

        CrearTextoUI(
            glossaryView.transform,
            "GlossaryHeader",
            "📚 Marco Teórico Aplicado en OrgRise",
            new Vector2(0.06f, 0.89f),
            new Vector2(0.94f, 0.96f),
            24f, 40f,
            new Color(0.96f, 0.85f, 0.35f),
            FontStyles.Bold
        );

        GameObject card = CrearPanelTarjeta(
            glossaryView.transform,
            "GlossaryBodyCard",
            new Vector2(0.06f, 0.15f),
            new Vector2(0.94f, 0.87f),
            new Color(0.10f, 0.14f, 0.21f, 1f)
        );

        string textoTeoria =
            "<b>1. Cadena de Mando y Jerarquía:</b>\n" +
            "Línea continua de autoridad que se extiende desde la cima estratégica (CEO) hasta el núcleo operativo (Analistas/Operadores), respetando el conducto regular.\n\n" +
            "<b>2. Unidad de Mando (Fayol):</b>\n" +
            "Cada subordinado reporta a un único jefe directo para evitar conflictos de autoridad e instrucciones contradictorias.\n\n" +
            "<b>3. Tramo de Control (Span of Control):</b>\n" +
            "Cantidad máxima de subordinados que un administrador puede supervisar con eficacia (indicado como <b>Tramo: X/Max</b> en cada nodo).\n\n" +
            "<b>4. Criterios de Departamentalización:</b>\n" +
            "• <b>Funcional:</b> Agrupamiento por especialidad (Finanzas, Comercial, Operaciones, Tecnología).\n" +
            "• <b>Por Producto:</b> Agrupamiento por línea de negocio o servicio (PyMEs, Consumo, Industria).\n" +
            "• <b>Geográfica:</b> Agrupamiento por territorio o zona regional (Norte, Centro, Sur).\n\n" +
            "<b>5. Centralización vs. Descentralización:</b>\n" +
            "Las estructuras <b>centralizadas (chatas)</b> concentran decisiones arriba con tramos amplios; las <b>descentralizadas (altas)</b> delegan autoridad táctica en mandos intermedios.";

        TextMeshProUGUI txt = CrearTextoUI(
            card.transform,
            "GlossaryText",
            textoTeoria,
            new Vector2(0.05f, 0.04f),
            new Vector2(0.95f, 0.96f),
            13f, 24f,
            new Color(0.92f, 0.95f, 0.98f),
            FontStyles.Normal
        );
        txt.alignment = TextAlignmentOptions.MidlineLeft;

        CrearBotonUI(
            glossaryView.transform,
            "BtnBackFromGlossary",
            "⬅ Volver al Menú Principal",
            new Vector2(0.15f, 0.04f),
            new Vector2(0.85f, 0.12f),
            new Color(0.24f, 0.29f, 0.38f, 1f),
            () => CambiarSubvistaMenu("home")
        );
    }

    // ==================== HELPERS DE CONSTRUCCIÓN DE UI RESPONSIVA ====================

    private GameObject CrearContenedorVista(Transform parent, string nombre)
    {
        GameObject view = new GameObject(nombre, typeof(RectTransform));
        view.transform.SetParent(parent, false);
        RectTransform rt = view.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return view;
    }

    private GameObject CrearPanelTarjeta(Transform parent, string nombre, Vector2 anchorMin, Vector2 anchorMax, Color colorFondo)
    {
        GameObject card = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(parent, false);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = card.GetComponent<Image>();
        img.color = colorFondo;
        return card;
    }

    private TextMeshProUGUI CrearTextoUI(
        Transform parent,
        string nombre,
        string contenido,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float fontMin,
        float fontMax,
        Color colorTexto,
        FontStyles estilo)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = contenido;
        tmp.color = colorTexto;
        tmp.fontStyle = estilo;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = fontMin;
        tmp.fontSizeMax = fontMax;
        tmp.margin = Vector4.zero;
        tmp.raycastTarget = false;
        return tmp;
    }

    private Button CrearBotonUI(
        Transform parent,
        string nombre,
        string etiqueta,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color colorFondo,
        UnityEngine.Events.UnityAction accion)
    {
        GameObject btnObj = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = btnObj.GetComponent<Image>();
        img.color = colorFondo;

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(accion);

        CrearTextoUI(
            btnObj.transform,
            "Label",
            etiqueta,
            new Vector2(0.04f, 0.1f),
            new Vector2(0.96f, 0.9f),
            13f, 24f,
            Color.white,
            FontStyles.Bold
        );

        return btn;
    }

    private Button CrearBotonMenuGrande(
        Transform parent,
        string nombre,
        string titulo,
        string subtitulo,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color colorFondo,
        UnityEngine.Events.UnityAction accion)
    {
        GameObject btnObj = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = btnObj.GetComponent<Image>();
        img.color = colorFondo;

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(accion);

        CrearTextoUI(
            btnObj.transform,
            "Title",
            titulo,
            new Vector2(0.04f, 0.45f),
            new Vector2(0.96f, 0.92f),
            15f, 26f,
            Color.white,
            FontStyles.Bold
        );

        CrearTextoUI(
            btnObj.transform,
            "Subtitle",
            subtitulo,
            new Vector2(0.04f, 0.08f),
            new Vector2(0.96f, 0.45f),
            11f, 19f,
            new Color(0.90f, 0.94f, 0.98f),
            FontStyles.Normal
        );

        return btn;
    }

    private void ActualizarCabeceraUI(string titulo, string subtitulo)
    {
        if (headerTitleText != null) headerTitleText.text = titulo;
        if (headerSubText != null) headerSubText.text = subtitulo;
    }
}
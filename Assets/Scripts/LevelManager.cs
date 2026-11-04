using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public const string PrefsKeyNivelMaxDesbloqueado = "OrgRise_NivelMaxDesbloqueado";
    public const string PrefsKeyIndiceCampana = "OrgRise_IndiceNivelCampana";
    public const string PrefsKeyEstrellasNivel = "OrgRise_Nivel_{0}_Estrellas";

    public LevelData nivelActual;
    public List<EmployeeNode> nodosActivos = new List<EmployeeNode>();
    private EmployeeNode nodoRaiz;

    [Header("Estado Académico de la Partida")]
    public int indiceNivelCampana = 0;
    public int nivelMaxDesbloqueado = 0;
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

    // UI de Menú Principal, Selector de Nivel, Glosario y Ajustes
    private GameObject mainMenuPanel;
    private GameObject mainHomeView;
    private GameObject levelSelectorView;
    private GameObject glossaryView;
    private GameObject settingsView;
    private TextMeshProUGUI menuStreakBadgeText;

    // Sistema de Deshacer (Undo)
    public class AccionHistorial
    {
        public EmployeeNode subordinado;
        public EmployeeNode jefeAnterior;
        public EmployeeNode nuevoJefe;
        public bool esMatricial;
        public bool comodinConsumido;
        public bool esDesconexion;
    }
    private Stack<AccionHistorial> pilaHistorial = new Stack<AccionHistorial>();

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
        if (Instance == null)
        {
            Instance = this;
            CargarPersistenciaCampana();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CargarPersistenciaCampana()
    {
        indiceNivelCampana = PlayerPrefs.GetInt(PrefsKeyIndiceCampana, 0);
        nivelMaxDesbloqueado = PlayerPrefs.GetInt(PrefsKeyNivelMaxDesbloqueado, 0);
    }

    private void GuardarPersistenciaCampana()
    {
        PlayerPrefs.SetInt(PrefsKeyIndiceCampana, indiceNivelCampana);
        if (indiceNivelCampana > nivelMaxDesbloqueado)
        {
            nivelMaxDesbloqueado = indiceNivelCampana;
            PlayerPrefs.SetInt(PrefsKeyNivelMaxDesbloqueado, nivelMaxDesbloqueado);
        }
        PlayerPrefs.Save();
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
            rachaRt.anchorMax = new Vector2(0.30f, 0.985f);
            rachaRt.offsetMin = Vector2.zero;
            rachaRt.offsetMax = Vector2.zero;

            txtRacha.margin = Vector4.zero;
            txtRacha.alignment = TextAlignmentOptions.MidlineLeft;
            txtRacha.enableAutoSizing = true;
            txtRacha.fontSizeMin = 22f;
            txtRacha.fontSizeMax = 40f;
        }

        Canvas.ForceUpdateCanvases();
    }

    public void LimpiarNodosRegistrados()
    {
        nodosActivos.Clear();
        nodoRaiz = null;
        nivelYaCompletado = false;
        pilaHistorial.Clear();
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

    // ==================== SISTEMA DE DESHACER (UNDO) ====================

    public void RegistrarAccionConexion(EmployeeNode sub, EmployeeNode prevJefe, EmployeeNode nuevoJefe, bool esMatricial, bool comodinConsumido)
    {
        pilaHistorial.Push(new AccionHistorial
        {
            subordinado = sub,
            jefeAnterior = prevJefe,
            nuevoJefe = nuevoJefe,
            esMatricial = esMatricial,
            comodinConsumido = comodinConsumido,
            esDesconexion = false
        });
    }

    public void RegistrarAccionDesconexion(EmployeeNode sub, EmployeeNode prevJefe, bool comodinConsumido)
    {
        pilaHistorial.Push(new AccionHistorial
        {
            subordinado = sub,
            jefeAnterior = prevJefe,
            nuevoJefe = null,
            esMatricial = false,
            comodinConsumido = comodinConsumido,
            esDesconexion = true
        });
    }

    public void DeshacerUltimaAccion()
    {
        if (pilaHistorial.Count == 0)
        {
            if (FeedbackManager.Instance != null)
            {
                FeedbackManager.Instance.MostrarMensajeEstado("No hay acciones previas para deshacer.", new Color(0.24f, 0.28f, 0.38f, 0.95f), 1.8f);
            }
            return;
        }

        AccionHistorial accion = pilaHistorial.Pop();
        if (accion.subordinado == null) return;

        UINodeInteraction subUI = accion.subordinado.GetComponent<UINodeInteraction>();

        if (accion.esDesconexion)
        {
            if (accion.jefeAnterior != null)
            {
                accion.subordinado.manager = accion.jefeAnterior;
                if (!accion.jefeAnterior.directReports.Contains(accion.subordinado))
                    accion.jefeAnterior.directReports.Add(accion.subordinado);

                if (subUI != null) subUI.RecrearLinea(accion.jefeAnterior, false);
            }
        }
        else
        {
            if (accion.esMatricial)
            {
                accion.subordinado.RemoveMatrixProjectManager();
                if (subUI != null && subUI.currentMatrixLine != null)
                {
                    Destroy(subUI.currentMatrixLine);
                    subUI.currentMatrixLine = null;
                }
            }
            else
            {
                accion.subordinado.RemoveCurrentManager();
                if (subUI != null && subUI.currentManagerLine != null)
                {
                    Destroy(subUI.currentManagerLine);
                    subUI.currentManagerLine = null;
                }

                if (accion.jefeAnterior != null)
                {
                    accion.subordinado.manager = accion.jefeAnterior;
                    if (!accion.jefeAnterior.directReports.Contains(accion.subordinado))
                        accion.jefeAnterior.directReports.Add(accion.subordinado);

                    if (subUI != null) subUI.RecrearLinea(accion.jefeAnterior, false);
                }
            }
        }

        if (accion.comodinConsumido && WildcardManager.Instance != null)
        {
            WildcardManager.Instance.RecompensarComodin(1);
        }

        accion.subordinado.RefreshNodeVisuals();
        if (accion.jefeAnterior != null) accion.jefeAnterior.RefreshNodeVisuals();
        if (accion.nuevoJefe != null) accion.nuevoJefe.RefreshNodeVisuals();

        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.MostrarMensajeEstado("↶ Última acción deshecha.", new Color(0.18f, 0.42f, 0.62f, 0.95f), 1.8f);
        }
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
        if (settingsView != null) settingsView.SetActive(vista == "settings");
    }

    public void IniciarNivelCampanaDesdeMenu(int indice)
    {
        System.Action iniciarAccion = () =>
        {
            enModoDiario = false;
            indiceNivelCampana = Mathf.Clamp(indice, 0, nivelesCampana.Length - 1);
            GuardarPersistenciaCampana();
            OcultarMenuPrincipal();
            CargarNivelActual();

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
        pilaHistorial.Clear();

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
            else
            {
                GuardarPersistenciaCampana();
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

            ScorecardOrganizacional scorecard = OrgValidator.EvaluarEstructura(nodosActivos, nodoRaiz, modoAutoridadActual);

            if (!enModoDiario && indiceNivelCampana < nivelesCampana.Length)
            {
                string keyEstrellas = string.Format(PrefsKeyEstrellasNivel, indiceNivelCampana);
                int estrellasPrevias = PlayerPrefs.GetInt(keyEstrellas, 0);
                if (scorecard.estrellas > estrellasPrevias)
                {
                    PlayerPrefs.SetInt(keyEstrellas, scorecard.estrellas);
                }

                if (indiceNivelCampana + 1 > nivelMaxDesbloqueado)
                {
                    nivelMaxDesbloqueado = Mathf.Min(nivelesCampana.Length - 1, indiceNivelCampana + 1);
                    PlayerPrefs.SetInt(PrefsKeyNivelMaxDesbloqueado, nivelMaxDesbloqueado);
                }
                PlayerPrefs.Save();
            }

            EjecutarFeedbackVictoria(scorecard);
        }
    }

    private void EjecutarFeedbackVictoria(ScorecardOrganizacional scorecard)
    {
        if (FeedbackManager.Instance != null)
            FeedbackManager.Instance.PlayVictoryFeedback(nodosActivos);

        if (DailyStreakManager.Instance != null)
            DailyStreakManager.Instance.RegisterWin();

        if (StreakUI.Instance != null)
            StreakUI.Instance.ActualizarPantalla();

        StartCoroutine(MostrarModalVictoriaConRetraso(0.85f, scorecard));
    }

    private IEnumerator MostrarModalVictoriaConRetraso(float espera, ScorecardOrganizacional scorecard)
    {
        yield return new WaitForSeconds(espera);

        string estrellasIcono = new string('⭐', Mathf.Clamp(scorecard.estrellas, 1, 3));
        string resumenAcademico = (!enModoDiario && indiceNivelCampana < nivelesCampana.Length)
            ? nivelesCampana[indiceNivelCampana].resumenVictoria
            : $"<b>¡Desafío Diario #{numeroDesafioDiario} Completado!</b>\n\nResolviste correctamente el organigrama bajo departamentalización <b>{criterioActual}</b> y autoridad <b>{modoAutoridadActual}</b>.";

        string mensajeFinal = $"{resumenAcademico}\n\n" +
                              $"<size=120%><color=#FFD54F>{estrellasIcono}</color></size>\n" +
                              $"{scorecard.evaluacionPedagogica}\n\n" +
                              $"<size=82%><color=#CFD8DC>• Gasto Overhead: <b>{scorecard.costoEstructura} pts</b> | Distancia Jerárquica: <b>{scorecard.distanciaMediaJerarquica:F1} saltos</b></color></size>";

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.MostrarPantallaNivelCompletado(mensajeFinal);
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

        // Botón 1: Menú / Selector de Niveles
        CrearBotonUI(
            topButtonsContainer.transform,
            "BotonAbrirMenu",
            "☰ Menú",
            new Vector2(0.32f, 0.925f),
            new Vector2(0.53f, 0.98f),
            new Color(0.16f, 0.35f, 0.55f, 0.96f),
            MostrarMenuPrincipal
        );

        // Botón 2: Deshacer (Undo) último movimiento
        CrearBotonUI(
            topButtonsContainer.transform,
            "BotonDeshacer",
            "↶ Deshacer",
            new Vector2(0.55f, 0.925f),
            new Vector2(0.75f, 0.98f),
            new Color(0.48f, 0.32f, 0.14f, 0.96f),
            DeshacerUltimaAccion
        );

        // Botón 3: Reiniciar Nivel Actual
        CrearBotonUI(
            topButtonsContainer.transform,
            "BotonReiniciarNivel",
            "↻ Reiniciar",
            new Vector2(0.77f, 0.925f),
            new Vector2(0.97f, 0.98f),
            new Color(0.22f, 0.28f, 0.38f, 0.96f),
            ReiniciarNivelActual
        );
    }

    // ==================== MENÚ PRINCIPAL, SELECTOR, GLOSARIO Y AJUSTES ====================

    private void CrearMenuPrincipalUI()
    {
        if (mainMenuPanel != null) return;

        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null) return;

        mainMenuPanel = new GameObject("MainMenuOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        mainMenuPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform rootRt = mainMenuPanel.GetComponent<RectTransform>();
        rootRt.localScale = Vector3.one;
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
        ConstruirSubvistaAjustes(mainMenuPanel.transform);
    }

    private void ConstruirSubvistaHome(Transform parent)
    {
        mainHomeView = CrearContenedorVista(parent, "HomeView");

        CrearTextoUI(
            mainHomeView.transform,
            "AcademicTag",
            "TRABAJO FINAL INTEGRADOR • GESTIÓN DE LA ORGANIZACIÓN",
            new Vector2(0.06f, 0.91f),
            new Vector2(0.94f, 0.96f),
            13f, 21f,
            new Color(0.50f, 0.80f, 0.77f),
            FontStyles.Bold
        );

        CrearTextoUI(
            mainHomeView.transform,
            "GameTitle",
            "🏢 OrgRise",
            new Vector2(0.06f, 0.81f),
            new Vector2(0.94f, 0.91f),
            38f, 72f,
            Color.white,
            FontStyles.Bold
        );

        CrearTextoUI(
            mainHomeView.transform,
            "GameSubtitle",
            "Simulador y Puzzle de Diseño de Estructuras Organizacionales\n<size=75%><color=#B0BEC5>Cadena de Mando • Tramo de Control • Departamentalización • Staff</color></size>",
            new Vector2(0.07f, 0.72f),
            new Vector2(0.93f, 0.81f),
            14f, 25f,
            new Color(0.95f, 0.85f, 0.35f),
            FontStyles.Normal
        );

        GameObject streakCard = CrearPanelTarjeta(
            mainHomeView.transform,
            "StreakBadgeCard",
            new Vector2(0.08f, 0.645f),
            new Vector2(0.92f, 0.705f),
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

        // Botón Campaña Académica
        CrearBotonMenuGrande(
            mainHomeView.transform,
            "BtnPlayCampaign",
            "▶  MODO CAMPAÑA ACADÉMICA",
            $"Recorrido por los 5 modelos estructurales (Nivel {Mathf.Min(indiceNivelCampana + 1, nivelesCampana.Length)}/5)",
            new Vector2(0.08f, 0.525f),
            new Vector2(0.92f, 0.625f),
            new Color(0.15f, 0.52f, 0.34f, 1f),
            () => IniciarNivelCampanaDesdeMenu(indiceNivelCampana < nivelesCampana.Length ? indiceNivelCampana : 0)
        );

        // Botón Selector de Niveles
        CrearBotonMenuGrande(
            mainHomeView.transform,
            "BtnOpenLevelSelector",
            "🗂️  SELECTOR DE CASOS (5 NIVELES)",
            "Acceso directo con evaluación de estrellas para la defensa oral",
            new Vector2(0.08f, 0.415f),
            new Vector2(0.92f, 0.515f),
            new Color(0.16f, 0.38f, 0.62f, 1f),
            () => {
                ConstruirBotonesSelectorNivelesDinamicos();
                CambiarSubvistaMenu("selector");
            }
        );

        // Botón Desafío Diario
        CrearBotonMenuGrande(
            mainHomeView.transform,
            "BtnDailyChallenge",
            "📅  DESAFÍO DIARIO PROCEDURAL",
            "Generación algorítmica con semilla determinista y racha",
            new Vector2(0.08f, 0.305f),
            new Vector2(0.92f, 0.405f),
            new Color(0.58f, 0.36f, 0.12f, 1f),
            IniciarDesafioDiarioDesdeMenu
        );

        // Botón Consultoría Bonificada
        CrearBotonMenuGrande(
            mainHomeView.transform,
            "BtnRewardedAdMenu",
            "🎬  CONSULTORÍA BONIFICADA (+2 ⚡)",
            "Mirar un video voluntario para acreditar +2 Comodines",
            new Vector2(0.08f, 0.195f),
            new Vector2(0.92f, 0.295f),
            new Color(0.42f, 0.22f, 0.58f, 1f),
            () => {
                if (AdMobManager.Instance != null)
                    AdMobManager.Instance.MostrarAnuncioBonificado(2, ActualizarBadgeMenu);
            }
        );

        // Fila inferior: Glosario Teórico y Ajustes
        CrearBotonUI(
            mainHomeView.transform,
            "BtnGlossary",
            "📚 Marco Teórico",
            new Vector2(0.08f, 0.085f),
            new Vector2(0.50f, 0.165f),
            new Color(0.20f, 0.25f, 0.34f, 1f),
            () => CambiarSubvistaMenu("glossary")
        );

        CrearBotonUI(
            mainHomeView.transform,
            "BtnSettings",
            "⚙ Ajustes",
            new Vector2(0.53f, 0.085f),
            new Vector2(0.92f, 0.165f),
            new Color(0.22f, 0.28f, 0.36f, 1f),
            () => CambiarSubvistaMenu("settings")
        );
    }

    private GameObject levelButtonsContainer;

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
            "Tocá cualquier nivel desbloqueado para cargarlo en el acto:",
            new Vector2(0.06f, 0.84f),
            new Vector2(0.94f, 0.89f),
            14f, 23f,
            new Color(0.80f, 0.86f, 0.92f),
            FontStyles.Normal
        );

        levelButtonsContainer = new GameObject("ButtonsContainer", typeof(RectTransform));
        levelButtonsContainer.transform.SetParent(levelSelectorView.transform, false);
        RectTransform rt = levelButtonsContainer.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.14f);
        rt.anchorMax = new Vector2(1f, 0.83f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        ConstruirBotonesSelectorNivelesDinamicos();

        CrearBotonUI(
            levelSelectorView.transform,
            "BtnBackFromSelector",
            "⬅ Volver al Menú Principal",
            new Vector2(0.15f, 0.035f),
            new Vector2(0.85f, 0.115f),
            new Color(0.24f, 0.29f, 0.38f, 1f),
            () => CambiarSubvistaMenu("home")
        );
    }

    private void ConstruirBotonesSelectorNivelesDinamicos()
    {
        if (levelButtonsContainer == null) return;

        for (int c = levelButtonsContainer.transform.childCount - 1; c >= 0; c--)
        {
            Destroy(levelButtonsContainer.transform.GetChild(c).gameObject);
        }

        Color[] coloresNiveles = new Color[]
        {
            new Color(0.15f, 0.44f, 0.32f, 1f),
            new Color(0.16f, 0.38f, 0.58f, 1f),
            new Color(0.48f, 0.32f, 0.14f, 1f),
            new Color(0.38f, 0.22f, 0.52f, 1f),
            new Color(0.14f, 0.42f, 0.48f, 1f)
        };

        float topStart = 0.96f;
        float cardHeight = 0.165f;
        float gap = 0.025f;

        int maxDesbloqueado = PlayerPrefs.GetInt(PrefsKeyNivelMaxDesbloqueado, 0);

        for (int i = 0; i < nivelesCampana.Length; i++)
        {
            int idx = i;
            ConfiguracionNivelAcademico cfg = nivelesCampana[i];
            float yMax = topStart - (i * (cardHeight + gap));
            float yMin = yMax - cardHeight;

            int estrellas = PlayerPrefs.GetInt(string.Format(PrefsKeyEstrellasNivel, idx), 0);
            string estrellasTxt = estrellas > 0 ? new string('⭐', estrellas) : "▫️▫️▫️";

            bool desbloqueado = idx <= maxDesbloqueado;
            Color btnColor = desbloqueado ? coloresNiveles[i % coloresNiveles.Length] : new Color(0.18f, 0.20f, 0.25f, 0.9f);
            string prefijoEstado = desbloqueado ? "" : "🔒 ";

            CrearBotonMenuGrande(
                levelButtonsContainer.transform,
                $"BtnSelectLevel_{i + 1}",
                $"{prefijoEstado}{cfg.nombreCorto}",
                $"{cfg.etiquetaConcepto}  |  {estrellasTxt}",
                new Vector2(0.07f, yMin),
                new Vector2(0.93f, yMax),
                btnColor,
                () => IniciarNivelCampanaDesdeMenu(idx)
            );
        }
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
            new Vector2(0.06f, 0.14f),
            new Vector2(0.94f, 0.87f),
            new Color(0.10f, 0.14f, 0.21f, 1f)
        );

        string textoTeoria =
            "<b>1. Cadena de Mando y Jerarquía (Fayol):</b>\n" +
            "Línea ininterrumpida de autoridad formal que vincula a todos los miembros desde la cúpula estratégica (CEO) hasta el núcleo operativo.\n\n" +
            "<b>2. Unidad de Mando vs. Estructura Matricial:</b>\n" +
            "Fayol estipula un único superior directo. La estructura <b>Matricial</b> flexibiliza esta regla, habilitando el doble reporte (Gerente Funcional + Gerente de Proyecto).\n\n" +
            "<b>3. Tramo de Control (Span of Control):</b>\n" +
            "Límite de reportes directos supervisables eficazmente (marcado como <b>Tramo: X/Max</b>). Superarlo satura al mando y crea cuellos de botella.\n\n" +
            "<b>4. Departamentalización:</b>\n" +
            "• <b>Funcional:</b> Agrupamiento por áreas de especialidad técnica.\n" +
            "• <b>Por Producto:</b> Divisiones autónomas orientadas al mercado.\n" +
            "• <b>Geográfica:</b> Agrupamiento por cobertura territorial.\n\n" +
            "<b>5. Órganos de Staff y Asesoría (Mintzberg):</b>\n" +
            "Puestos de tecnoestructura o consultoría que asisten a la Alta Dirección sin ejercer autoridad de línea sobre los subordinados.";

        TextMeshProUGUI txt = CrearTextoUI(
            card.transform,
            "GlossaryText",
            textoTeoria,
            new Vector2(0.05f, 0.04f),
            new Vector2(0.95f, 0.96f),
            12f, 22f,
            new Color(0.92f, 0.95f, 0.98f),
            FontStyles.Normal
        );
        txt.alignment = TextAlignmentOptions.MidlineLeft;

        CrearBotonUI(
            glossaryView.transform,
            "BtnBackFromGlossary",
            "⬅ Volver al Menú Principal",
            new Vector2(0.15f, 0.035f),
            new Vector2(0.85f, 0.115f),
            new Color(0.24f, 0.29f, 0.38f, 1f),
            () => CambiarSubvistaMenu("home")
        );
    }

    private void ConstruirSubvistaAjustes(Transform parent)
    {
        settingsView = CrearContenedorVista(parent, "SettingsView");

        CrearTextoUI(
            settingsView.transform,
            "SettingsHeader",
            "⚙ Ajustes de la Aplicación",
            new Vector2(0.06f, 0.89f),
            new Vector2(0.94f, 0.96f),
            24f, 40f,
            new Color(0.96f, 0.85f, 0.35f),
            FontStyles.Bold
        );

        GameObject card = CrearPanelTarjeta(
            settingsView.transform,
            "SettingsBodyCard",
            new Vector2(0.08f, 0.20f),
            new Vector2(0.92f, 0.86f),
            new Color(0.10f, 0.14f, 0.21f, 1f)
        );

        // Toggle Sonido SFX
        Button btnSFX = CrearBotonUI(
            card.transform,
            "BtnToggleSFX",
            FeedbackManager.Instance != null && FeedbackManager.Instance.IsSFXEnabled() ? "Efectos de Sonido (SFX): ACTIVO" : "Efectos de Sonido (SFX): APAGADO",
            new Vector2(0.06f, 0.76f),
            new Vector2(0.94f, 0.90f),
            new Color(0.18f, 0.36f, 0.54f, 1f),
            null
        );
        btnSFX.onClick.AddListener(() => {
            if (FeedbackManager.Instance != null) {
                bool nuevo = !FeedbackManager.Instance.IsSFXEnabled();
                FeedbackManager.Instance.SetSFXEnabled(nuevo);
                btnSFX.GetComponentInChildren<TextMeshProUGUI>().text = nuevo ? "Efectos de Sonido (SFX): ACTIVO" : "Efectos de Sonido (SFX): APAGADO";
            }
        });

        // Toggle Música BGM
        Button btnBGM = CrearBotonUI(
            card.transform,
            "BtnToggleBGM",
            FeedbackManager.Instance != null && FeedbackManager.Instance.IsBGMEnabled() ? "Música Ambiental (BGM): ACTIVA" : "Música Ambiental (BGM): APAGADA",
            new Vector2(0.06f, 0.58f),
            new Vector2(0.94f, 0.72f),
            new Color(0.24f, 0.40f, 0.32f, 1f),
            null
        );
        btnBGM.onClick.AddListener(() => {
            if (FeedbackManager.Instance != null) {
                bool nuevo = !FeedbackManager.Instance.IsBGMEnabled();
                FeedbackManager.Instance.SetBGMEnabled(nuevo);
                btnBGM.GetComponentInChildren<TextMeshProUGUI>().text = nuevo ? "Música Ambiental (BGM): ACTIVA" : "Música Ambiental (BGM): APAGADA";
            }
        });

        // Toggle Vibración Háptica
        Button btnHaptics = CrearBotonUI(
            card.transform,
            "BtnToggleHaptics",
            FeedbackManager.Instance != null && FeedbackManager.Instance.IsHapticsEnabled() ? "Vibración Háptica: ACTIVA" : "Vibración Háptica: APAGADA",
            new Vector2(0.06f, 0.40f),
            new Vector2(0.94f, 0.54f),
            new Color(0.48f, 0.32f, 0.16f, 1f),
            null
        );
        btnHaptics.onClick.AddListener(() => {
            if (FeedbackManager.Instance != null) {
                bool nuevo = !FeedbackManager.Instance.IsHapticsEnabled();
                FeedbackManager.Instance.SetHapticsEnabled(nuevo);
                btnHaptics.GetComponentInChildren<TextMeshProUGUI>().text = nuevo ? "Vibración Háptica: ACTIVA" : "Vibración Háptica: APAGADA";
            }
        });

        // Botón Restablecer Progreso
        CrearBotonUI(
            card.transform,
            "BtnResetProgress",
            "⚠️ Restablecer Progreso y Racha",
            new Vector2(0.06f, 0.10f),
            new Vector2(0.94f, 0.24f),
            new Color(0.68f, 0.16f, 0.16f, 1f),
            () => {
                PlayerPrefs.DeleteKey(PrefsKeyNivelMaxDesbloqueado);
                PlayerPrefs.DeleteKey(PrefsKeyIndiceCampana);
                for (int k = 0; k < 10; k++) PlayerPrefs.DeleteKey(string.Format(PrefsKeyEstrellasNivel, k));
                if (WildcardManager.Instance != null) WildcardManager.Instance.RestablecerComodinesPorDefecto();
                CargarPersistenciaCampana();
                if (FeedbackManager.Instance != null) FeedbackManager.Instance.MostrarMensajeEstado("Progreso restablecido correctamente.", new Color(0.5f, 0.15f, 0.15f, 0.95f), 2.5f);
                ActualizarBadgeMenu();
            }
        );

        CrearBotonUI(
            settingsView.transform,
            "BtnBackFromSettings",
            "⬅ Volver al Menú Principal",
            new Vector2(0.15f, 0.035f),
            new Vector2(0.85f, 0.115f),
            new Color(0.24f, 0.29f, 0.38f, 1f),
            () => CambiarSubvistaMenu("home")
        );
    }

    // ==================== HELPERS DE CONSTRUCCIÓN DE UI ====================

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
        if (accion != null) btn.onClick.AddListener(accion);

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
        if (accion != null) btn.onClick.AddListener(accion);

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
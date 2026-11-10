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
    public const string PrefsKeyXPCarrera = "OrgRise_XP_Carrera";

    public LevelData nivelActual;
    public List<EmployeeNode> nodosActivos = new List<EmployeeNode>();
    private EmployeeNode nodoRaiz;

    [Header("Estado Comercial de la Partida")]
    public int indiceNivelCampana = 0;
    public int nivelMaxDesbloqueado = 0;
    public int xpCarrera = 0;
    public CriterioDepartamentalizacion criterioActual = CriterioDepartamentalizacion.Funcional;
    public ModoAutoridad modoAutoridadActual = ModoAutoridad.Equilibrada;
    public bool ExigirCadenaEstricta = true;

    private bool nivelYaCompletado = false;
    private bool enModoDiario = false;
    private int numeroDesafioDiario = 1;
    private int erroresEnNivelActual = 0;
    private float tiempoInicioNivel = 0f;
    private int pestanaEtapaSelector = 0; // 0 = Niveles 1-5, 1 = Niveles 6-10, 2 = Niveles 11-15

    // UI de Cabecera y Navegación
    private GameObject headerPanelObj;
    private TextMeshProUGUI headerTitleText;
    private TextMeshProUGUI headerSubText;
    private GameObject topButtonsContainer;

    // UI de Menú Principal, Selector de Nivel, Carrera/Logros y Ajustes
    private GameObject mainMenuPanel;
    private GameObject mainHomeView;
    private GameObject levelSelectorView;
    private GameObject careerView;
    private GameObject settingsView;
    private TextMeshProUGUI menuStreakBadgeText;
    private TextMeshProUGUI careerRankSummaryText;
    private Button botonRescatarRachaMenu;

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

    public struct ConfiguracionNivelComercial
    {
        public string nombreCorto;
        public string etiquetaModo;
        public string titulo;
        public string subtitulo;
        public string resumenVictoria;
        public int cantidadNodos;
        public CriterioDepartamentalizacion criterio;
        public ModoAutoridad autoridad;
        public bool incluirStaff;
        public bool preconectarCrisis;
        public int semillaBase;
    }

    // 15 Niveles Comerciales Progresivos divididos en 3 Etapas Corporativas
    public readonly ConfiguracionNivelComercial[] nivelesCampana = new ConfiguracionNivelComercial[]
    {
        // ==================== ETAPA 1: STARTUP SEED (Niveles 1 a 5) ====================
        new ConfiguracionNivelComercial
        {
            nombreCorto = "01. Garage Fintech",
            etiquetaModo = "Startup • Estructura Funcional",
            titulo = "Nivel 1: Garage Fintech",
            subtitulo = "Conectá cada especialista con el líder de su misma división",
            resumenVictoria = "<b>¡Ronda Semilla Completada!</b>\nOrganizaste los primeros equipos de tu Fintech sin saturar a ningún líder.",
            cantidadNodos = 7,
            criterio = CriterioDepartamentalizacion.Funcional,
            autoridad = ModoAutoridad.Equilibrada,
            incluirStaff = false,
            preconectarCrisis = false,
            semillaBase = 10101
        },
        new ConfiguracionNivelComercial
        {
            nombreCorto = "02. Lanzamiento SaaS",
            etiquetaModo = "Producto • Unidades de Negocio",
            titulo = "Nivel 2: Lanzamiento SaaS",
            subtitulo = "Agrupá los puestos por línea de producto (Fintech, Retail, Cloud)",
            resumenVictoria = "<b>¡Lanzamiento Exitoso!</b>\nCada unidad de producto opera con su propio VP y flujo ejecutivo dedicado.",
            cantidadNodos = 8,
            criterio = CriterioDepartamentalizacion.PorProducto,
            autoridad = ModoAutoridad.Equilibrada,
            incluirStaff = false,
            preconectarCrisis = false,
            semillaBase = 20202
        },
        new ConfiguracionNivelComercial
        {
            nombreCorto = "03. Escala E-Commerce",
            etiquetaModo = "Ágil • Supervisión Directa",
            titulo = "Nivel 3: Escala E-Commerce",
            subtitulo = "Estructura Horizontal: conectá los equipos con la cúpula ágil",
            resumenVictoria = "<b>¡Crecimiento Acelerado!</b>\nCon una estructura horizontal ágil, la dirección toma decisiones en tiempo récord.",
            cantidadNodos = 8,
            criterio = CriterioDepartamentalizacion.Funcional,
            autoridad = ModoAutoridad.Centralizada,
            incluirStaff = false,
            preconectarCrisis = false,
            semillaBase = 30303
        },
        new ConfiguracionNivelComercial
        {
            nombreCorto = "04. Despliegue Regional",
            etiquetaModo = "Regiones • Cobertura Territorial",
            titulo = "Nivel 4: Despliegue Regional",
            subtitulo = "Organizá las operaciones por zonas (Norte, Centro y Sur)",
            resumenVictoria = "<b>¡Expansión Nacional!</b>\nCada región cuenta con su cadena de mando lista para escalar ventas.",
            cantidadNodos = 9,
            criterio = CriterioDepartamentalizacion.Geografica,
            autoridad = ModoAutoridad.Equilibrada,
            incluirStaff = false,
            preconectarCrisis = false,
            semillaBase = 40404
        },
        new ConfiguracionNivelComercial
        {
            nombreCorto = "05. Auditoría & Staff VIP",
            etiquetaModo = "Staff VIP • Asesoría al CEO",
            titulo = "Nivel 5: Auditoría & Staff VIP",
            subtitulo = "Asigná el puesto ⚖️ Staff VIP directo al CEO o VP y armá las divisiones",
            resumenVictoria = "<b>¡Serie A Asegurada!</b>\nIntegraste la asesoría legal/VIP en la cúpula manteniendo el control táctico.",
            cantidadNodos = 10,
            criterio = CriterioDepartamentalizacion.Funcional,
            autoridad = ModoAutoridad.Descentralizada,
            incluirStaff = true,
            preconectarCrisis = false,
            semillaBase = 50505
        },

        // ==================== ETAPA 2: SCALEUP & SQUADS (Niveles 6 a 10) ====================
        new ConfiguracionNivelComercial
        {
            nombreCorto = "06. Squads Matriciales",
            etiquetaModo = "Matricial • Doble Mando",
            titulo = "Nivel 6: Squads Matriciales",
            subtitulo = "Doble Mando: cada especialista puede reportar a 2 Managers distintos",
            resumenVictoria = "<b>¡Sinergia Matricial!</b>\nDominaste el sistema de Doble Mando conectando especialistas con líderes funcionales y de squad.",
            cantidadNodos = 9,
            criterio = CriterioDepartamentalizacion.Matricial,
            autoridad = ModoAutoridad.Equilibrada,
            incluirStaff = false,
            preconectarCrisis = false,
            semillaBase = 60606
        },
        new ConfiguracionNivelComercial
        {
            nombreCorto = "07. Crisis en Logística",
            etiquetaModo = "Crisis • Reestructuración Activa",
            titulo = "Nivel 7: Crisis en Logística",
            subtitulo = "Tablero en operación: completá y reestructurá las ramas con precisión",
            resumenVictoria = "<b>¡Crisis Superada!</b>\nReordenaste las líneas de mando activas sin frenar la distribución.",
            cantidadNodos = 10,
            criterio = CriterioDepartamentalizacion.PorProducto,
            autoridad = ModoAutoridad.Equilibrada,
            incluirStaff = false,
            preconectarCrisis = true,
            semillaBase = 70707
        },
        new ConfiguracionNivelComercial
        {
            nombreCorto = "08. Holding Multirregional",
            etiquetaModo = "Cupo Estrecho • Delegación",
            titulo = "Nivel 8: Holding Multirregional",
            subtitulo = "Cupos al límite: delegá cada puesto sin desperdiciar capacidad",
            resumenVictoria = "<b>¡Holding Consolidado!</b>\nCalibraste los cupos de supervisión al 100% de eficiencia en todas las sedes.",
            cantidadNodos = 10,
            criterio = CriterioDepartamentalizacion.Geografica,
            autoridad = ModoAutoridad.Descentralizada,
            incluirStaff = false,
            preconectarCrisis = false,
            semillaBase = 80808
        },
        new ConfiguracionNivelComercial
        {
            nombreCorto = "09. Fusión Corporativa M&A",
            etiquetaModo = "Staff VIP + Crisis",
            titulo = "Nivel 9: Fusión Corporativa M&A",
            subtitulo = "Integrá el ⚖️ M&A Advisor en la cúpula y ordená las divisiones fusionadas",
            resumenVictoria = "<b>¡Fusión Exitosa!</b>\nUnificaste ambas compañías bajo un mismo directorio con asesoría M&A directa.",
            cantidadNodos = 11,
            criterio = CriterioDepartamentalizacion.PorProducto,
            autoridad = ModoAutoridad.Descentralizada,
            incluirStaff = true,
            preconectarCrisis = true,
            semillaBase = 90909
        },
        new ConfiguracionNivelComercial
        {
            nombreCorto = "10. Unicornio Tech",
            etiquetaModo = "Matricial + Staff VIP",
            titulo = "Nivel 10: Unicornio Tech ($1B)",
            subtitulo = "Estructura de alto rendimiento con Squads Matriciales y Staff VIP",
            resumenVictoria = "<b>¡Valuación de $1.000M Alcanzada!</b>\nTu arquitectura organizacional llevó a la compañía al estatus de Unicornio.",
            cantidadNodos = 11,
            criterio = CriterioDepartamentalizacion.Matricial,
            autoridad = ModoAutoridad.Equilibrada,
            incluirStaff = true,
            preconectarCrisis = false,
            semillaBase = 101101
        },

        // ==================== ETAPA 3: IMPERIO GLOBAL (Niveles 11 a 15) ====================
        new ConfiguracionNivelComercial
        {
            nombreCorto = "11. Red Global 4 Regiones",
            etiquetaModo = "Global • Máxima Escala",
            titulo = "Nivel 11: Red Global 4 Regiones",
            subtitulo = "Coordiná las sedes continentales con precisión quirúrgica",
            resumenVictoria = "<b>¡Operación Intercontinental!</b>\nTodas las sedes regionales reportan en tiempo real a la casa matriz.",
            cantidadNodos = 11,
            criterio = CriterioDepartamentalizacion.Geografica,
            autoridad = ModoAutoridad.Descentralizada,
            incluirStaff = true,
            preconectarCrisis = false,
            semillaBase = 111211
        },
        new ConfiguracionNivelComercial
        {
            nombreCorto = "12. Rescate Bancario",
            etiquetaModo = "Crisis Extrema • Cupo Estricto",
            titulo = "Nivel 12: Rescate Bancario",
            subtitulo = "Reestructuración financiera bajo cupo estricto y ramas pre-conectadas",
            resumenVictoria = "<b>¡Solvencia Restaurada!</b>\nEliminaste los cuellos de botella del banco en tiempo récord.",
            cantidadNodos = 11,
            criterio = CriterioDepartamentalizacion.Funcional,
            autoridad = ModoAutoridad.Descentralizada,
            incluirStaff = false,
            preconectarCrisis = true,
            semillaBase = 121312
        },
        new ConfiguracionNivelComercial
        {
            nombreCorto = "13. Consorcio Aeroespacial",
            etiquetaModo = "Matricial Avanzado",
            titulo = "Nivel 13: Consorcio Aeroespacial",
            subtitulo = "Conectá la ingeniería de misión con doble reporte de proyecto",
            resumenVictoria = "<b>¡Despegue Autorizado!</b>\nLa matriz de ingeniería y misión quedó sincronizada sin fallos.",
            cantidadNodos = 12,
            criterio = CriterioDepartamentalizacion.Matricial,
            autoridad = ModoAutoridad.Descentralizada,
            incluirStaff = false,
            preconectarCrisis = true,
            semillaBase = 131413
        },
        new ConfiguracionNivelComercial
        {
            nombreCorto = "14. Toma de Control Hostil",
            etiquetaModo = "Crisis + Staff VIP",
            titulo = "Nivel 14: Toma de Control Hostil",
            subtitulo = "Blindá el directorio con ⚖️ Legal Counsel y reorganizá las 4 divisiones",
            resumenVictoria = "<b>¡Defensa Corporativa Triunfal!</b>\nReorganizaste el holding bajo presión y aseguraste el control accionario.",
            cantidadNodos = 12,
            criterio = CriterioDepartamentalizacion.PorProducto,
            autoridad = ModoAutoridad.Descentralizada,
            incluirStaff = true,
            preconectarCrisis = true,
            semillaBase = 141514
        },
        new ConfiguracionNivelComercial
        {
            nombreCorto = "15. Cúpula Fortune 500",
            etiquetaModo = "Gran Final • Imperio Global",
            titulo = "Nivel 15: Cúpula Fortune 500",
            subtitulo = "El desafío definitivo: Matricial + Staff VIP + Cupo Ejecutivo",
            resumenVictoria = "<b>👑 ¡LEYENDA DE WALL STREET!</b>\nCompletaste los 15 escenarios corporativos y llevaste tu imperio a la cima mundial.",
            cantidadNodos = 12,
            criterio = CriterioDepartamentalizacion.Matricial,
            autoridad = ModoAutoridad.Descentralizada,
            incluirStaff = true,
            preconectarCrisis = true,
            semillaBase = 151615
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
        xpCarrera = PlayerPrefs.GetInt(PrefsKeyXPCarrera, 0);
    }

    private void GuardarPersistenciaCampana()
    {
        PlayerPrefs.SetInt(PrefsKeyIndiceCampana, indiceNivelCampana);
        if (indiceNivelCampana > nivelMaxDesbloqueado)
        {
            nivelMaxDesbloqueado = indiceNivelCampana;
            PlayerPrefs.SetInt(PrefsKeyNivelMaxDesbloqueado, nivelMaxDesbloqueado);
        }
        PlayerPrefs.SetInt(PrefsKeyXPCarrera, xpCarrera);
        PlayerPrefs.Save();
    }

    public int ObtenerTotalEstrellasCampana()
    {
        int total = 0;
        for (int i = 0; i < nivelesCampana.Length; i++)
        {
            total += PlayerPrefs.GetInt(string.Format(PrefsKeyEstrellasNivel, i), 0);
        }
        return total;
    }

    public string ObtenerTituloRangoCorporativo()
    {
        int estrellas = ObtenerTotalEstrellasCampana();
        if (estrellas >= 40) return "🌍 Leyenda de Wall Street";
        if (estrellas >= 30) return "👑 Magnate Corporativo";
        if (estrellas >= 20) return "🏛️ VP Ejecutivo";
        if (estrellas >= 12) return "🚀 Director de Expansión";
        if (estrellas >= 5) return "💼 Manager de Operaciones";
        return "🌱 Fundador Startup";
    }

    public void RegistrarErrorMovimiento()
    {
        erroresEnNivelActual++;
    }

    public void MejorarNivelActualATresEstrellas()
    {
        if (!enModoDiario && indiceNivelCampana < nivelesCampana.Length)
        {
            string keyEstrellas = string.Format(PrefsKeyEstrellasNivel, indiceNivelCampana);
            int previas = PlayerPrefs.GetInt(keyEstrellas, 0);
            if (previas < 3)
            {
                PlayerPrefs.SetInt(keyEstrellas, 3);
                xpCarrera += 75;
                PlayerPrefs.SetInt(PrefsKeyXPCarrera, xpCarrera);
                PlayerPrefs.Save();
            }
            ActualizarBadgeMenu();
        }
    }

    /// <summary>
    /// Conecta automáticamente un nodo pendiente con un líder válido (recompensa del botón 🎯 Auto-Pista / Headhunter).
    /// </summary>
    public void AplicarAutoPistaHeadhunter()
    {
        if (nivelYaCompletado || nodosActivos.Count == 0) return;

        // Buscar primero un empleado o mando desconectado (de abajo hacia arriba)
        EmployeeNode candidatoSub = null;
        EmployeeNode candidatoJefe = null;

        // Ordenar búsqueda: primero niveles superiores desconectados (Directores -> Gerentes -> Empleados) para armar el tronco
        NivelJerarquico[] ordenBusqueda = { NivelJerarquico.Director, NivelJerarquico.Gerente, NivelJerarquico.EmpleadoBase };
        foreach (var nv in ordenBusqueda)
        {
            foreach (var n in nodosActivos)
            {
                if (n == null || n.isRootNode || n.nivel != nv) continue;

                bool necesitaJefePrincipal = (n.manager == null);
                bool necesitaJefeMatricial = (criterioActual == CriterioDepartamentalizacion.Matricial && n.nivel == NivelJerarquico.EmpleadoBase && n.manager != null && n.matrixProjectManager == null);

                if (necesitaJefePrincipal || necesitaJefeMatricial)
                {
                    foreach (var posibleJefe in nodosActivos)
                    {
                        if (posibleJefe == null || posibleJefe == n) continue;
                        if (n.CanAssignManagerPreview(posibleJefe, out _, out _))
                        {
                            candidatoSub = n;
                            candidatoJefe = posibleJefe;
                            break;
                        }
                    }
                }
                if (candidatoSub != null) break;
            }
            if (candidatoSub != null) break;
        }

        if (candidatoSub != null && candidatoJefe != null)
        {
            UINodeInteraction.ConectarDosNodos(candidatoSub, candidatoJefe, silencioso: false);
            if (FeedbackManager.Instance != null)
            {
                FeedbackManager.Instance.PlayHintFeedback(
                    candidatoSub.transform,
                    $"🎯 Headhunter asignó: {candidatoSub.roleName} → {candidatoJefe.roleName}"
                );
            }
        }
        else
        {
            if (FeedbackManager.Instance != null)
            {
                FeedbackManager.Instance.MostrarMensajeEstado(
                    "💡 Todos los cupos actuales están ocupados. Desconectá un puesto sobrecargado o usá ⚡ Reestructurar.",
                    new Color(0.55f, 0.36f, 0.08f, 0.96f),
                    3.0f
                );
            }
        }
    }

    public void IniciarJuego()
    {
        NormalizarLayoutVerticalMovil();
        CrearCabeceraComercialUI();
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
            spawnRt.anchorMin = new Vector2(0.03f, 0.205f);
            spawnRt.anchorMax = new Vector2(0.97f, 0.82f);
            spawnRt.offsetMin = Vector2.zero;
            spawnRt.offsetMax = Vector2.zero;
        }

        if (StreakUI.Instance != null && StreakUI.Instance.textoRacha != null)
        {
            TextMeshProUGUI txtRacha = StreakUI.Instance.textoRacha;
            RectTransform rachaRt = txtRacha.GetComponent<RectTransform>();
            rachaRt.localScale = Vector3.one;
            rachaRt.anchorMin = new Vector2(0.035f, 0.925f);
            rachaRt.anchorMax = new Vector2(0.29f, 0.985f);
            rachaRt.offsetMin = Vector2.zero;
            rachaRt.offsetMax = Vector2.zero;

            txtRacha.margin = Vector4.zero;
            txtRacha.alignment = TextAlignmentOptions.MidlineLeft;
            txtRacha.enableAutoSizing = true;
            txtRacha.fontSizeMin = 20f;
            txtRacha.fontSizeMax = 38f;
        }

        Canvas.ForceUpdateCanvases();
    }

    public void LimpiarNodosRegistrados()
    {
        nodosActivos.Clear();
        nodoRaiz = null;
        nivelYaCompletado = false;
        erroresEnNivelActual = 0;
        tiempoInicioNivel = Time.time;
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
            if (n != null && !n.isStaffNode && n.nivel == nivelInmediatoSuperior && (n.departamento == depto || n.departamento == Departamento.General))
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
                FeedbackManager.Instance.MostrarMensajeEstado("No hay movimientos previos para deshacer.", new Color(0.20f, 0.26f, 0.36f, 0.95f), 1.8f);
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
            FeedbackManager.Instance.MostrarMensajeEstado("↶ Último movimiento revertido.", new Color(0.16f, 0.40f, 0.62f, 0.95f), 1.6f);
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
        if (menuStreakBadgeText != null)
        {
            int racha = DailyStreakManager.Instance != null ? DailyStreakManager.Instance.currentStreak : 0;
            int comodines = WildcardManager.Instance != null ? WildcardManager.Instance.wildcardsDisponibles : 2;
            int totalEstrellas = ObtenerTotalEstrellasCampana();
            int maxEstrellas = nivelesCampana.Length * 3;
            string rango = ObtenerTituloRangoCorporativo();

            menuStreakBadgeText.text =
                $"<color=#FBBF24><b>{rango}</b></color>\n" +
                $"<size=86%>⭐ <b>{totalEstrellas}/{maxEstrellas}</b>   |   🔥 Racha: <b>{racha}d</b>   |   ⚡ Comodines: <b>{comodines}</b></size>";
        }

        if (botonRescatarRachaMenu != null && DailyStreakManager.Instance != null)
        {
            bool tieneRachaPerdida = DailyStreakManager.Instance.lostStreakToRecover > 0;
            botonRescatarRachaMenu.gameObject.SetActive(tieneRachaPerdida);
            if (tieneRachaPerdida)
            {
                TextMeshProUGUI txt = botonRescatarRachaMenu.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = $"🔥 ¡Recuperar Racha de {DailyStreakManager.Instance.lostStreakToRecover} días! (🎬 Video)";
                }
            }
        }

        if (careerRankSummaryText != null)
        {
            ActualizarTextoCarrera();
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
        if (careerView != null) careerView.SetActive(vista == "career");
        if (settingsView != null) settingsView.SetActive(vista == "settings");

        if (vista == "career") ActualizarTextoCarrera();
    }

    public void IniciarNivelCampanaDesdeMenu(int indice)
    {
        int maxDesbloqueado = PlayerPrefs.GetInt(PrefsKeyNivelMaxDesbloqueado, 0);
        if (indice > maxDesbloqueado)
        {
            if (FeedbackManager.Instance != null)
            {
                FeedbackManager.Instance.MostrarMensajeEstado(
                    $"🔒 Completá primero el Nivel {maxDesbloqueado + 1} para desbloquear esta corporación.",
                    new Color(0.65f, 0.20f, 0.20f, 0.96f),
                    2.5f
                );
            }
            return;
        }

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
                    $"📅 Operación Diaria #{numeroDesafioDiario} (Semilla #{seed}). ¡Sumá días a tu racha para ganar comodines!",
                    new Color(0.12f, 0.42f, 0.58f, 0.96f),
                    3.0f
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
        erroresEnNivelActual = 0;
        tiempoInicioNivel = Time.time;

        if (!enModoDiario && indiceNivelCampana < nivelesCampana.Length)
        {
            ConfiguracionNivelComercial cfg = nivelesCampana[indiceNivelCampana];
            criterioActual = cfg.criterio;
            modoAutoridadActual = cfg.autoridad;
            ExigirCadenaEstricta = true;

            ActualizarCabeceraUI(cfg.titulo, cfg.subtitulo);
            ProceduralLevelGenerator.Instance.GenerarNivelComercial(
                cfg.semillaBase,
                cfg.cantidadNodos,
                cfg.criterio,
                cfg.autoridad,
                cfg.incluirStaff,
                cfg.preconectarCrisis
            );
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

            bool staffDiario = (numeroDesafioDiario % 2 == 0);
            bool crisisDiaria = (numeroDesafioDiario % 3 == 0);

            ActualizarCabeceraUI(
                $"📅 Operación Diaria #{numeroDesafioDiario} • {criterioActual}",
                $"Modo {modoAutoridadActual} | {(staffDiario ? "Con Staff VIP | " : "")}Doble toque para desconectar"
            );

            ProceduralLevelGenerator.Instance.GenerarNivelComercial(seed, 10, criterioActual, modoAutoridadActual, staffDiario, crisisDiaria);
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
                FeedbackManager.Instance.MostrarMensajeEstado("↻ Tablero corporativo reiniciado.", new Color(0.20f, 0.28f, 0.40f, 0.94f), 1.6f);
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
            float duracionSegundos = Mathf.Max(1f, Time.time - tiempoInicioNivel);

            ScorecardOrganizacional scorecard = OrgValidator.EvaluarEstructura(
                nodosActivos,
                nodoRaiz,
                modoAutoridadActual,
                erroresEnNivelActual,
                duracionSegundos
            );

            xpCarrera += scorecard.xpGanada;
            PlayerPrefs.SetInt(PrefsKeyXPCarrera, xpCarrera);

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
            }
            PlayerPrefs.Save();

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
        string resumenNivel = (!enModoDiario && indiceNivelCampana < nivelesCampana.Length)
            ? nivelesCampana[indiceNivelCampana].resumenVictoria
            : $"<b>¡Operación Diaria #{numeroDesafioDiario} Completada!</b>\nOptimizaste la estructura <b>{criterioActual}</b> y sumaste progreso a tu carrera ejecutiva.";

        string mensajeFinal =
            $"{resumenNivel}\n\n" +
            $"<size=130%><color=#FBBF24>{estrellasIcono}</color></size>\n" +
            $"{scorecard.evaluacionComercial}\n\n" +
            $"<size=82%><color=#CBD5E1>💼 Presupuesto: <b>${scorecard.costoEstructura}K/mes</b>  |  ⚡ Precisión: <b>{Mathf.RoundToInt(scorecard.eficienciaTramo * 100f)}%</b>  |  🏆 <b>+{scorecard.xpGanada} XP</b></color></size>";

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.MostrarPantallaNivelCompletado(mensajeFinal, scorecard.estrellas);
        }
        else
        {
            AvanzarSiguienteNivel();
        }
    }

    // ==================== CONSTRUCCIÓN DE CABECERA Y BOTONES DE JUEGO ====================

    private void CrearCabeceraComercialUI()
    {
        if (headerTitleText != null) return;

        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null) return;

        if (Camera.main != null)
        {
            Camera.main.backgroundColor = VectorUIKit.BgDeepNavy;
        }

        headerPanelObj = new GameObject("CommercialHeaderPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        headerPanelObj.transform.SetParent(mainCanvas.transform, false);
        headerPanelObj.transform.SetAsFirstSibling();

        RectTransform rt = headerPanelObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.025f, 0.83f);
        rt.anchorMax = new Vector2(0.975f, 0.916f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = headerPanelObj.GetComponent<Image>();
        VectorUIKit.ApplyRoundedStyle(bg, new Color(0.08f, 0.13f, 0.22f, 0.96f), pill: false, addShadow: true);
        VectorUIKit.ApplyOutline(bg, new Color(0.35f, 0.58f, 0.90f, 0.30f), 1.5f);
        bg.raycastTarget = false;

        GameObject titleObj = new GameObject("HeaderTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(headerPanelObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.04f, 0.48f);
        titleRt.anchorMax = new Vector2(0.96f, 0.96f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;

        headerTitleText = titleObj.GetComponent<TextMeshProUGUI>();
        headerTitleText.fontStyle = FontStyles.Bold;
        headerTitleText.alignment = TextAlignmentOptions.Center;
        headerTitleText.color = VectorUIKit.AccentGold;
        headerTitleText.enableAutoSizing = true;
        headerTitleText.fontSizeMin = 15f;
        headerTitleText.fontSizeMax = 27f;
        headerTitleText.raycastTarget = false;

        GameObject subObj = new GameObject("HeaderSubtitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(headerPanelObj.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.04f, 0.06f);
        subRt.anchorMax = new Vector2(0.96f, 0.48f);
        subRt.offsetMin = Vector2.zero;
        subRt.offsetMax = Vector2.zero;

        headerSubText = subObj.GetComponent<TextMeshProUGUI>();
        headerSubText.alignment = TextAlignmentOptions.Center;
        headerSubText.color = new Color(0.86f, 0.92f, 0.98f);
        headerSubText.enableAutoSizing = true;
        headerSubText.fontSizeMin = 11f;
        headerSubText.fontSizeMax = 19f;
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

        CrearBotonUI(
            topButtonsContainer.transform,
            "BotonAbrirMenu",
            "☰ Menú",
            new Vector2(0.31f, 0.926f),
            new Vector2(0.52f, 0.982f),
            new Color(0.14f, 0.34f, 0.56f, 0.98f),
            MostrarMenuPrincipal
        );

        CrearBotonUI(
            topButtonsContainer.transform,
            "BotonDeshacer",
            "↶ Deshacer",
            new Vector2(0.54f, 0.926f),
            new Vector2(0.75f, 0.982f),
            new Color(0.52f, 0.34f, 0.12f, 0.98f),
            DeshacerUltimaAccion
        );

        CrearBotonUI(
            topButtonsContainer.transform,
            "BotonReiniciarNivel",
            "↻ Reiniciar",
            new Vector2(0.77f, 0.926f),
            new Vector2(0.975f, 0.982f),
            new Color(0.20f, 0.27f, 0.38f, 0.98f),
            ReiniciarNivelActual
        );
    }

    // ==================== MENÚ PRINCIPAL COMERCIAL, CAMPAÑA 15 NIVELES Y CARRERA ====================

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
        rootBg.color = VectorUIKit.BgDeepNavy;
        rootBg.raycastTarget = true;

        ConstruirSubvistaHome(mainMenuPanel.transform);
        ConstruirSubvistaSelectorNiveles(mainMenuPanel.transform);
        ConstruirSubvistaCarrera(mainMenuPanel.transform);
        ConstruirSubvistaAjustes(mainMenuPanel.transform);
    }

    private void ConstruirSubvistaHome(Transform parent)
    {
        mainHomeView = CrearContenedorVista(parent, "HomeView");

        CrearTextoUI(
            mainHomeView.transform,
            "CommercialTag",
            "CORPORATE STRATEGY & PUZZLE TYCOON",
            new Vector2(0.06f, 0.915f),
            new Vector2(0.94f, 0.96f),
            12f, 20f,
            VectorUIKit.AccentCyan,
            FontStyles.Bold
        );

        CrearTextoUI(
            mainHomeView.transform,
            "GameTitle",
            "🏢 OrgRise",
            new Vector2(0.06f, 0.815f),
            new Vector2(0.94f, 0.915f),
            40f, 76f,
            Color.white,
            FontStyles.Bold
        );

        CrearTextoUI(
            mainHomeView.transform,
            "GameSubtitle",
            "Conectá el Talento • Optimizá el Mando • Construí tu Imperio",
            new Vector2(0.07f, 0.76f),
            new Vector2(0.93f, 0.815f),
            13f, 23f,
            VectorUIKit.AccentGold,
            FontStyles.Bold
        );

        // Tarjeta de Rango Corporativo, Estrellas y Racha
        GameObject streakCard = CrearPanelTarjeta(
            mainHomeView.transform,
            "CareerStatusCard",
            new Vector2(0.06f, 0.65f),
            new Vector2(0.94f, 0.75f),
            new Color(0.10f, 0.16f, 0.26f, 1f)
        );
        VectorUIKit.ApplyOutline(streakCard.GetComponent<Image>(), new Color(0.35f, 0.60f, 0.95f, 0.35f), 1.5f);

        menuStreakBadgeText = CrearTextoUI(
            streakCard.transform,
            "StreakBadgeText",
            "🌱 Fundador Startup\n⭐ 0/45   |   🔥 Racha: 0d   |   ⚡ Comodines: 2",
            new Vector2(0.04f, 0.08f),
            new Vector2(0.96f, 0.92f),
            13f, 23f,
            Color.white,
            FontStyles.Normal
        );

        // Botón Jugar Campaña Principal
        CrearBotonMenuGrande(
            mainHomeView.transform,
            "BtnPlayCampaign",
            "▶  CONTINUAR IMPERIO CORPORATIVO",
            "Avanzá por las 15 compañías (Startups, Scaleups y Fortune 500)",
            new Vector2(0.06f, 0.53f),
            new Vector2(0.94f, 0.63f),
            new Color(0.08f, 0.56f, 0.38f, 1f),
            () => IniciarNivelCampanaDesdeMenu(indiceNivelCampana < nivelesCampana.Length ? indiceNivelCampana : nivelesCampana.Length - 1)
        );

        // Botón Mapa de 15 Niveles
        CrearBotonMenuGrande(
            mainHomeView.transform,
            "BtnOpenLevelSelector",
            "🗺️  MAPA DE EMPRESAS (15 NIVELES)",
            "Elegí la etapa corporativa y buscá las 3 ⭐ en cada nivel",
            new Vector2(0.06f, 0.415f),
            new Vector2(0.94f, 0.515f),
            new Color(0.14f, 0.38f, 0.68f, 1f),
            () =>
            {
                pestanaEtapaSelector = Mathf.Clamp(indiceNivelCampana / 5, 0, 2);
                ConstruirBotonesSelectorNivelesDinamicos();
                CambiarSubvistaMenu("selector");
            }
        );

        // Botón Operación Diaria Infinita
        CrearBotonMenuGrande(
            mainHomeView.transform,
            "BtnDailyChallenge",
            "📅  OPERACIÓN DIARIA INFINITA",
            "Nuevo desafío cada día • Sumá racha y ganá paquetes de ⚡ Comodines",
            new Vector2(0.06f, 0.30f),
            new Vector2(0.94f, 0.40f),
            new Color(0.68f, 0.38f, 0.10f, 1f),
            IniciarDesafioDiarioDesdeMenu
        );

        // Botón Inyección de Capital (Rewarded Ad en Menú)
        CrearBotonMenuGrande(
            mainHomeView.transform,
            "BtnRewardedAdMenu",
            "🎬  INYECCIÓN DE CAPITAL (+2 ⚡ GRATIS)",
            "Mirá un breve video para sumar +2 Comodines de Reestructuración",
            new Vector2(0.06f, 0.185f),
            new Vector2(0.94f, 0.285f),
            new Color(0.46f, 0.22f, 0.68f, 1f),
            () =>
            {
                if (AdMobManager.Instance != null)
                    AdMobManager.Instance.MostrarAnuncioBonificado(2, ActualizarBadgeMenu);
                else if (WildcardManager.Instance != null)
                    WildcardManager.Instance.RecompensarComodin(2);
            }
        );

        // Botón emergente Salva-Racha (visible solo si el jugador perdió una racha >= 2 días)
        botonRescatarRachaMenu = CrearBotonUI(
            mainHomeView.transform,
            "BtnRescueStreak",
            "🔥 ¡Recuperar Racha Perdida! (🎬 Video)",
            new Vector2(0.06f, 0.125f),
            new Vector2(0.94f, 0.175f),
            new Color(0.85f, 0.26f, 0.18f, 1f),
            () =>
            {
                if (AdMobManager.Instance != null)
                {
                    AdMobManager.Instance.MostrarAnuncioBonificado(1, () =>
                    {
                        if (DailyStreakManager.Instance != null)
                            DailyStreakManager.Instance.RecuperarRachaConVideo();
                    });
                }
                else if (DailyStreakManager.Instance != null)
                {
                    DailyStreakManager.Instance.RecuperarRachaConVideo();
                }
            }
        );
        botonRescatarRachaMenu.gameObject.SetActive(false);

        // Fila inferior: Carrera & Logros y Ajustes
        CrearBotonUI(
            mainHomeView.transform,
            "BtnCareer",
            "🏆 Carrera & Logros",
            new Vector2(0.06f, 0.035f),
            new Vector2(0.485f, 0.112f),
            new Color(0.18f, 0.26f, 0.38f, 1f),
            () => CambiarSubvistaMenu("career")
        );

        CrearBotonUI(
            mainHomeView.transform,
            "BtnSettings",
            "⚙ Ajustes",
            new Vector2(0.515f, 0.035f),
            new Vector2(0.94f, 0.112f),
            new Color(0.20f, 0.27f, 0.38f, 1f),
            () => CambiarSubvistaMenu("settings")
        );
    }

    private GameObject levelButtonsContainer;
    private Button[] tabEtapaButtons = new Button[3];

    private void ConstruirSubvistaSelectorNiveles(Transform parent)
    {
        levelSelectorView = CrearContenedorVista(parent, "LevelSelectorView");

        CrearTextoUI(
            levelSelectorView.transform,
            "SelectorHeader",
            "🗺️ Mapa de Corporaciones (15 Niveles)",
            new Vector2(0.05f, 0.90f),
            new Vector2(0.95f, 0.965f),
            22f, 38f,
            VectorUIKit.AccentGold,
            FontStyles.Bold
        );

        // Pestañas de las 3 Etapas (Niveles 1-5, 6-10, 11-15)
        string[] nombresEtapas = { "Etapa 1: Startups", "Etapa 2: Scaleups", "Etapa 3: Imperios" };
        for (int t = 0; t < 3; t++)
        {
            int tabIdx = t;
            float xMin = 0.05f + t * 0.305f;
            float xMax = xMin + 0.29f;
            tabEtapaButtons[t] = CrearBotonUI(
                levelSelectorView.transform,
                $"TabEtapa_{t}",
                nombresEtapas[t],
                new Vector2(xMin, 0.825f),
                new Vector2(xMax, 0.888f),
                new Color(0.16f, 0.24f, 0.36f, 1f),
                () =>
                {
                    pestanaEtapaSelector = tabIdx;
                    ConstruirBotonesSelectorNivelesDinamicos();
                }
            );
        }

        levelButtonsContainer = new GameObject("ButtonsContainer", typeof(RectTransform));
        levelButtonsContainer.transform.SetParent(levelSelectorView.transform, false);
        RectTransform rt = levelButtonsContainer.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.135f);
        rt.anchorMax = new Vector2(1f, 0.81f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        ConstruirBotonesSelectorNivelesDinamicos();

        CrearBotonUI(
            levelSelectorView.transform,
            "BtnBackFromSelector",
            "⬅ Volver al Menú Principal",
            new Vector2(0.12f, 0.035f),
            new Vector2(0.88f, 0.112f),
            new Color(0.22f, 0.28f, 0.38f, 1f),
            () => CambiarSubvistaMenu("home")
        );
    }

    private void ConstruirBotonesSelectorNivelesDinamicos()
    {
        if (levelButtonsContainer == null) return;

        for (int t = 0; t < tabEtapaButtons.Length; t++)
        {
            if (tabEtapaButtons[t] != null)
            {
                Image img = tabEtapaButtons[t].GetComponent<Image>();
                if (img != null)
                {
                    img.color = (t == pestanaEtapaSelector)
                        ? new Color(0.14f, 0.46f, 0.74f, 1f)
                        : new Color(0.13f, 0.19f, 0.29f, 0.95f);
                }
            }
        }

        for (int c = levelButtonsContainer.transform.childCount - 1; c >= 0; c--)
        {
            Destroy(levelButtonsContainer.transform.GetChild(c).gameObject);
        }

        Color[] coloresNiveles = new Color[]
        {
            new Color(0.12f, 0.46f, 0.34f, 1f),
            new Color(0.14f, 0.38f, 0.62f, 1f),
            new Color(0.52f, 0.34f, 0.14f, 1f),
            new Color(0.40f, 0.22f, 0.58f, 1f),
            new Color(0.12f, 0.44f, 0.52f, 1f)
        };

        float topStart = 0.97f;
        float cardHeight = 0.168f;
        float gap = 0.024f;

        int maxDesbloqueado = PlayerPrefs.GetInt(PrefsKeyNivelMaxDesbloqueado, 0);
        int startIndex = pestanaEtapaSelector * 5;
        int endIndex = Mathf.Min(startIndex + 5, nivelesCampana.Length);

        for (int i = startIndex; i < endIndex; i++)
        {
            int idx = i;
            int slotLocal = i - startIndex;
            ConfiguracionNivelComercial cfg = nivelesCampana[i];
            float yMax = topStart - (slotLocal * (cardHeight + gap));
            float yMin = yMax - cardHeight;

            int estrellas = PlayerPrefs.GetInt(string.Format(PrefsKeyEstrellasNivel, idx), 0);
            string estrellasTxt = estrellas > 0 ? new string('⭐', estrellas) : "☆☆☆";

            bool desbloqueado = idx <= maxDesbloqueado;
            Color btnColor = desbloqueado ? coloresNiveles[slotLocal % coloresNiveles.Length] : new Color(0.14f, 0.17f, 0.23f, 0.92f);
            string prefijoEstado = desbloqueado ? "" : "🔒 ";

            CrearBotonMenuGrande(
                levelButtonsContainer.transform,
                $"BtnSelectLevel_{i + 1}",
                $"{prefijoEstado}{cfg.nombreCorto}",
                $"{cfg.etiquetaModo}   |   {estrellasTxt}",
                new Vector2(0.06f, yMin),
                new Vector2(0.94f, yMax),
                btnColor,
                () => IniciarNivelCampanaDesdeMenu(idx)
            );
        }
    }

    private void ConstruirSubvistaCarrera(Transform parent)
    {
        careerView = CrearContenedorVista(parent, "CareerView");

        CrearTextoUI(
            careerView.transform,
            "CareerHeader",
            "🏆 Carrera Corporativa & Logros",
            new Vector2(0.06f, 0.89f),
            new Vector2(0.94f, 0.96f),
            22f, 38f,
            VectorUIKit.AccentGold,
            FontStyles.Bold
        );

        GameObject card = CrearPanelTarjeta(
            careerView.transform,
            "CareerBodyCard",
            new Vector2(0.06f, 0.14f),
            new Vector2(0.94f, 0.87f),
            new Color(0.09f, 0.14f, 0.22f, 1f)
        );
        VectorUIKit.ApplyOutline(card.GetComponent<Image>(), new Color(0.35f, 0.58f, 0.90f, 0.30f), 1.5f);

        careerRankSummaryText = CrearTextoUI(
            card.transform,
            "CareerSummaryText",
            "",
            new Vector2(0.06f, 0.05f),
            new Vector2(0.94f, 0.95f),
            12f, 22f,
            new Color(0.92f, 0.95f, 0.99f),
            FontStyles.Normal
        );
        careerRankSummaryText.alignment = TextAlignmentOptions.MidlineLeft;

        ActualizarTextoCarrera();

        CrearBotonUI(
            careerView.transform,
            "BtnBackFromCareer",
            "⬅ Volver al Menú Principal",
            new Vector2(0.12f, 0.035f),
            new Vector2(0.88f, 0.112f),
            new Color(0.22f, 0.28f, 0.38f, 1f),
            () => CambiarSubvistaMenu("home")
        );
    }

    private void ActualizarTextoCarrera()
    {
        if (careerRankSummaryText == null) return;

        int estrellas = ObtenerTotalEstrellasCampana();
        int maxEstrellas = nivelesCampana.Length * 3;
        int mejorRacha = DailyStreakManager.Instance != null ? DailyStreakManager.Instance.bestStreak : 0;
        string rango = ObtenerTituloRangoCorporativo();

        string chk1 = indiceNivelCampana >= 1 || nivelMaxDesbloqueado >= 1 ? "✅" : "⬜";
        string chk2 = nivelMaxDesbloqueado >= 5 ? "✅" : "⬜";
        string chk3 = estrellas >= 15 ? "✅" : "⬜";
        string chk4 = mejorRacha >= 3 ? "✅" : "⬜";
        string chk5 = estrellas >= 30 ? "✅" : "⬜";
        string chk6 = estrellas >= 45 ? "✅" : "⬜";

        careerRankSummaryText.text =
            $"<size=115%><b>Rango Actual: <color=#FBBF24>{rango}</color></b></size>\n" +
            $"• Estrellas Totales: <b>{estrellas} / {maxEstrellas} ⭐</b>\n" +
            $"• Reputación Acumulada: <b>{xpCarrera} XP</b>\n" +
            $"• Récord de Racha Diaria: <b>{mejorRacha} días 🔥</b>\n\n" +
            $"<b>🏅 HITOS Y TROFEOS CORPORATIVOS:</b>\n" +
            $"{chk1} <b>Primera Ronda Seed:</b> Completar tu primera startup.\n" +
            $"{chk2} <b>Arquitecto Scaleup:</b> Desbloquear la Etapa 2 (Nivel 6+).\n" +
            $"{chk3} <b>Socio Director (15⭐):</b> Acumular 15 estrellas en campaña.\n" +
            $"{chk4} <b>Hábito Ejecutivo (3d 🔥):</b> Alcanzar 3 días seguidos de racha.\n" +
            $"{chk5} <b>Magnate Global (30⭐):</b> Acumular 30 estrellas en campaña.\n" +
            $"{chk6} <b>Perfección Fortune 500 (45⭐):</b> Lograr 3⭐ en los 15 niveles.";
    }

    private void ConstruirSubvistaAjustes(Transform parent)
    {
        settingsView = CrearContenedorVista(parent, "SettingsView");

        CrearTextoUI(
            settingsView.transform,
            "SettingsHeader",
            "⚙ Ajustes del Juego",
            new Vector2(0.06f, 0.89f),
            new Vector2(0.94f, 0.96f),
            24f, 40f,
            VectorUIKit.AccentGold,
            FontStyles.Bold
        );

        GameObject card = CrearPanelTarjeta(
            settingsView.transform,
            "SettingsBodyCard",
            new Vector2(0.07f, 0.18f),
            new Vector2(0.93f, 0.86f),
            new Color(0.09f, 0.14f, 0.22f, 1f)
        );
        VectorUIKit.ApplyOutline(card.GetComponent<Image>(), new Color(0.35f, 0.58f, 0.90f, 0.30f), 1.5f);

        Button btnSFX = CrearBotonUI(
            card.transform,
            "BtnToggleSFX",
            FeedbackManager.Instance != null && FeedbackManager.Instance.IsSFXEnabled() ? "🔊 Efectos de Sonido (SFX): ACTIVO" : "🔇 Efectos de Sonido (SFX): APAGADO",
            new Vector2(0.06f, 0.76f),
            new Vector2(0.94f, 0.90f),
            new Color(0.16f, 0.36f, 0.56f, 1f),
            null
        );
        btnSFX.onClick.AddListener(() =>
        {
            if (FeedbackManager.Instance != null)
            {
                bool nuevo = !FeedbackManager.Instance.IsSFXEnabled();
                FeedbackManager.Instance.SetSFXEnabled(nuevo);
                btnSFX.GetComponentInChildren<TextMeshProUGUI>().text = nuevo ? "🔊 Efectos de Sonido (SFX): ACTIVO" : "🔇 Efectos de Sonido (SFX): APAGADO";
            }
        });

        Button btnBGM = CrearBotonUI(
            card.transform,
            "BtnToggleBGM",
            FeedbackManager.Instance != null && FeedbackManager.Instance.IsBGMEnabled() ? "🎵 Música Ambiental (BGM): ACTIVA" : "🎵 Música Ambiental (BGM): APAGADA",
            new Vector2(0.06f, 0.58f),
            new Vector2(0.94f, 0.72f),
            new Color(0.14f, 0.44f, 0.34f, 1f),
            null
        );
        btnBGM.onClick.AddListener(() =>
        {
            if (FeedbackManager.Instance != null)
            {
                bool nuevo = !FeedbackManager.Instance.IsBGMEnabled();
                FeedbackManager.Instance.SetBGMEnabled(nuevo);
                btnBGM.GetComponentInChildren<TextMeshProUGUI>().text = nuevo ? "🎵 Música Ambiental (BGM): ACTIVA" : "🎵 Música Ambiental (BGM): APAGADA";
            }
        });

        Button btnHaptics = CrearBotonUI(
            card.transform,
            "BtnToggleHaptics",
            FeedbackManager.Instance != null && FeedbackManager.Instance.IsHapticsEnabled() ? "📳 Vibración Háptica: ACTIVA" : "📳 Vibración Háptica: APAGADA",
            new Vector2(0.06f, 0.40f),
            new Vector2(0.94f, 0.54f),
            new Color(0.52f, 0.34f, 0.14f, 1f),
            null
        );
        btnHaptics.onClick.AddListener(() =>
        {
            if (FeedbackManager.Instance != null)
            {
                bool nuevo = !FeedbackManager.Instance.IsHapticsEnabled();
                FeedbackManager.Instance.SetHapticsEnabled(nuevo);
                btnHaptics.GetComponentInChildren<TextMeshProUGUI>().text = nuevo ? "📳 Vibración Háptica: ACTIVA" : "📳 Vibración Háptica: APAGADA";
            }
        });

        CrearBotonUI(
            card.transform,
            "BtnResetProgress",
            "⚠️ Restablecer Progreso y Carrera",
            new Vector2(0.06f, 0.10f),
            new Vector2(0.94f, 0.24f),
            new Color(0.68f, 0.16f, 0.18f, 1f),
            () =>
            {
                PlayerPrefs.DeleteKey(PrefsKeyNivelMaxDesbloqueado);
                PlayerPrefs.DeleteKey(PrefsKeyIndiceCampana);
                PlayerPrefs.DeleteKey(PrefsKeyXPCarrera);
                for (int k = 0; k < 20; k++) PlayerPrefs.DeleteKey(string.Format(PrefsKeyEstrellasNivel, k));
                if (WildcardManager.Instance != null) WildcardManager.Instance.RestablecerComodinesPorDefecto();
                CargarPersistenciaCampana();
                if (FeedbackManager.Instance != null) FeedbackManager.Instance.MostrarMensajeEstado("Progreso restablecido.", new Color(0.55f, 0.15f, 0.15f, 0.95f), 2.2f);
                ActualizarBadgeMenu();
            }
        );

        CrearBotonUI(
            settingsView.transform,
            "BtnBackFromSettings",
            "⬅ Volver al Menú Principal",
            new Vector2(0.12f, 0.035f),
            new Vector2(0.88f, 0.112f),
            new Color(0.22f, 0.28f, 0.38f, 1f),
            () => CambiarSubvistaMenu("home")
        );
    }

    // ==================== HELPERS DE CONSTRUCCIÓN DE UI VECTORIAL ====================

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
        VectorUIKit.ApplyRoundedStyle(img, colorFondo, pill: false, addShadow: true);
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
        VectorUIKit.ApplyRoundedStyle(img, colorFondo, pill: true, addShadow: true);
        VectorUIKit.ApplyOutline(img, new Color(1f, 1f, 1f, 0.22f), 1.2f);

        Button btn = btnObj.GetComponent<Button>();
        if (accion != null) btn.onClick.AddListener(accion);

        CrearTextoUI(
            btnObj.transform,
            "Label",
            etiqueta,
            new Vector2(0.05f, 0.1f),
            new Vector2(0.95f, 0.9f),
            12f, 22f,
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
        VectorUIKit.ApplyRoundedStyle(img, colorFondo, pill: false, addShadow: true);
        VectorUIKit.ApplyOutline(img, new Color(1f, 1f, 1f, 0.25f), 1.4f);

        Button btn = btnObj.GetComponent<Button>();
        if (accion != null) btn.onClick.AddListener(accion);

        CrearTextoUI(
            btnObj.transform,
            "Title",
            titulo,
            new Vector2(0.04f, 0.46f),
            new Vector2(0.96f, 0.93f),
            14f, 25f,
            Color.white,
            FontStyles.Bold
        );

        CrearTextoUI(
            btnObj.transform,
            "Subtitle",
            subtitulo,
            new Vector2(0.04f, 0.08f),
            new Vector2(0.96f, 0.46f),
            10f, 18f,
            new Color(0.90f, 0.95f, 1.0f),
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
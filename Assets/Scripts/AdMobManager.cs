using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

/// <summary>
/// Gestor centralizado de Google AdMob (Google Mobile Ads Unity Plugin v11.6.0).
/// Implementa los 5 formatos de la cuenta respetando estrictamente las políticas oficiales de Google AdMob:
/// 1. Banner (fijo en el borde inferior con franja de seguridad libre de controles interactivos).
/// 2. Carga de aplicación / App Open (al abrir la app y al volver desde segundo plano, con expiración de 4h y cooldown).
/// 3. Intersticial (pre-cargado, solo en transiciones naturales, máximo 1 cada 2 acciones de usuario y cooldown mínimo).
/// 4. Intersticial Bonificado (en transiciones de fin de nivel, con pantalla introductoria obligatoria y opción "No, gracias").
/// 5. Bonificado / Rewarded (100% voluntario mediante botones con divulgación explícita de la recompensa).
/// </summary>
public class AdMobManager : MonoBehaviour
{
    public static AdMobManager Instance;

    [Header("ID de Aplicación AdMob")]
    public string appId = "ca-app-pub-2266949018056491~8163314986";

    [Header("IDs de Bloques de Anuncios (Producción)")]
    public string bannerAdUnitId = "ca-app-pub-2266949018056491/9799830280";
    public string rewardedAdUnitId = "ca-app-pub-2266949018056491/2693038440";
    public string appOpenAdUnitId = "ca-app-pub-2266949018056491/6383329457";
    public string interstitialAdUnitId = "ca-app-pub-2266949018056491/4224069973";
    public string rewardedInterstitialAdUnitId = "ca-app-pub-2266949018056491/2347434413";

    [Header("Políticas de Frecuencia y Seguridad Legal (AdMob Compliance)")]
    [Tooltip("Mínimo de acciones de transición del usuario antes de permitir un Intersticial (Política AdMob: mínimo 2).")]
    public int minAccionesParaIntersticial = 2;

    [Tooltip("Tiempo mínimo en segundos entre anuncios de pantalla completa no voluntarios (Intersticial / App Open).")]
    public float cooldownPantallaCompletaSegundos = 45f;

    [Tooltip("Segundos de cuenta regresiva en el aviso previo obligatorio del Intersticial Bonificado.")]
    public int segundosIntroIntersticialBonificado = 4;

    // Instancias del SDK v11.6.0
    private BannerView bannerView;
    private InterstitialAd interstitialAd;
    private AppOpenAd appOpenAd;
    private RewardedAd rewardedAd;
    private RewardedInterstitialAd rewardedInterstitialAd;

    // Control de estado y políticas
    private bool sdkInicializado = false;
    private bool anuncioPantallaCompletaActivo = false;
    private bool mostrarAppOpenEnPrimerInicio = true;
    private bool bannerVisible = false;
    private DateTime appOpenHoraCarga = DateTime.MinValue;
    private float ultimoAnuncioPantallaCompletaTiempo = -999f;
    private float ultimoCierrePantallaCompletaTiempo = -999f;
    private int contadorAccionesTransicion = 0;
    private int contadorNivelesCompletados = 0;

    // UI de franja segura para el Banner y Modal Introductorio de Intersticial Bonificado
    private GameObject bannerSafeZoneBar;
    private GameObject rewardedInterstitialPromptPanel;
    private TextMeshProUGUI promptCountdownText;
    private TextMeshProUGUI promptBodyText;
    private Coroutine promptCountdownCoroutine;
    private Action pendingTransitionCallback;
    private int pendingRewardAmount = 2;

    /// <summary>
    /// Garantiza que AdMobManager se cree automáticamente al iniciar la escena incluso si no fue agregado manualmente al Inspector.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrap()
    {
        if (Instance == null)
        {
            AdMobManager existente = FindFirstObjectByType<AdMobManager>();
            if (existente == null)
            {
                GameObject go = new GameObject("AdMobManager");
                existente = go.AddComponent<AdMobManager>();
            }
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        CrearFranjaSeguraBannerUI();

        // Asegurar que todos los callbacks del SDK de AdMob se ejecuten en el hilo principal de Unity
        MobileAds.RaiseAdEventsOnUnityMainThread = true;

        // Suscribirse a cambios de estado de la app (Foreground/Background) para el bloque "Carga de aplicación" (App Open Ad)
        AppStateEventNotifier.AppStateChanged += OnAppStateChanged;

        // Inicializar Google Mobile Ads SDK v11.6.0
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            sdkInicializado = true;
            Debug.Log("[AdMob] SDK v11.6.0 inicializado correctamente.");

            CargarYMostrarBanner();
            CargarAppOpenAd();
            CargarInterstitialAd();
            CargarRewardedAd();
            CargarRewardedInterstitialAd();
        });
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            AppStateEventNotifier.AppStateChanged -= OnAppStateChanged;
            DestruirBanner();
            if (interstitialAd != null) interstitialAd.Destroy();
            if (appOpenAd != null) appOpenAd.Destroy();
            if (rewardedAd != null) rewardedAd.Destroy();
            if (rewardedInterstitialAd != null) rewardedInterstitialAd.Destroy();
        }
    }

    // =========================================================================
    // 1. BANNER (ca-app-pub-2266949018056491/9799830280)
    // Política AdMob: 1 solo banner fijo en el borde inferior, sin superponerse
    // con botones ni contenido interactivo.
    // =========================================================================

    public void CargarYMostrarBanner()
    {
        if (!sdkInicializado) return;

        CrearFranjaSeguraBannerUI();

        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }

        // Usamos Banner estándar (320x50) en la posición inferior fija
        bannerView = new BannerView(bannerAdUnitId, AdSize.Banner, AdPosition.Bottom);

        bannerView.OnBannerAdLoaded += () =>
        {
            bannerVisible = true;
            Debug.Log("[AdMob] Banner cargado y visible en AdPosition.Bottom.");
        };

        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            bannerVisible = false;
            Debug.LogWarning($"[AdMob] Error al cargar Banner: {error}");
        };

        bannerView.LoadAd(new AdRequest());
    }

    public void MostrarBannerSiDisponible()
    {
        if (bannerView != null && !anuncioPantallaCompletaActivo)
        {
            bannerView.Show();
            bannerVisible = true;
        }
    }

    public void OcultarBannerTemporalmente()
    {
        if (bannerView != null && bannerVisible)
        {
            bannerView.Hide();
            bannerVisible = false;
        }
    }

    private void DestruirBanner()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
            bannerVisible = false;
        }
    }

    /// <summary>
    /// Crea un zócalo reservado en el borde inferior del Canvas (0% a 6.5% de altura)
    /// para separar visualmente el Banner de los controles del juego y evitar clics accidentales.
    /// </summary>
    public void CrearFranjaSeguraBannerUI()
    {
        if (bannerSafeZoneBar != null) return;

        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null) return;

        bannerSafeZoneBar = new GameObject("AdMobBannerSafeZone", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bannerSafeZoneBar.transform.SetParent(mainCanvas.transform, false);
        bannerSafeZoneBar.transform.SetAsLastSibling();

        RectTransform rt = bannerSafeZoneBar.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0.065f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = bannerSafeZoneBar.GetComponent<Image>();
        bg.color = new Color(0.03f, 0.05f, 0.08f, 0.98f);
        bg.raycastTarget = true; // Bloquea toques accidentales hacia elementos debajo del banner

        // Línea divisoria superior de seguridad (evita clics accidentales cerca del borde del anuncio)
        GameObject borderLine = new GameObject("SafeBorderLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        borderLine.transform.SetParent(bannerSafeZoneBar.transform, false);
        RectTransform lineRt = borderLine.GetComponent<RectTransform>();
        lineRt.localScale = Vector3.one;
        lineRt.anchorMin = new Vector2(0f, 0.94f);
        lineRt.anchorMax = new Vector2(1f, 1f);
        lineRt.offsetMin = Vector2.zero;
        lineRt.offsetMax = Vector2.zero;

        Image lineImg = borderLine.GetComponent<Image>();
        lineImg.color = new Color(0.22f, 0.30f, 0.42f, 0.85f);
        lineImg.raycastTarget = false;

        GameObject labelObj = new GameObject("SafeZoneLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(bannerSafeZoneBar.transform, false);
        RectTransform lblRt = labelObj.GetComponent<RectTransform>();
        lblRt.localScale = Vector3.one;
        lblRt.anchorMin = new Vector2(0.05f, 0.08f);
        lblRt.anchorMax = new Vector2(0.95f, 0.90f);
        lblRt.offsetMin = Vector2.zero;
        lblRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelObj.GetComponent<TextMeshProUGUI>();
        tmp.text = "ESPACIO PUBLICITARIO (ADMOB BANNER)";
        tmp.fontSizeMin = 9f;
        tmp.fontSizeMax = 14f;
        tmp.enableAutoSizing = true;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.45f, 0.52f, 0.62f, 0.65f);
        tmp.raycastTarget = false;
    }

    // =========================================================================
    // 2. CARGA DE APLICACIÓN / APP OPEN (ca-app-pub-2266949018056491/6383329457)
    // Política AdMob: Solo al abrir la aplicación o al regresar desde segundo plano.
    // Expiración máxima de caché: 4 horas.
    // =========================================================================

    public void CargarAppOpenAd()
    {
        if (!sdkInicializado) return;

        if (appOpenAd != null)
        {
            appOpenAd.Destroy();
            appOpenAd = null;
        }

        AppOpenAd.Load(appOpenAdUnitId, new AdRequest(), (AppOpenAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning($"[AdMob] Error al pre-cargar AppOpenAd: {error}");
                return;
            }

            appOpenAd = ad;
            appOpenHoraCarga = DateTime.UtcNow;
            RegistrarEventosAppOpen(appOpenAd);
            Debug.Log("[AdMob] AppOpenAd (Carga de aplicación) listo.");

            // Si es la apertura inicial en frío de la app y estamos dentro de los primeros segundos en el menú, lo mostramos
            if (mostrarAppOpenEnPrimerInicio && Time.realtimeSinceStartup < 12f && !anuncioPantallaCompletaActivo)
            {
                mostrarAppOpenEnPrimerInicio = false;
                MostrarAppOpenAdSiDisponible();
            }
        });
    }

    private bool EsAppOpenAdValido()
    {
        return appOpenAd != null
            && appOpenAd.CanShowAd()
            && (DateTime.UtcNow - appOpenHoraCarga).TotalHours < 4.0;
    }

    private void OnAppStateChanged(AppState state)
    {
        if (state == AppState.Foreground)
        {
            // Evitar que el regreso a primer plano tras cerrar un Intersticial o Bonificado dispare otro anuncio encima
            if (Time.realtimeSinceStartup - ultimoCierrePantallaCompletaTiempo < 5f)
            {
                return;
            }

            MostrarAppOpenAdSiDisponible();
        }
    }

    public void MostrarAppOpenAdSiDisponible()
    {
        if (anuncioPantallaCompletaActivo) return;

        // Respetar tiempo mínimo entre anuncios de pantalla completa para no saturar al usuario
        if (Time.realtimeSinceStartup - ultimoAnuncioPantallaCompletaTiempo < cooldownPantallaCompletaSegundos)
        {
            return;
        }

        if (!EsAppOpenAdValido())
        {
            CargarAppOpenAd();
            return;
        }

        anuncioPantallaCompletaActivo = true;
        ultimoAnuncioPantallaCompletaTiempo = Time.realtimeSinceStartup;
        OcultarBannerTemporalmente();
        appOpenAd.Show();
    }

    private void RegistrarEventosAppOpen(AppOpenAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            anuncioPantallaCompletaActivo = true;
            OcultarBannerTemporalmente();
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            anuncioPantallaCompletaActivo = false;
            ultimoCierrePantallaCompletaTiempo = Time.realtimeSinceStartup;
            MostrarBannerSiDisponible();
            CargarAppOpenAd();
        };

        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            anuncioPantallaCompletaActivo = false;
            MostrarBannerSiDisponible();
            CargarAppOpenAd();
        };
    }

    // =========================================================================
    // 3. INTERSTICIAL (ca-app-pub-2266949018056491/4224069973)
    // Política AdMob: Solo en pausas/transiciones naturales, pre-cargado antes
    // de cambiar de pantalla, y NUNCA más de 1 cada 2 acciones del usuario.
    // =========================================================================

    public void CargarInterstitialAd()
    {
        if (!sdkInicializado) return;

        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        InterstitialAd.Load(interstitialAdUnitId, new AdRequest(), (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning($"[AdMob] Error al pre-cargar InterstitialAd: {error}");
                return;
            }

            interstitialAd = ad;
            Debug.Log("[AdMob] InterstitialAd pre-cargado y listo.");
        });
    }

    /// <summary>
    /// Registra una acción de transición natural del usuario (cambiar de nivel, volver al menú, reiniciar)
    /// y muestra un anuncio Intersticial SOLO si se cumple el mínimo legal de >= 2 acciones y el cooldown.
    /// Ejecuta <paramref name="alContinuar"/> inmediatamente si no se muestra anuncio, o al cerrarse el anuncio.
    /// </summary>
    public void RegistrarTransicionYMostrarIntersticialSiCorresponde(Action alContinuar)
    {
        contadorAccionesTransicion++;

        bool cumpleFrecuenciaLegal = contadorAccionesTransicion >= Mathf.Max(2, minAccionesParaIntersticial);
        bool cumpleCooldown = (Time.realtimeSinceStartup - ultimoAnuncioPantallaCompletaTiempo) >= cooldownPantallaCompletaSegundos;
        bool listoParaMostrar = interstitialAd != null && interstitialAd.CanShowAd() && !anuncioPantallaCompletaActivo;

        if (cumpleFrecuenciaLegal && cumpleCooldown && listoParaMostrar)
        {
            contadorAccionesTransicion = 0;
            anuncioPantallaCompletaActivo = true;
            ultimoAnuncioPantallaCompletaTiempo = Time.realtimeSinceStartup;
            OcultarBannerTemporalmente();

            Action callbackSeguro = alContinuar;
            bool ejecutado = false;

            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                anuncioPantallaCompletaActivo = false;
                ultimoCierrePantallaCompletaTiempo = Time.realtimeSinceStartup;
                MostrarBannerSiDisponible();
                CargarInterstitialAd();

                if (!ejecutado)
                {
                    ejecutado = true;
                    callbackSeguro?.Invoke();
                }
            };

            interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
            {
                anuncioPantallaCompletaActivo = false;
                MostrarBannerSiDisponible();
                CargarInterstitialAd();

                if (!ejecutado)
                {
                    ejecutado = true;
                    callbackSeguro?.Invoke();
                }
            };

            interstitialAd.Show();
        }
        else
        {
            if (interstitialAd == null || !interstitialAd.CanShowAd())
            {
                CargarInterstitialAd();
            }
            alContinuar?.Invoke();
        }
    }

    // =========================================================================
    // 4. INTERSTICIAL BONIFICADO (ca-app-pub-2266949018056491/2347434413)
    // Política AdMob: Aparece en transiciones naturales (ej. al completar un nivel),
    // pero EXIGE una pantalla previa con opción clara de "No, gracias / Omitir".
    // =========================================================================

    public void CargarRewardedInterstitialAd()
    {
        if (!sdkInicializado) return;

        if (rewardedInterstitialAd != null)
        {
            rewardedInterstitialAd.Destroy();
            rewardedInterstitialAd = null;
        }

        RewardedInterstitialAd.Load(rewardedInterstitialAdUnitId, new AdRequest(), (RewardedInterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning($"[AdMob] Error al pre-cargar RewardedInterstitialAd: {error}");
                return;
            }

            rewardedInterstitialAd = ad;
            Debug.Log("[AdMob] RewardedInterstitialAd (Intersticial bonificado) listo.");
        });
    }

    public bool TieneIntersticialBonificadoListo()
    {
        return rewardedInterstitialAd != null && rewardedInterstitialAd.CanShowAd() && !anuncioPantallaCompletaActivo;
    }

    /// <summary>
    /// Se invoca al avanzar de un nivel completado al siguiente.
    /// Si el Intersticial Bonificado está listo, muestra la pantalla introductoria reglamentaria con opción "No, gracias"
    /// para otorgar +2 Comodines. En caso contrario, evalúa el Intersticial estándar cumpliendo la regla de 2 acciones.
    /// </summary>
    public void IntentarTransicionVictoriaSiguienteNivel(Action avanzarNivelCallback, int comodinesRecompensa = 2)
    {
        contadorNivelesCompletados++;

        if (TieneIntersticialBonificadoListo())
        {
            MostrarPromptIntroductorioIntersticialBonificado(avanzarNivelCallback, comodinesRecompensa);
            return;
        }

        // Si el Intersticial Bonificado no está disponible en este momento, recargarlo y usar la transición legal con Intersticial estándar
        CargarRewardedInterstitialAd();
        RegistrarTransicionYMostrarIntersticialSiCorresponde(avanzarNivelCallback);
    }

    private void MostrarPromptIntroductorioIntersticialBonificado(Action alTerminarTransicion, int cantidadComodines)
    {
        AsegurarModalPromptIntersticialBonificado();
        if (rewardedInterstitialPromptPanel == null)
        {
            alTerminarTransicion?.Invoke();
            return;
        }

        pendingTransitionCallback = alTerminarTransicion;
        pendingRewardAmount = cantidadComodines;

        OcultarBannerTemporalmente();
        rewardedInterstitialPromptPanel.SetActive(true);
        rewardedInterstitialPromptPanel.transform.SetAsLastSibling();

        if (promptBodyText != null)
        {
            promptBodyText.text =
                "<b>🎁 BONIFICACIÓN DE REESTRUCTURACIÓN</b>\n\n" +
                $"Mirando un breve anuncio antes del próximo nivel recibirás automáticamente <b>+{cantidadComodines} Comodines de Reestructuración (⚡)</b>.\n\n" +
                "<size=80%><color=#B0BEC5>Podés cancelar en cualquier momento tocando \"No, gracias\" sin afectar tu partida.</color></size>";
        }

        if (promptCountdownCoroutine != null) StopCoroutine(promptCountdownCoroutine);
        promptCountdownCoroutine = StartCoroutine(RutinaCuentaRegresivaIntersticialBonificado());
    }

    private IEnumerator RutinaCuentaRegresivaIntersticialBonificado()
    {
        int restantes = Mathf.Max(3, segundosIntroIntersticialBonificado);
        while (restantes > 0)
        {
            if (promptCountdownText != null)
            {
                promptCountdownText.text = $"El anuncio bonificado comenzará en {restantes} s...";
            }
            yield return new WaitForSeconds(1f);
            restantes--;
        }

        promptCountdownCoroutine = null;
        AceptarYMostrarIntersticialBonificado();
    }

    private void AceptarYMostrarIntersticialBonificado()
    {
        if (promptCountdownCoroutine != null)
        {
            StopCoroutine(promptCountdownCoroutine);
            promptCountdownCoroutine = null;
        }

        if (rewardedInterstitialPromptPanel != null)
        {
            rewardedInterstitialPromptPanel.SetActive(false);
        }

        Action callbackFinal = pendingTransitionCallback;
        pendingTransitionCallback = null;
        int recompensa = pendingRewardAmount;

        if (!TieneIntersticialBonificadoListo())
        {
            MostrarBannerSiDisponible();
            CargarRewardedInterstitialAd();
            callbackFinal?.Invoke();
            return;
        }

        anuncioPantallaCompletaActivo = true;
        ultimoAnuncioPantallaCompletaTiempo = Time.realtimeSinceStartup;
        contadorAccionesTransicion = 0;

        bool recompensaEntregada = false;
        bool cierreEjecutado = false;

        rewardedInterstitialAd.OnAdFullScreenContentClosed += () =>
        {
            anuncioPantallaCompletaActivo = false;
            ultimoCierrePantallaCompletaTiempo = Time.realtimeSinceStartup;
            MostrarBannerSiDisponible();
            CargarRewardedInterstitialAd();

            if (!cierreEjecutado)
            {
                cierreEjecutado = true;
                callbackFinal?.Invoke();

                if (recompensaEntregada && FeedbackManager.Instance != null)
                {
                    FeedbackManager.Instance.MostrarMensajeEstado(
                        $"🎁 ¡Bono acreditado! Recibiste +{recompensa} Comodines de Reestructuración.",
                        new Color(0.15f, 0.50f, 0.30f, 0.96f),
                        3.0f
                    );
                }
            }
        };

        rewardedInterstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            anuncioPantallaCompletaActivo = false;
            MostrarBannerSiDisponible();
            CargarRewardedInterstitialAd();

            if (!cierreEjecutado)
            {
                cierreEjecutado = true;
                callbackFinal?.Invoke();
            }
        };

        rewardedInterstitialAd.Show((Reward reward) =>
        {
            if (!recompensaEntregada)
            {
                recompensaEntregada = true;
                if (WildcardManager.Instance != null)
                {
                    WildcardManager.Instance.RecompensarComodin(recompensa);
                }
            }
        });
    }

    private void RechazarIntersticialBonificado()
    {
        if (promptCountdownCoroutine != null)
        {
            StopCoroutine(promptCountdownCoroutine);
            promptCountdownCoroutine = null;
        }

        if (rewardedInterstitialPromptPanel != null)
        {
            rewardedInterstitialPromptPanel.SetActive(false);
        }

        MostrarBannerSiDisponible();

        Action callbackFinal = pendingTransitionCallback;
        pendingTransitionCallback = null;
        callbackFinal?.Invoke();
    }

    private void AsegurarModalPromptIntersticialBonificado()
    {
        if (rewardedInterstitialPromptPanel != null) return;

        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null) return;

        rewardedInterstitialPromptPanel = new GameObject("RewardedInterstitialOptOutModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rewardedInterstitialPromptPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform rootRt = rewardedInterstitialPromptPanel.GetComponent<RectTransform>();
        rootRt.localScale = Vector3.one;
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        Image overlayBg = rewardedInterstitialPromptPanel.GetComponent<Image>();
        overlayBg.color = new Color(0.03f, 0.06f, 0.11f, 0.95f);
        overlayBg.raycastTarget = true;

        // Tarjeta central
        GameObject card = new GameObject("ModalCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(rewardedInterstitialPromptPanel.transform, false);
        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.localScale = Vector3.one;
        cardRt.anchorMin = new Vector2(0.07f, 0.22f);
        cardRt.anchorMax = new Vector2(0.93f, 0.78f);
        cardRt.offsetMin = Vector2.zero;
        cardRt.offsetMax = Vector2.zero;

        Image cardBg = card.GetComponent<Image>();
        cardBg.color = new Color(0.10f, 0.15f, 0.23f, 1f);

        // Cuerpo de texto con divulgación clara de recompensa (Política AdMob)
        GameObject bodyObj = new GameObject("PromptBody", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        bodyObj.transform.SetParent(card.transform, false);
        RectTransform bodyRt = bodyObj.GetComponent<RectTransform>();
        bodyRt.localScale = Vector3.one;
        bodyRt.anchorMin = new Vector2(0.06f, 0.44f);
        bodyRt.anchorMax = new Vector2(0.94f, 0.93f);
        bodyRt.offsetMin = Vector2.zero;
        bodyRt.offsetMax = Vector2.zero;

        promptBodyText = bodyObj.GetComponent<TextMeshProUGUI>();
        promptBodyText.alignment = TextAlignmentOptions.Center;
        promptBodyText.color = Color.white;
        promptBodyText.enableAutoSizing = true;
        promptBodyText.fontSizeMin = 15f;
        promptBodyText.fontSizeMax = 28f;

        // Texto de cuenta regresiva
        GameObject countObj = new GameObject("PromptCountdown", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        countObj.transform.SetParent(card.transform, false);
        RectTransform countRt = countObj.GetComponent<RectTransform>();
        countRt.localScale = Vector3.one;
        countRt.anchorMin = new Vector2(0.08f, 0.33f);
        countRt.anchorMax = new Vector2(0.92f, 0.43f);
        countRt.offsetMin = Vector2.zero;
        countRt.offsetMax = Vector2.zero;

        promptCountdownText = countObj.GetComponent<TextMeshProUGUI>();
        promptCountdownText.alignment = TextAlignmentOptions.Center;
        promptCountdownText.fontStyle = FontStyles.Bold;
        promptCountdownText.color = new Color(0.98f, 0.84f, 0.32f, 1f);
        promptCountdownText.enableAutoSizing = true;
        promptCountdownText.fontSizeMin = 14f;
        promptCountdownText.fontSizeMax = 24f;

        // Botón Aceptar ahora
        CrearBotonModal(
            card.transform,
            "BtnWatchNow",
            "🎬 Ver anuncio ahora (+2 ⚡ Comodines)",
            new Vector2(0.08f, 0.18f),
            new Vector2(0.92f, 0.30f),
            new Color(0.15f, 0.52f, 0.32f, 1f),
            AceptarYMostrarIntersticialBonificado
        );

        // Botón Obligatorio de Opt-Out ("No, gracias") exigido por Google AdMob para Intersticial Bonificado
        CrearBotonModal(
            card.transform,
            "BtnNoThanksOptOut",
            "✖ No, gracias (Continuar al siguiente nivel)",
            new Vector2(0.08f, 0.05f),
            new Vector2(0.92f, 0.155f),
            new Color(0.26f, 0.31f, 0.40f, 1f),
            RechazarIntersticialBonificado
        );

        rewardedInterstitialPromptPanel.SetActive(false);
    }

    private void CrearBotonModal(
        Transform parent,
        string nombre,
        string texto,
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

        GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.localScale = Vector3.one;
        txtRt.anchorMin = new Vector2(0.04f, 0.1f);
        txtRt.anchorMax = new Vector2(0.96f, 0.9f);
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 13f;
        tmp.fontSizeMax = 22f;
        tmp.raycastTarget = false;
    }

    // =========================================================================
    // 5. BONIFICADO / REWARDED (ca-app-pub-2266949018056491/2693038440)
    // Política AdMob: Solo se muestra tras aceptación explícita (opt-in) del usuario
    // en botones que indican con exactitud cuántos Comodines recibirá.
    // =========================================================================

    public void CargarRewardedAd()
    {
        if (!sdkInicializado) return;

        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        RewardedAd.Load(rewardedAdUnitId, new AdRequest(), (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning($"[AdMob] Error al pre-cargar RewardedAd: {error}");
                return;
            }

            rewardedAd = ad;
            Debug.Log("[AdMob] RewardedAd (Bonificado) listo.");
        });
    }

    public bool TieneAnuncioBonificadoListo()
    {
        return rewardedAd != null && rewardedAd.CanShowAd() && !anuncioPantallaCompletaActivo;
    }

    /// <summary>
    /// Muestra un anuncio Bonificado (RewardedAd) cuando el jugador presiona voluntariamente un botón de recompensa.
    /// Al finalizar el video, acredita <paramref name="cantidadComodines"/> en WildcardManager.
    /// </summary>
    public void MostrarAnuncioBonificado(int cantidadComodines = 1, Action alCompletarRecompensa = null)
    {
        if (!TieneAnuncioBonificadoListo())
        {
            CargarRewardedAd();
            if (FeedbackManager.Instance != null)
            {
                FeedbackManager.Instance.MostrarMensajeEstado(
                    "⏳ Cargando anuncio bonificado de AdMob... Probá de nuevo en unos segundos.",
                    new Color(0.22f, 0.34f, 0.52f, 0.95f),
                    2.6f
                );
            }
            return;
        }

        anuncioPantallaCompletaActivo = true;
        ultimoAnuncioPantallaCompletaTiempo = Time.realtimeSinceStartup;
        OcultarBannerTemporalmente();

        bool recompensaOtorgada = false;

        rewardedAd.OnAdFullScreenContentClosed += () =>
        {
            anuncioPantallaCompletaActivo = false;
            ultimoCierrePantallaCompletaTiempo = Time.realtimeSinceStartup;
            MostrarBannerSiDisponible();
            CargarRewardedAd();

            if (recompensaOtorgada)
            {
                alCompletarRecompensa?.Invoke();
                if (FeedbackManager.Instance != null)
                {
                    FeedbackManager.Instance.MostrarMensajeEstado(
                        $"🎉 ¡Recompensa acreditada! Recibiste +{cantidadComodines} Comodín(es) de Reestructuración (⚡).",
                        new Color(0.14f, 0.48f, 0.28f, 0.96f),
                        3.0f
                    );
                }
            }
        };

        rewardedAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            anuncioPantallaCompletaActivo = false;
            MostrarBannerSiDisponible();
            CargarRewardedAd();
        };

        rewardedAd.Show((Reward reward) =>
        {
            if (!recompensaOtorgada)
            {
                recompensaOtorgada = true;
                if (WildcardManager.Instance != null)
                {
                    WildcardManager.Instance.RecompensarComodin(cantidadComodines);
                }
            }
        });
    }
}

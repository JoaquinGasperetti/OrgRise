using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

/// <summary>
/// Gestor centralizado de Google AdMob (Google Mobile Ads Unity Plugin v11.6.0).
/// Incluye adaptación responsiva automática para todas las resoluciones y relaciones de aspecto
/// (16:9, 18:9, 19.5:9, 20:9, 4:3 tablets y ventana libre del Editor):
/// - Usa Anchored Adaptive Banner (FullWidth) para que el banner se ajuste al 100% del ancho de cualquier pantalla.
/// - Reajusta dinámicamente los Canvas de anuncios (tanto en dispositivo como los PlaceholderAds del Editor)
///   para evitar que los anuncios de pantalla completa, el botón "Close Ad", el contador o el banner se corten o desborden.
/// - Recalcula automáticamente los anuncios si cambia la resolución o el aspecto de la pantalla en tiempo real.
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

    // Seguimiento de resolución para recalcular anuncios si cambia el tamaño/aspecto de pantalla
    private int ultimoAnchoPantalla = 0;
    private int ultimoAltoPantalla = 0;
    private float alturaRelativaBannerActual = 0.068f;

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
        ultimoAnchoPantalla = Screen.width;
        ultimoAltoPantalla = Screen.height;

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

    void Update()
    {
        // Si cambia la resolución o relación de aspecto (por ejemplo al cambiar de dispositivo en el simulador de Unity o rotar),
        // reajustamos automáticamente el Banner adaptativo y cualquier anuncio activo en pantalla.
        if (Screen.width != ultimoAnchoPantalla || Screen.height != ultimoAltoPantalla)
        {
            ultimoAnchoPantalla = Screen.width;
            ultimoAltoPantalla = Screen.height;

            if (sdkInicializado)
            {
                CargarYMostrarBanner();
                AjustarCanvasesDeAnunciosEnPantalla();
            }

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.RefrescarLayoutResponsivo();
            }
        }

        // Mantener normalizados los Canvas de anuncios cuando hay uno visible en el Editor
        if (anuncioPantallaCompletaActivo || bannerVisible)
        {
            AjustarCanvasesDeAnunciosEnPantalla();
        }
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
    // NORMALIZADOR RESPONSIVO DE ANUNCIOS PARA TODAS LAS PANTALLAS
    // Corrige el escalado tanto en dispositivos de distinto aspecto (16:9, 20:9,
    // 4:3 tablets) como en los prefabs PlaceholderAds del Editor de Unity.
    // =========================================================================

    public void AjustarCanvasesDeAnunciosEnPantalla()
    {
        Canvas[] todosLosCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in todosLosCanvases)
        {
            if (c == null || c.gameObject == null) continue;
            string nombre = c.gameObject.name;

            // 1. Ajuste responsivo del Banner (ADAPTIVE, BANNER, SMART_BANNER)
            if (nombre.StartsWith("ADAPTIVE") || nombre.StartsWith("BANNER") || nombre.StartsWith("SMART_BANNER") || nombre.StartsWith("LARGE_BANNER") || nombre.StartsWith("FULL_BANNER"))
            {
                NormalizarCanvasBanner(c);
            }
            // 2. Ajuste responsivo de anuncios de pantalla completa (Intersticial, AppOpen, Bonificado, Intersticial Bonificado)
            else if (nombre.StartsWith("768x1024") || nombre.StartsWith("1024x768"))
            {
                NormalizarCanvasPantallaCompleta(c);
            }
        }
    }

    private void NormalizarCanvasBanner(Canvas bannerCanvas)
    {
        bannerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        bannerCanvas.overrideSorting = true;
        bannerCanvas.sortingOrder = 500;

        CanvasScaler scaler = bannerCanvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Buscar el objeto hijo "Image" que representa el contenedor del banner
        Transform imgTransform = bannerCanvas.transform.Find("Image");
        if (imgTransform == null && bannerCanvas.transform.childCount > 0)
        {
            imgTransform = bannerCanvas.transform.GetChild(0);
        }

        if (imgTransform != null)
        {
            RectTransform rt = imgTransform.GetComponent<RectTransform>();
            if (rt != null)
            {
                // Respetar SafeArea inferior del teléfono (barra gestual en iOS/Android modernos)
                float safeBottomRatio = Screen.height > 0 ? Mathf.Clamp01(Screen.safeArea.yMin / Screen.height) : 0f;
                float bannerHeightRatio = 0.062f;
                alturaRelativaBannerActual = Mathf.Clamp(safeBottomRatio + bannerHeightRatio, 0.065f, 0.10f);

                rt.localScale = Vector3.one;
                rt.anchorMin = new Vector2(0f, safeBottomRatio);
                rt.anchorMax = new Vector2(1f, alturaRelativaBannerActual);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            Image img = imgTransform.GetComponent<Image>();
            if (img != null)
            {
                img.preserveAspect = false;
            }

            // Ajustar el texto interno si existe ("This is a Test Adaptive Banner")
            Text legacyText = imgTransform.GetComponentInChildren<Text>(true);
            if (legacyText != null)
            {
                RectTransform txtRt = legacyText.GetComponent<RectTransform>();
                if (txtRt != null)
                {
                    txtRt.localScale = Vector3.one;
                    txtRt.anchorMin = new Vector2(0.04f, 0.08f);
                    txtRt.anchorMax = new Vector2(0.96f, 0.92f);
                    txtRt.anchoredPosition = Vector2.zero;
                    txtRt.sizeDelta = Vector2.zero;
                    txtRt.offsetMin = Vector2.zero;
                    txtRt.offsetMax = Vector2.zero;
                }
                legacyText.resizeTextForBestFit = true;
                legacyText.resizeTextMinSize = 12;
                legacyText.resizeTextMaxSize = 28;
                legacyText.alignment = TextAnchor.MiddleCenter;
            }
        }

        ActualizarDimensionFranjaSegura();
    }

    private void NormalizarCanvasPantallaCompleta(Canvas fullScreenCanvas)
    {
        fullScreenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fullScreenCanvas.overrideSorting = true;
        fullScreenCanvas.sortingOrder = 1000;

        CanvasScaler scaler = fullScreenCanvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        // 1. Asegurar que el fondo oscuro cubra el 100% de cualquier pantalla
        Transform bgTransform = fullScreenCanvas.transform.Find("Background");
        if (bgTransform != null)
        {
            RectTransform bgRt = bgTransform.GetComponent<RectTransform>();
            if (bgRt != null)
            {
                bgRt.localScale = Vector3.one;
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.anchoredPosition = Vector2.zero;
                bgRt.sizeDelta = Vector2.zero;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;
            }
            Image bgImg = bgTransform.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.color = new Color(0.02f, 0.04f, 0.08f, 0.94f);
            }
        }

        // 2. Ajustar el contenedor "Ad" con anclas relativas dentro del SafeArea para que NUNCA se desborde
        // ni en pantallas 20:9, ni en 16:9, ni en 4:3 tablets, ni en ventana libre del Editor
        Transform adTransform = fullScreenCanvas.transform.Find("Ad");
        if (adTransform != null)
        {
            float safeMinX = Screen.width > 0 ? Mathf.Clamp01(Screen.safeArea.xMin / Screen.width) : 0f;
            float safeMaxX = Screen.width > 0 ? Mathf.Clamp01(Screen.safeArea.xMax / Screen.width) : 1f;
            float safeMinY = Screen.height > 0 ? Mathf.Clamp01(Screen.safeArea.yMin / Screen.height) : 0f;
            float safeMaxY = Screen.height > 0 ? Mathf.Clamp01(Screen.safeArea.yMax / Screen.height) : 1f;

            RectTransform adRt = adTransform.GetComponent<RectTransform>();
            if (adRt != null)
            {
                adRt.localScale = Vector3.one;
                adRt.anchorMin = new Vector2(Mathf.Max(0.04f, safeMinX + 0.02f), Mathf.Max(0.06f, safeMinY + 0.03f));
                adRt.anchorMax = new Vector2(Mathf.Min(0.96f, safeMaxX - 0.02f), Mathf.Min(0.94f, safeMaxY - 0.03f));
                adRt.pivot = new Vector2(0.5f, 0.5f);
                adRt.anchoredPosition = Vector2.zero;
                adRt.sizeDelta = Vector2.zero;
                adRt.offsetMin = Vector2.zero;
                adRt.offsetMax = Vector2.zero;
            }

            // 3. Ajustar el botón "Close Ad" en la esquina superior izquierda/derecha dentro del contenedor
            Transform btnTransform = adTransform.Find("Button");
            if (btnTransform != null)
            {
                RectTransform btnRt = btnTransform.GetComponent<RectTransform>();
                if (btnRt != null)
                {
                    btnRt.localScale = Vector3.one;
                    btnRt.anchorMin = new Vector2(0.03f, 0.90f);
                    btnRt.anchorMax = new Vector2(0.40f, 0.975f);
                    btnRt.pivot = new Vector2(0.5f, 0.5f);
                    btnRt.anchoredPosition = Vector2.zero;
                    btnRt.sizeDelta = Vector2.zero;
                    btnRt.offsetMin = Vector2.zero;
                    btnRt.offsetMax = Vector2.zero;
                }

                Text btnText = btnTransform.GetComponentInChildren<Text>(true);
                if (btnText != null)
                {
                    RectTransform btnTxtRt = btnText.GetComponent<RectTransform>();
                    if (btnTxtRt != null)
                    {
                        btnTxtRt.localScale = Vector3.one;
                        btnTxtRt.anchorMin = new Vector2(0.06f, 0.08f);
                        btnTxtRt.anchorMax = new Vector2(0.94f, 0.92f);
                        btnTxtRt.anchoredPosition = Vector2.zero;
                        btnTxtRt.sizeDelta = Vector2.zero;
                        btnTxtRt.offsetMin = Vector2.zero;
                        btnTxtRt.offsetMax = Vector2.zero;
                    }
                    btnText.resizeTextForBestFit = true;
                    btnText.resizeTextMinSize = 14;
                    btnText.resizeTextMaxSize = 34;
                    btnText.alignment = TextAnchor.MiddleCenter;
                }
            }

            // 4. Ajustar el contador "5 second(s) remaining" de los anuncios bonificados para que nunca se corte
            for (int i = 0; i < adTransform.childCount; i++)
            {
                Transform child = adTransform.GetChild(i);
                if (child != btnTransform && child.name == "Text")
                {
                    RectTransform countRt = child.GetComponent<RectTransform>();
                    if (countRt != null)
                    {
                        countRt.localScale = Vector3.one;
                        countRt.anchorMin = new Vector2(0.43f, 0.90f);
                        countRt.anchorMax = new Vector2(0.97f, 0.975f);
                        countRt.pivot = new Vector2(0.5f, 0.5f);
                        countRt.anchoredPosition = Vector2.zero;
                        countRt.sizeDelta = Vector2.zero;
                        countRt.offsetMin = Vector2.zero;
                        countRt.offsetMax = Vector2.zero;
                    }

                    Text countTxt = child.GetComponent<Text>();
                    if (countTxt != null)
                    {
                        countTxt.resizeTextForBestFit = true;
                        countTxt.resizeTextMinSize = 12;
                        countTxt.resizeTextMaxSize = 30;
                        countTxt.alignment = TextAnchor.MiddleRight;
                    }
                }
            }
        }
    }

    // =========================================================================
    // 1. BANNER ADAPTATIVO (ca-app-pub-2266949018056491/9799830280)
    // Política AdMob: 1 solo banner fijo en el borde inferior, usando
    // Anchored Adaptive Banner (FullWidth) para ajustarse a cualquier pantalla.
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

        // Usamos Anchored Adaptive Banner de ancho completo (AdSize.FullWidth) según la orientación actual,
        // que se adapta automáticamente al ancho exacto de cualquier teléfono o tablet.
        AdSize adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
        bannerView = new BannerView(bannerAdUnitId, adaptiveSize, AdPosition.Bottom);

        bannerView.OnBannerAdLoaded += () =>
        {
            bannerVisible = true;
            AjustarCanvasesDeAnunciosEnPantalla();
            ActualizarDimensionFranjaSegura();
            Debug.Log($"[AdMob] Adaptive Banner cargado ({bannerView.GetWidthInPixels()}x{bannerView.GetHeightInPixels()} px) en AdPosition.Bottom.");
        };

        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            bannerVisible = false;
            Debug.LogWarning($"[AdMob] Error al cargar Banner: {error}");
        };

        bannerView.LoadAd(new AdRequest());
        AjustarCanvasesDeAnunciosEnPantalla();
    }

    public void MostrarBannerSiDisponible()
    {
        if (bannerView != null && !anuncioPantallaCompletaActivo)
        {
            bannerView.Show();
            bannerVisible = true;
            AjustarCanvasesDeAnunciosEnPantalla();
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
    /// Crea y actualiza el zócalo reservado en el borde inferior del Canvas
    /// calculando su altura según el tamaño real del Adaptive Banner y el SafeArea del dispositivo.
    /// </summary>
    public void CrearFranjaSeguraBannerUI()
    {
        if (bannerSafeZoneBar == null)
        {
            Canvas mainCanvas = FindFirstObjectByType<Canvas>();
            if (mainCanvas == null) return;

            bannerSafeZoneBar = new GameObject("AdMobBannerSafeZone", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bannerSafeZoneBar.transform.SetParent(mainCanvas.transform, false);
            bannerSafeZoneBar.transform.SetAsLastSibling();

            Image bg = bannerSafeZoneBar.GetComponent<Image>();
            bg.color = new Color(0.03f, 0.05f, 0.08f, 0.98f);
            bg.raycastTarget = true;

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
            tmp.text = "ESPACIO PUBLICITARIO ADAPTATIVO (ADMOB)";
            tmp.fontSizeMin = 9f;
            tmp.fontSizeMax = 14f;
            tmp.enableAutoSizing = true;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.45f, 0.52f, 0.62f, 0.65f);
            tmp.raycastTarget = false;
        }

        ActualizarDimensionFranjaSegura();
    }

    private void ActualizarDimensionFranjaSegura()
    {
        if (bannerSafeZoneBar == null) return;

        float ratioAltura = alturaRelativaBannerActual;
        if (bannerView != null && Screen.height > 0)
        {
            float pxHeight = bannerView.GetHeightInPixels();
            if (pxHeight > 10f)
            {
                float safeBottom = Mathf.Max(0f, Screen.safeArea.yMin);
                ratioAltura = Mathf.Clamp((pxHeight + safeBottom + 8f) / Screen.height, 0.065f, 0.105f);
            }
        }

        RectTransform rt = bannerSafeZoneBar.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, ratioAltura);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        bannerSafeZoneBar.transform.SetAsLastSibling();
    }

    public float ObtenerAlturaRelativaZonaSeguraBanner()
    {
        return Mathf.Clamp(alturaRelativaBannerActual, 0.065f, 0.105f);
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
        AjustarCanvasesDeAnunciosEnPantalla();
    }

    private void RegistrarEventosAppOpen(AppOpenAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            anuncioPantallaCompletaActivo = true;
            OcultarBannerTemporalmente();
            AjustarCanvasesDeAnunciosEnPantalla();
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
            AjustarCanvasesDeAnunciosEnPantalla();
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

    public void IntentarTransicionVictoriaSiguienteNivel(Action avanzarNivelCallback, int comodinesRecompensa = 2)
    {
        contadorNivelesCompletados++;

        if (TieneIntersticialBonificadoListo())
        {
            MostrarPromptIntroductorioIntersticialBonificado(avanzarNivelCallback, comodinesRecompensa);
            return;
        }

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
            yield return new WaitForSecondsRealtime(1f);
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

        AjustarCanvasesDeAnunciosEnPantalla();
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

        GameObject card = new GameObject("ModalCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(rewardedInterstitialPromptPanel.transform, false);
        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.localScale = Vector3.one;
        cardRt.anchorMin = new Vector2(0.06f, 0.18f);
        cardRt.anchorMax = new Vector2(0.94f, 0.82f);
        cardRt.offsetMin = Vector2.zero;
        cardRt.offsetMax = Vector2.zero;

        Image cardBg = card.GetComponent<Image>();
        cardBg.color = new Color(0.10f, 0.15f, 0.23f, 1f);

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
        promptBodyText.fontSizeMin = 14f;
        promptBodyText.fontSizeMax = 28f;

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
        promptCountdownText.fontSizeMin = 13f;
        promptCountdownText.fontSizeMax = 24f;

        CrearBotonModal(
            card.transform,
            "BtnWatchNow",
            "🎬 Ver anuncio ahora (+2 ⚡ Comodines)",
            new Vector2(0.07f, 0.18f),
            new Vector2(0.93f, 0.30f),
            new Color(0.15f, 0.52f, 0.32f, 1f),
            AceptarYMostrarIntersticialBonificado
        );

        CrearBotonModal(
            card.transform,
            "BtnNoThanksOptOut",
            "✖ No, gracias (Continuar al siguiente nivel)",
            new Vector2(0.07f, 0.045f),
            new Vector2(0.93f, 0.155f),
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
        tmp.fontSizeMin = 12f;
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

        AjustarCanvasesDeAnunciosEnPantalla();
    }
}

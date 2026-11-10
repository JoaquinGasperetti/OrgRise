using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;

/// <summary>
/// Gestor Comercial de Monetización Google AdMob (v11.6.0) optimizado para alto eCPM y retención en Play Store.
/// Soporta los 5 formatos de ingresos:
/// 1. Anchored Adaptive Banner (siempre visible en zona segura inferior).
/// 2. App Open Ad (apertura en frío y regreso desde segundo plano).
/// 3. Intersticial Estándar (en transiciones naturales con frecuencia legal AdMob).
/// 4. Intersticial Bonificado (en cambio de nivel con modal opt-out vectorial de alta conversión).
/// 5. Video Bonificado Opt-In (para Auto-Pista Headhunter, Mejora a 3⭐, Salva-Racha y Recarga de Comodines).
/// </summary>
public class AdMobManager : MonoBehaviour
{
    public static AdMobManager Instance;

    [Header("ID de Aplicación AdMob")]
    public string appId = "ca-app-pub-2266949018056491~8163314986";

    [Header("Modo de Seguridad y Desarrollo")]
    [Tooltip("En Play Store (Release Build), poner modoPruebas = false para servir anuncios comerciales reales.")]
    public bool modoPruebas = true;

    private const string TestBannerId = "ca-app-pub-3940256099942544/6300978111";
    private const string TestInterstitialId = "ca-app-pub-3940256099942544/1033173712";
    private const string TestRewardedId = "ca-app-pub-3940256099942544/5224354917";
    private const string TestRewardedInterstitialId = "ca-app-pub-3940256099942544/5354046379";
    private const string TestAppOpenId = "ca-app-pub-3940256099942544/9257395921";

    public string BannerId => (modoPruebas || Application.isEditor || Debug.isDebugBuild) ? TestBannerId : bannerAdUnitId;
    public string InterstitialId => (modoPruebas || Application.isEditor || Debug.isDebugBuild) ? TestInterstitialId : interstitialAdUnitId;
    public string RewardedId => (modoPruebas || Application.isEditor || Debug.isDebugBuild) ? TestRewardedId : rewardedAdUnitId;
    public string RewardedInterstitialId => (modoPruebas || Application.isEditor || Debug.isDebugBuild) ? TestRewardedInterstitialId : rewardedInterstitialAdUnitId;
    public string AppOpenId => (modoPruebas || Application.isEditor || Debug.isDebugBuild) ? TestAppOpenId : appOpenAdUnitId;

    [Header("IDs de Bloques de Anuncios (Producción)")]
    public string bannerAdUnitId = "ca-app-pub-2266949018056491/9799830280";
    public string rewardedAdUnitId = "ca-app-pub-2266949018056491/2693038440";
    public string appOpenAdUnitId = "ca-app-pub-2266949018056491/6383329457";
    public string interstitialAdUnitId = "ca-app-pub-2266949018056491/4224069973";
    public string rewardedInterstitialAdUnitId = "ca-app-pub-2266949018056491/2347434413";

    [Header("Optimización de Frecuencia e Ingresos (AdMob Compliance)")]
    public int minAccionesParaIntersticial = 2;
    public float cooldownPantallaCompletaSegundos = 35f;
    public int segundosIntroIntersticialBonificado = 4;

    private BannerView bannerView;
    private InterstitialAd interstitialAd;
    private AppOpenAd appOpenAd;
    private RewardedAd rewardedAd;
    private RewardedInterstitialAd rewardedInterstitialAd;

    private bool sdkInicializado = false;
    private bool anuncioPantallaCompletaActivo = false;
    private bool mostrarAppOpenEnPrimerInicio = true;
    private bool bannerVisible = false;
    private DateTime appOpenHoraCarga = DateTime.MinValue;
    private float ultimoAnuncioPantallaCompletaTiempo = -999f;
    private float ultimoCierrePantallaCompletaTiempo = -999f;
    private int contadorAccionesTransicion = 0;
    private int contadorNivelesCompletados = 0;

    private int ultimoAnchoPantalla = 0;
    private int ultimoAltoPantalla = 0;
    private float alturaRelativaBannerActual = 0.068f;

    private GameObject bannerSafeZoneBar;
    private GameObject rewardedInterstitialPromptPanel;
    private TextMeshProUGUI promptCountdownText;
    private TextMeshProUGUI promptBodyText;
    private Coroutine promptCountdownCoroutine;
    private Action pendingTransitionCallback;
    private int pendingRewardAmount = 2;

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
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        AppStateEventNotifier.AppStateChanged += OnAppStateChanged;

        SolicitarConsentimientoUMPeInicializar();
    }

    private void SolicitarConsentimientoUMPeInicializar()
    {
        ConsentRequestParameters request = new ConsentRequestParameters();

        ConsentInformation.Update(request, (FormError error) =>
        {
            if (error != null)
            {
                InicializarSDKAdMob();
                return;
            }

            ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
            {
                if (ConsentInformation.CanRequestAds())
                {
                    InicializarSDKAdMob();
                }
            });
        });

        if (ConsentInformation.CanRequestAds())
        {
            InicializarSDKAdMob();
        }
    }

    private void InicializarSDKAdMob()
    {
        if (sdkInicializado) return;

        if (modoPruebas || Application.isEditor || Debug.isDebugBuild)
        {
            RequestConfiguration requestConfiguration = new RequestConfiguration
            {
                TestDeviceIds = new System.Collections.Generic.List<string>
                {
                    AdRequest.TestDeviceSimulator
                }
            };
            MobileAds.SetRequestConfiguration(requestConfiguration);
        }

        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            sdkInicializado = true;
            CargarYMostrarBanner();
            CargarAppOpenAd();
            CargarInterstitialAd();
            CargarRewardedAd();
            CargarRewardedInterstitialAd();
        });
    }

    void Update()
    {
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

    public void AjustarCanvasesDeAnunciosEnPantalla()
    {
        Canvas[] todosLosCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in todosLosCanvases)
        {
            if (c == null || c.gameObject == null) continue;
            string nombre = c.gameObject.name;

            if (nombre.StartsWith("ADAPTIVE") || nombre.StartsWith("BANNER") || nombre.StartsWith("SMART_BANNER") || nombre.StartsWith("LARGE_BANNER") || nombre.StartsWith("FULL_BANNER"))
            {
                NormalizarCanvasBanner(c);
            }
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
            if (img != null) img.preserveAspect = false;

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
            if (bgImg != null) bgImg.color = new Color(0.02f, 0.04f, 0.08f, 0.94f);
        }

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

    public void CargarYMostrarBanner()
    {
        if (!sdkInicializado) return;

        CrearFranjaSeguraBannerUI();

        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }

        AdSize adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
        bannerView = new BannerView(BannerId, adaptiveSize, AdPosition.Bottom);

        bannerView.OnBannerAdLoaded += () =>
        {
            bannerVisible = true;
            AjustarCanvasesDeAnunciosEnPantalla();
            ActualizarDimensionFranjaSegura();
        };

        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            bannerVisible = false;
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
            bg.color = new Color(0.03f, 0.05f, 0.09f, 0.98f);
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
            lineImg.color = new Color(0.22f, 0.34f, 0.52f, 0.85f);
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
            tmp.text = "ESPACIO PUBLICITARIO ADAPTATIVO";
            tmp.fontSizeMin = 9f;
            tmp.fontSizeMax = 14f;
            tmp.enableAutoSizing = true;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.45f, 0.52f, 0.62f, 0.55f);
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

    public void CargarAppOpenAd()
    {
        if (!sdkInicializado) return;

        if (appOpenAd != null)
        {
            appOpenAd.Destroy();
            appOpenAd = null;
        }

        AppOpenAd.Load(AppOpenId, new AdRequest(), (AppOpenAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null) return;

            appOpenAd = ad;
            appOpenHoraCarga = DateTime.UtcNow;
            RegistrarEventosAppOpen(appOpenAd);

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
            if (Time.realtimeSinceStartup - ultimoCierrePantallaCompletaTiempo < 5f) return;
            MostrarAppOpenAdSiDisponible();
        }
    }

    public void MostrarAppOpenAdSiDisponible()
    {
        if (anuncioPantallaCompletaActivo) return;
        if (Time.realtimeSinceStartup - ultimoAnuncioPantallaCompletaTiempo < cooldownPantallaCompletaSegundos) return;

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

    public void CargarInterstitialAd()
    {
        if (!sdkInicializado) return;

        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        InterstitialAd.Load(InterstitialId, new AdRequest(), (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null) return;
            interstitialAd = ad;
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

    public void CargarRewardedInterstitialAd()
    {
        if (!sdkInicializado) return;

        if (rewardedInterstitialAd != null)
        {
            rewardedInterstitialAd.Destroy();
            rewardedInterstitialAd = null;
        }

        RewardedInterstitialAd.Load(RewardedInterstitialId, new AdRequest(), (RewardedInterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null) return;
            rewardedInterstitialAd = ad;
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
                "<b>🎁 BONO DE INVERSIÓN CORPORATIVA</b>\n\n" +
                $"Mirando un breve anuncio antes del próximo nivel recibirás automáticamente <b>+{cantidadComodines} ⚡ Comodines de Reestructuración</b>.\n\n" +
                "<size=80%><color=#B0BEC5>Podés omitirlo tocando \"No, gracias\" sin afectar tu partida.</color></size>";
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
                        $"🎁 ¡Bono acreditado! Recibiste +{recompensa} ⚡ Comodines.",
                        new Color(0.10f, 0.50f, 0.32f, 0.96f),
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
        VectorUIKit.ApplyRoundedStyle(cardBg, new Color(0.10f, 0.15f, 0.24f, 1f), pill: false, addShadow: true);
        VectorUIKit.ApplyOutline(cardBg, VectorUIKit.AccentGold, 1.8f);

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
        promptCountdownText.color = VectorUIKit.AccentGold;
        promptCountdownText.enableAutoSizing = true;
        promptCountdownText.fontSizeMin = 13f;
        promptCountdownText.fontSizeMax = 24f;

        CrearBotonModal(
            card.transform,
            "BtnWatchNow",
            "🎬 Reclamar Ahora (+2 ⚡ Comodines)",
            new Vector2(0.07f, 0.18f),
            new Vector2(0.93f, 0.30f),
            VectorUIKit.AccentEmerald,
            AceptarYMostrarIntersticialBonificado
        );

        CrearBotonModal(
            card.transform,
            "BtnNoThanksOptOut",
            "✖ No, gracias (Continuar al nivel)",
            new Vector2(0.07f, 0.045f),
            new Vector2(0.93f, 0.155f),
            new Color(0.24f, 0.30f, 0.40f, 1f),
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
        VectorUIKit.ApplyRoundedStyle(img, colorFondo, pill: true, addShadow: true);

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

    public void CargarRewardedAd()
    {
        if (!sdkInicializado) return;

        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        RewardedAd.Load(RewardedId, new AdRequest(), (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null) return;
            rewardedAd = ad;
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
                    "⏳ Cargando video bonificado... Probá de nuevo en unos segundos.",
                    new Color(0.20f, 0.32f, 0.50f, 0.95f),
                    2.4f
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
                        $"🎉 ¡Bono acreditado! Recibiste +{cantidadComodines} ⚡ Comodín(es).",
                        new Color(0.10f, 0.48f, 0.30f, 0.96f),
                        2.8f
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

using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if ADMOB
using GoogleMobileAds.Api;
#endif

#if GAMEANALYTICS
using GameAnalyticsSDK;
#endif

public class Advertisement : MonoBehaviour
{
    private static Advertisement instance;

    public static Action<Placement> OnError;
    public static Action<Placement> OnLoaded;
    public static Action<Placement> OnOpen;
    public static Action<Placement> OnClose;
    public static Action<Placement> OnRewarded;
    public static Action<Placement> OnRewardedFailed;

    [SerializeField] bool isTest;
    [SerializeField] bool adapterDebug;

    [SerializeField] string androidAppKey;
    [SerializeField] string iosAppKey;

    [SerializeField] Placement[] placements;
    public Placement[] Placements => placements;

    public static bool skipInterstitial { get; private set; }

    static bool setConsent;
    static bool consentEnabled;

    static DateTime lastAdWatch;
    static DateTime lastRewardedAdWatch;

    public static int interstitialDelay;
    public static int rewardedDelay;

    string gameAnalyticsSDKName;

    Placement currentPlacement;

    GameManager _gameManager;

    public static bool IsInitialized { get; private set; }

    public static Placement GetPlacement(string placement)
    {
        var value = instance.placements.FirstOrDefault(x => x.placement.Equals(placement));

        if (value == null)
            Debug.Log($"Placement '{placement}' not found!");

        return value;
    }

    public static void Show(string placement)
    {
        GetPlacement(placement)?.Show();
    }

    public static void Show(string placement, Action<bool> success) 
    {
        var p = GetPlacement(placement);

        if (p != null)
        {
            if (instance.showAdCoroutine != null)
                instance.StopCoroutine(instance.showAdCoroutine);

            instance.showAdCoroutine = instance.StartCoroutine(instance.ShowCoroutine(p, result =>
            {
                success.Invoke(result);
            }));
        }
        else 
        {
            success.Invoke(false);
        }
    }

    [SerializeField] bool waitForShowAd;
    bool showAdResult;
    Coroutine showAdCoroutine;

    IEnumerator ShowCoroutine(Placement placement, Action<bool> success)
    {
        showAdResult = false;

        if (placement.IsReady())
        {
            waitForShowAd = true;
            placement.Show();
            yield return new WaitUntil(() => !waitForShowAd);

            success(showAdResult);
        }
        else
        {
            placement.Request();
            success(showAdResult);
        }
    }

    void OnApplicationPause(bool paused)
    {
#if IRONSOURCE
        IronSource.Agent.onApplicationPause(paused);
#endif

#if GAMEANALYTICS
		if (paused)
		{
			if (currentPlacement != null)
			{
				GameAnalytics.PauseTimer(currentPlacement);
			}
		}
		else
		{
			if (currentPlacement != null)
			{
				GameAnalytics.ResumeTimer(currentPlacement);
			}
		}
#endif
    }

    private void Awake()
    {
        instance = this;
    }

    public static void Initialize(GameManager gameManager) 
    {
        instance._gameManager = gameManager;

        instance.StartCoroutine(instance.InitializeCoroutine());
    }

    IEnumerator InitializeCoroutine()
    {
        yield return new WaitUntil(() => setConsent);

        Debug.Log($"Advertisement consent: {consentEnabled}");

#if ADMOB
        gameAnalyticsSDKName = "admob";

        RequestConfiguration requestConfiguration =
            new RequestConfiguration.Builder()
            .SetSameAppKeyEnabled(true).build();
        MobileAds.SetRequestConfiguration(requestConfiguration);

        MobileAds.Initialize(initStatus =>
        {
            IsInitialized = true;

            RequestAll();
        });
#endif

#if IRONSOURCE
        gameAnalyticsSDKName = "ironsource";

#if UNITY_ANDROID
        string appKey = androidAppKey;
#elif UNITY_IPHONE
        string appKey = instance.iosAppKey;
#else
		string appKey = "unexpected_platform";
#endif
        lastAdWatch = DateTime.Now.AddSeconds(-interstitialDelay);
        lastRewardedAdWatch = DateTime.Now.AddSeconds(-rewardedDelay);

        //Dynamic config example
        IronSourceConfig.Instance.setClientSideCallbacks(true);

        IronSource.Agent.setAdaptersDebug(adapterDebug);
        IronSource.Agent.setConsent(consentEnabled);

        string id = IronSource.Agent.getAdvertiserId();
        Debug.Log("IS Advertiser Id : " + id);

        Debug.Log("IS Validate integration...");
        IronSource.Agent.validateIntegration();
        Debug.Log(IronSource.unityVersion());

        // App tracking transparrency
        IronSourceEvents.onConsentViewDidAcceptEvent += (type) => { Debug.Log($"ConsentViewDidShowSuccessEvent {type}"); };
        IronSourceEvents.onConsentViewDidLoadSuccessEvent += (type) => { IronSource.Agent.showConsentViewWithType("pre"); };
        IronSourceEvents.onConsentViewDidShowSuccessEvent += (type) => { PlayerPrefs.SetInt("iosAppTrackingTransparrencyAccepted", 1); PlayerPrefs.Save(); };

        // Errors
        IronSourceEvents.onConsentViewDidFailToLoadWithErrorEvent += (type, error) => { Debug.LogWarning($"ConsentViewDidFailToLoadWithErrorEvent {error.getCode()} | {error.getDescription()}"); };
        IronSourceEvents.onConsentViewDidFailToShowWithErrorEvent += (type, error) => { Debug.LogWarning($"ConsentViewDidFailToShowWithErrorEvent {error.getCode()} | {error.getDescription()}"); };

        IronSourceEvents.onBannerAdLoadFailedEvent += (error) => { OnAdError(currentPlacement, $"{error.getCode()} | {error.getDescription()}"); };
        IronSourceEvents.onInterstitialAdLoadFailedEvent += (error) => { OnAdError(currentPlacement, $"InterstitialAdLoadFailedEvent {error.getCode()} | {error.getDescription()}"); };
        IronSourceEvents.onInterstitialAdShowFailedEvent += (error) => { OnAdError(currentPlacement, $"InterstitialAdShowFailedEvent {error.getCode()} | {error.getDescription()}"); };
        IronSourceEvents.onRewardedVideoAdShowFailedEvent += (error) => { OnAdError(currentPlacement, $"RewardedVideoAdShowFailedEvent {error.getCode()} | {error.getDescription()}"); };

        // Add Banner Events
        IronSourceEvents.onBannerAdLoadedEvent += () => { OnAdLoaded(currentPlacement); };
        IronSourceEvents.onBannerAdClickedEvent += () => { OnAdClicked(currentPlacement); };
        IronSourceEvents.onBannerAdScreenPresentedEvent += () => { OnAdOpen(currentPlacement); };
        IronSourceEvents.onBannerAdScreenDismissedEvent += () => { OnAdClose(currentPlacement); };
        IronSourceEvents.onBannerAdLeftApplicationEvent += () => { Debug.Log("BannerAdLeftApplicationEvent"); };

        // Add Interstitial Events
        IronSourceEvents.onInterstitialAdReadyEvent += () => { OnAdLoaded(currentPlacement); };
        IronSourceEvents.onInterstitialAdShowSucceededEvent += () => { };
        IronSourceEvents.onInterstitialAdClickedEvent += () => { OnAdClicked(currentPlacement); };
        IronSourceEvents.onInterstitialAdOpenedEvent += () => { OnAdOpen(currentPlacement); };
        IronSourceEvents.onInterstitialAdClosedEvent += () => { OnAdClose(currentPlacement); };

        //Add Rewarded Video Events
        IronSourceEvents.onRewardedVideoAdOpenedEvent += () => { OnAdOpen(currentPlacement); };
        IronSourceEvents.onRewardedVideoAdClosedEvent += () => { OnAdClose(currentPlacement); };
        IronSourceEvents.onRewardedVideoAdStartedEvent += () => { };
        IronSourceEvents.onRewardedVideoAdEndedEvent += () => { };
        IronSourceEvents.onRewardedVideoAdRewardedEvent += (placement) => { OnAdReward(currentPlacement); };
        IronSourceEvents.onRewardedVideoAdClickedEvent += (placement) => { OnAdClicked(currentPlacement); };

        // Revenue
        IronSourceEvents.onImpressionSuccessEvent += (impression) => 
        {
            if (impression != null && !string.IsNullOrEmpty(currentPlacement.placement))
            {
                var parameters = new Dictionary<string, object>();
                parameters.Add("ad_platform", "ironSource");
                parameters.Add("ad_source", impression.adNetwork);
                parameters.Add("ad_unit_name", currentPlacement);
                parameters.Add("ad_format", impression.instanceName);
                parameters.Add("currency", "USD");
                parameters.Add("value", impression.revenue);

                FirebaseManager.ReportEvent("ad_impression", parameters);

                var value = (decimal)impression.revenue;

                // Report revenue
                var revenue = new YandexAppMetricaRevenue(value, "USD");

                revenue.ProductID = currentPlacement.placement;
                revenue.Receipt = new YandexAppMetricaReceipt();

                AppMetrica.Instance.ReportRevenue(revenue);
                FirebaseManager.ReportRevenue(currentPlacement.placement, (double)value, "USD");

                Debug.Log($"IS Report revenue AdUnit: {currentPlacement} Value: {value} Currency: {"USD"}");
            }
        };

        //IronSource.Agent.init(appKey);
        IronSource.Agent.init(appKey, IronSourceAdUnits.REWARDED_VIDEO, IronSourceAdUnits.INTERSTITIAL, IronSourceAdUnits.BANNER);
        //IronSource.Agent.initISDemandOnly (appKey, IronSourceAdUnits.REWARDED_VIDEO, IronSourceAdUnits.INTERSTITIAL);
        // Set User ID For Server To Server Integration
        //IronSource.Agent.setUserId ("UserId");
        //IronSource.Agent.loadBanner(IronSourceBannerSize.BANNER, IronSourceBannerPosition.BOTTOM);
        IronSource.Agent.loadInterstitial();

#if UNITY_ANDROID && !UNITY_EDITOR
				AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				AndroidJavaObject currentActivity = up.GetStatic<AndroidJavaObject>("currentActivity");
				AndroidJavaClass client = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient");
				AndroidJavaObject adInfo = client.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", currentActivity);
		
				//advertisingIdClient.text = adInfo.Call<string>("getId").ToString();
				Debug.Log($"IRONSOURCE Android advertising ID: {adInfo.Call<string>("getId").ToString()}");
#endif

#if UNITY_IOS && !UNITY_EDITOR
				Application.RequestAdvertisingIdentifierAsync((string advertisingId, bool trackingEnabled, string error) =>
				{
					//advertisingIdClient.text = advertisingId;
					Debug.Log($"IRONSOURCE iOS advertising ID: {advertisingId}");
				});
#endif

        if (PlayerPrefs.GetInt("iosAppTrackingTransparrencyAccepted") <= 0)
            IronSource.Agent.loadConsentViewWithType("pre");

        IsInitialized = true;
#endif
    }

    public static void SetConsent(bool consent) 
    {
        setConsent = true;
        consentEnabled = consent;
    }

    public static void SkipInterstitial(bool skip) 
    {
        skipInterstitial = skip;
    }

    public static void RequestAll() 
    {
        if (!IsInitialized)
            return;

        foreach (var placement in instance.placements)
            placement.Request();
    }

    public static void HideAll() 
    {
        foreach (var placement in instance.placements)
            placement.Hide();
    }

    public static void Destroy()
    {
        foreach (var placement in instance.placements)
            placement.Destroy();
    }

    [Serializable]
    public class Placement
    {
        public string placement;

        [SerializeField] string ironsourcePlacement;
        [SerializeField] string androidId;
        [SerializeField] string iosId;
        public Type type;

        public int completeLevelsToShow;

        public enum Type
        {
            Banner,
            Interstitial,
            Rewarded
        }

        public string Id
        {
#if UNITY_ANDROID
            get => androidId;
#endif

#if UNITY_IOS
            get => iosId;
/*#else
            get => "";*/
#endif
        }

        BannerView banner;
        InterstitialAd interstitial;
        RewardedAd rewarded;

        public bool earnedReward;

        public bool IsDelayed() 
        {
            if (type == Type.Interstitial) return (DateTime.Now - lastAdWatch).TotalSeconds < interstitialDelay;
            else if (type == Type.Rewarded) return (DateTime.Now - lastRewardedAdWatch).TotalSeconds < rewardedDelay;
            else return false;
        }

        public bool IsReady() 
        {
            if (IsDelayed())
                return false;

            if (instance._gameManager.CompletedLevelsCount() + 1 < completeLevelsToShow)
                return false;

#if ADMOB
            switch (type)
            {
                case Type.Banner:
                    return banner != null;
                case Type.Interstitial:
                    return interstitial != null ? interstitial.IsLoaded() : false;
                case Type.Rewarded:
                    return rewarded != null ? rewarded.IsLoaded() : false;
            }
#endif

#if IRONSOURCE
            if(IronSource.Agent == null)
                    return false;

            switch (type) 
            {
                case Type.Banner:
                    return false;
                case Type.Interstitial:
                    return IronSource.Agent.isInterstitialReady();
                case Type.Rewarded:
                    return IronSource.Agent.isRewardedVideoAvailable();
            }
#endif

            return false;
        }

        public void Request() 
        {
            if (IsReady())
                return;

#if ADMOB
            AdRequest request = new AdRequest.Builder().Build();

            switch (type)
            {
                case Type.Banner:
                    if (banner == null) 
                    {
                        if (instance.isTest)
                            banner = new BannerView("ca-app-pub-3940256099942544/6300978111", AdSize.Banner, AdPosition.Bottom);
                        else
                            banner = new BannerView(Id, AdSize.Banner, AdPosition.Bottom);

                        banner.OnAdLoaded += (sender, args) => { Hide(); instance.OnAdLoaded(this); };
                        banner.OnAdFailedToLoad += (sender, args) => { instance.OnAdError(this, args.LoadAdError.GetMessage()); };
                        banner.OnAdOpening += (sender, args) => { instance.OnAdOpen(this); };
                        banner.OnAdClosed += (sender, args) => { instance.OnAdClose(this); };
                        banner.OnPaidEvent += (sender, args) => { OnPaid(Id, args.AdValue); };
                    }

                    banner.LoadAd(request);
                    break;
                case Type.Interstitial:
                    if (interstitial == null) 
                    {
                        if (instance.isTest)
                            interstitial = new InterstitialAd("ca-app-pub-3940256099942544/1033173712");
                        else
                            interstitial = new InterstitialAd(Id);

                        interstitial.OnAdLoaded += (sender, args) => { instance.OnAdLoaded(this); };
                        interstitial.OnAdFailedToLoad += (sender, args) => { instance.OnAdError(this, args.LoadAdError.GetMessage()); };
                        interstitial.OnAdFailedToShow += (sender, args) => { instance.OnAdError(this, args.AdError.GetMessage()); };
                        interstitial.OnAdOpening += (sender, args) => { instance.OnAdOpen(this); };
                        interstitial.OnAdClosed += (sender, args) => { instance.OnAdClose(this); };
                        interstitial.OnPaidEvent += (sender, args) => { OnPaid(Id, args.AdValue); };
                    }

                    interstitial.LoadAd(request);
                    break;
                case Type.Rewarded:
                    if (rewarded == null) 
                    {
                        if (instance.isTest)
                            rewarded = new RewardedAd("ca-app-pub-3940256099942544/5224354917");
                        else
                            rewarded = new RewardedAd(Id);

                        rewarded.OnAdLoaded += (sender, args) => { instance.OnAdLoaded(this); };
                        rewarded.OnAdOpening += (sender, args) => { instance.OnAdOpen(this); };
                        rewarded.OnAdFailedToLoad += (sender, args) => { instance.OnAdError(this, args.LoadAdError.GetMessage()); };
                        rewarded.OnAdFailedToShow += (sender, args) => { instance.OnAdError(this, args.AdError.GetMessage()); };
                        rewarded.OnUserEarnedReward += (sender, args) => { instance.OnAdReward(this); };
                        rewarded.OnAdClosed += (sender, args) => { instance.OnAdClose(this); };
                        rewarded.OnPaidEvent += (sender, args) => { OnPaid(Id, args.AdValue); };
                    }

                    rewarded.LoadAd(request);
                    break;
            }
#endif


#if IRONSOURCE
            switch (type) 
            {
                case Type.Banner:
                    break;
                case Type.Interstitial:
                    IronSource.Agent.loadInterstitial();
                    break;
                case Type.Rewarded:
                    break;
            }

#endif
        }

        public void Show() 
        {
            if (!IsReady())
                return;

            instance.currentPlacement = this;

#if ADMOB
            switch (type)
            {
                case Type.Banner:
                    banner.Show();
                    break;
                case Type.Interstitial:
                    interstitial.Show();
                    break;
                case Type.Rewarded:
                    earnedReward = false;
                    rewarded.Show();
                    break;
            }
#endif


#if IRONSOURCE
            switch (type)
            {
                case Type.Banner:
                    IronSource.Agent.displayBanner();
                    break;
                case Type.Interstitial:
                    IronSource.Agent.showInterstitial(ironsourcePlacement);
                    break;
                case Type.Rewarded:
                    IronSource.Agent.showRewardedVideo(ironsourcePlacement);
                    break;
            }
#endif
        }

        public void Hide() 
        {
#if ADMOB
            switch (type)
            {
                case Type.Banner:
                    banner?.Hide();
                    break;
            }
#endif


#if IRONSOURCE
            switch (type)
            {
                case Type.Banner:
                    IronSource.Agent.hideBanner();
                    break;
            }
#endif
        }

        public void Destroy()
        {
#if ADMOB
            banner?.Destroy();
            interstitial?.Destroy();
#endif


#if IRONSOURCE
            IronSource.Agent.destroyBanner();
#endif
        }

        private void OnPaid(string paidAdUnit, AdValue paidAdValue) 
        {
            var revenue = new YandexAppMetricaRevenue((decimal)(paidAdValue.Value / 1000000f), paidAdValue.CurrencyCode);

            revenue.ProductID = paidAdUnit;
            revenue.Receipt = new YandexAppMetricaReceipt();

            AppMetrica.Instance.ReportRevenue(revenue);
            FirebaseManager.ReportRevenue(paidAdUnit, paidAdValue.Value / 1000000f, paidAdValue.CurrencyCode);

            Debug.Log($"AdMob Report revenue AdUnit: {paidAdUnit} Value: {paidAdValue.Value / 1000000f} Currency: {paidAdValue.CurrencyCode}");
        }
    }

    private void OnAdError(Placement placement, string errorMessage) 
    {
        showAdResult = false;

        Debug.LogWarning($"OnAdError: {placement.placement} {errorMessage}");
        OnError?.Invoke(placement);

#if GAMEANALYTICS
		GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.RewardedVideo, gameAnalyticsSDKName, currentPlacement);
#endif
    }

    private void OnAdLoaded(Placement placement)
    {
        Debug.Log($"OnAdLoaded: {placement.placement}");
        OnLoaded?.Invoke(placement);
    }

    private void OnAdOpen(Placement placement)
    {
        showAdResult = true;

        Debug.Log($"OnAdOpen: {placement.placement}");
        OnOpen?.Invoke(placement);

#if GAMEANALYTICS
		GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType, gameAnalyticsSDKName, currentPlacement);
#endif
    }

    private void OnAdClicked(Placement placement)
    {
        Debug.Log($"OnAdClicked: {placement.placement}");

#if GAMEANALYTICS
		GameAnalytics.NewAdEvent(GAAdAction.Clicked, GAAdType, gameAnalyticsSDKName, currentPlacement);
#endif
    }

    private void OnAdClose(Placement placement)
    {
        Debug.Log($"OnAdClose: {placement.placement}");
        OnClose?.Invoke(placement);

        switch (placement.type) 
        {
            case Placement.Type.Interstitial:
                lastAdWatch = DateTime.Now;
                break;
            case Placement.Type.Rewarded:
                lastRewardedAdWatch = DateTime.Now;

                StopCoroutine("GetReward");
                StartCoroutine("GetReward", placement);
                break;
        }

        waitForShowAd = false;

        RequestAll();

#if GAMEANALYTICS
		long elapsedTime = GameAnalytics.StopTimer(currentPlacement);

		GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.RewardedVideo, gameAnalyticsSDKName, currentPlacement, elapsedTime);
#endif
    }

    WaitForSeconds checkRewardDelay = new WaitForSeconds(0.5f);

    IEnumerator GetReward(Placement placement)
    {
        yield return checkRewardDelay;

        if(placement.earnedReward) OnRewarded?.Invoke(placement);
        else OnRewardedFailed?.Invoke(placement);
    }

    private void OnAdReward(Placement placement)
    {
        Debug.Log($"OnAdReward: {placement.placement}");
        placement.earnedReward = true;

#if GAMEANALYTICS
		GameAnalytics.NewAdEvent(GAAdAction.RewardReceived, GAAdType.RewardedVideo, gameAnalyticsSDKName, currentPlacement);
#endif
    }
}
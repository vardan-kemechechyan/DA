/*
using UnityEngine;
using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;

#if GAMEANALYTICS
using GameAnalyticsSDK;
#endif

public class IronSourceManager : MonoBehaviour
{
	public static IronSourceManager Instance { get; private set; }

	[SerializeField] bool adapterDebug;
	[SerializeField] bool enableIntersttial;
	[SerializeField] bool enableRewarded;

	[SerializeField] private string androidAppKey;
	[SerializeField] private string iosAppKey;

	[SerializeField] AdPlacement[] placements;

	private AdPlacement GetPlacement(string placement)
	{
		return placements.FirstOrDefault(x => x.name.Equals(placement));
	}

	public int interstitialDelay = 1;
	public int rewardedDelay = 1;

	static DateTime LastAdWatch;
	static DateTime LastRewardedAdWatch;

	public bool isRewarded;

	public bool skipInterstitial;

	float elapsedTime;
	string gameAnalyticsSDKName = "ironsource";

	public bool editorTestInterstitialReady = true;
	public bool editorTestRewardedReady = true;
	public bool editorTestInterstitialSucces = true;
	public bool editorTestRewardedSucces = true;

	public bool IsAdOpen { get; private set; }

	public bool IsInterstitialReady(string placement)
	{
		if (!enableIntersttial)
			return false;

		var p = GetPlacement(placement);

		if (p == null)
		{
			Debug.LogWarning($"Placement {placement} not found!");
			return false;
		}

		if (_gameManager.CompletedLevelsCount() + 1 < p.completeLevelsToShow)
			return false;

		if ((DateTime.Now - LastAdWatch).TotalSeconds < interstitialDelay)
		{
			return false;
		}
		else
		{
#if UNITY_EDITOR
			return editorTestInterstitialReady;
#endif

#if !UNITY_EDITOR
			return IronSource.Agent != null ? IronSource.Agent.isInterstitialReady() : false;
#endif
		}
	}

	public bool IsRewardedReady(string placement)
	{
		if (!enableRewarded)
			return false;

		var p = GetPlacement(placement);

		if (p == null)
		{
			Debug.LogWarning($"Placement {placement} not found!");
			return false;
		}

		if (_gameManager.CompletedLevelsCount() + 1 < p.completeLevelsToShow)
			return false;

		if ((DateTime.Now - LastRewardedAdWatch).TotalSeconds < rewardedDelay)
		{
			return false;
		}
		else
		{
#if UNITY_EDITOR
			return editorTestRewardedReady;
#endif

#if !UNITY_EDITOR
			return IronSource.Agent != null ? IronSource.Agent.isRewardedVideoAvailable() : false;
#endif
		}
	}

	public static event OnShowAd OnShow;
	public delegate void OnShowAd();

	public static event OnEarnReward OnRewarded;
	public delegate void OnEarnReward();

	public static event OnFailedRewarded OnRewardedFailed;
	public delegate void OnFailedRewarded();

	private string currentPlacement;

	GameManager _gameManager;
	IAP _iap;

	bool setConsent;
	bool consentEnabled;

public void Initialize(GameManager gameManager, IAP iap)
	{
		Instance = this;

		_gameManager = gameManager;
		_iap = iap;

		StartCoroutine(WaitForSetConsent(true));
	}

	IEnumerator WaitForSetConsent(bool wait)
	{
		if (wait)
			yield return new WaitUntil(() => setConsent);
		else
			yield return null;

#if UNITY_ANDROID
		string appKey = androidAppKey;
#elif UNITY_IPHONE
        string appKey = iosAppKey;
#else
		string appKey = "unexpected_platform";
#endif
		LastAdWatch = DateTime.Now.AddSeconds(-interstitialDelay);
		LastRewardedAdWatch = DateTime.Now.AddSeconds(-rewardedDelay);

		//Dynamic config example
		IronSourceConfig.Instance.setClientSideCallbacks(true);

		IronSource.Agent.setAdaptersDebug(adapterDebug);
		IronSource.Agent.setConsent(consentEnabled);

		Debug.Log($"IRONSOURCE Enable consent: {consentEnabled}");

		string id = IronSource.Agent.getAdvertiserId();
		Debug.Log("IRONSOURCE Advertiser Id : " + id);

		Debug.Log("IRONSOURCE Validate integration...");
		IronSource.Agent.validateIntegration();

		Debug.Log("IRONSOURCE Unity version:" + IronSource.unityVersion());

		// App tracking transparrency
		IronSourceEvents.onConsentViewDidAcceptEvent += onConsentViewDidAcceptEvent;
		IronSourceEvents.onConsentViewDidFailToLoadWithErrorEvent += onConsentViewDidFailToLoadWithErrorEvent;
		IronSourceEvents.onConsentViewDidLoadSuccessEvent += onConsentViewDidLoadSuccessEvent;
		IronSourceEvents.onConsentViewDidFailToShowWithErrorEvent += onConsentViewDidFailToShowWithErrorEvent;
		IronSourceEvents.onConsentViewDidShowSuccessEvent += onConsentViewDidShowSuccessEvent;

		// Add Banner Events
		IronSourceEvents.onBannerAdLoadedEvent += BannerAdLoadedEvent;
		IronSourceEvents.onBannerAdLoadFailedEvent += BannerAdLoadFailedEvent;
		IronSourceEvents.onBannerAdClickedEvent += BannerAdClickedEvent;
		IronSourceEvents.onBannerAdScreenPresentedEvent += BannerAdScreenPresentedEvent;
		IronSourceEvents.onBannerAdScreenDismissedEvent += BannerAdScreenDismissedEvent;
		IronSourceEvents.onBannerAdLeftApplicationEvent += BannerAdLeftApplicationEvent;

		// Add Interstitial Events
		IronSourceEvents.onInterstitialAdReadyEvent += InterstitialAdReadyEvent;
		IronSourceEvents.onInterstitialAdLoadFailedEvent += InterstitialAdLoadFailedEvent;
		IronSourceEvents.onInterstitialAdShowSucceededEvent += InterstitialAdShowSucceededEvent;
		IronSourceEvents.onInterstitialAdShowFailedEvent += InterstitialAdShowFailedEvent;
		IronSourceEvents.onInterstitialAdClickedEvent += InterstitialAdClickedEvent;
		IronSourceEvents.onInterstitialAdOpenedEvent += InterstitialAdOpenedEvent;
		IronSourceEvents.onInterstitialAdClosedEvent += InterstitialAdClosedEvent;

		//Add Rewarded Video Events
		IronSourceEvents.onRewardedVideoAdOpenedEvent += RewardedVideoAdOpenedEvent;
		IronSourceEvents.onRewardedVideoAdClosedEvent += RewardedVideoAdClosedEvent;
		IronSourceEvents.onRewardedVideoAvailabilityChangedEvent += RewardedVideoAvailabilityChangedEvent;
		IronSourceEvents.onRewardedVideoAdStartedEvent += RewardedVideoAdStartedEvent;
		IronSourceEvents.onRewardedVideoAdEndedEvent += RewardedVideoAdEndedEvent;
		IronSourceEvents.onRewardedVideoAdRewardedEvent += RewardedVideoAdRewardedEvent;
		IronSourceEvents.onRewardedVideoAdShowFailedEvent += RewardedVideoAdShowFailedEvent;
		IronSourceEvents.onRewardedVideoAdClickedEvent += RewardedVideoAdClickedEvent;

		// Revenue
		IronSourceEvents.onImpressionSuccessEvent += ImpressionSuccessEvent;

		// SDK init
		Debug.Log("IRONSOURCE Init");
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
	}

	public void SetConsent(bool accept)
	{
		setConsent = true;
		consentEnabled = accept;
	}

	// Consent View was loaded successfully
	private void onConsentViewDidShowSuccessEvent(string consentViewType)
	{
		Debug.Log("IronSource onConsentViewDidShowSuccessEvent");
	}
	// Consent view was failed to load
	private void onConsentViewDidFailToShowWithErrorEvent(string consentViewType, IronSourceError error)
	{
		Debug.LogWarning($"IronSource onConsentViewDidFailToShowWithErrorEvent {error.getCode()} {error.getErrorCode()} {error.getDescription()}");
	}

	// Consent view was displayed successfully
	private void onConsentViewDidLoadSuccessEvent(string consentViewType)
	{
		IronSource.Agent.showConsentViewWithType("pre");
	}

	// Consent view was not displayed, due to error
	private void onConsentViewDidFailToLoadWithErrorEvent(string consentViewType, IronSourceError error)
	{
		Debug.LogWarning($"IronSource onConsentViewDidFailToLoadWithErrorEvent {error.getCode()} {error.getErrorCode()} {error.getDescription()}");
	}

	// The user pressed the Settings or Next buttons
	private void onConsentViewDidAcceptEvent(string consentViewType)
	{
		PlayerPrefs.SetInt("iosAppTrackingTransparrencyAccepted", 1);
		PlayerPrefs.Save();
	}

	private void ImpressionSuccessEvent(IronSourceImpressionData impressionData)
	{
		if (impressionData != null && !string.IsNullOrEmpty(currentPlacement))
		{
			var parameters = new Dictionary<string, object>();
			parameters.Add("ad_platform", "ironSource");
			parameters.Add("ad_source", impressionData.adNetwork);
			parameters.Add("ad_unit_name", currentPlacement);
			parameters.Add("ad_format", impressionData.instanceName);
			parameters.Add("currency", "USD");
			parameters.Add("value", impressionData.revenue);

			FirebaseManager.ReportEvent("ad_impression", parameters);

			var value = (decimal)impressionData.revenue;

			// Report revenue
			var revenue = new YandexAppMetricaRevenue(value, "USD");

			revenue.ProductID = currentPlacement;
			revenue.Receipt = new YandexAppMetricaReceipt();

			AppMetrica.Instance.ReportRevenue(revenue);
			FirebaseManager.ReportRevenue(currentPlacement, (double)value, "USD");

			Debug.Log($"AdMob Report revenue AdUnit: {currentPlacement} Value: {value} Currency: {"USD"}");
		}
	}

	void OnApplicationPause(bool paused)
	{
		Debug.Log("IRONSOURCE OnApplicationPause = " + paused);
		IronSource.Agent.onApplicationPause(paused);

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

	public void DestroyAd()
	{
#if !UNITY_EDITOR
		IronSource.Agent.destroyBanner();
#endif
	}

	public void ShowBanner()
	{
		if (_iap.IsNoAdsPurchased())
			return;

		IronSource.Agent.displayBanner();
	}

	public void HideBanner()
	{
		IronSource.Agent.hideBanner();
	}

	bool isLoadingInterstitial;

	public void RequestInterstitial()
	{
		if (!IronSource.Agent.isInterstitialReady())
		{
			OnShow?.Invoke();

			isLoadingInterstitial = true;

			IronSource.Agent.loadInterstitial();
		}
	}

	public void ShowInterstitial(string placement)
	{
		if (!IsInterstitialReady(placement))
			return;

		if (_iap.IsNoAdsPurchased())
			return;

		currentPlacement = placement;

		if (IronSource.Agent.isInterstitialReady())
		{
			IronSource.Agent.showInterstitial();
		}
		else
		{
			RequestInterstitial();
		}
	}

	public void ShowInterstitial(string placement, Action<bool> success)
	{
		if (_iap.IsNoAdsPurchased())
			return;

		var p = GetPlacement(placement);

		if (p == null)
		{
			Debug.LogWarning($"Placement {placement} not found!");
			success(false);
			return;
		}

		if (interstitialCoroutine != null)
			StopCoroutine(interstitialCoroutine);

		interstitialCoroutine = StartCoroutine(ShowAdCoroutine(placement, result =>
		{
			success.Invoke(result);
		}));
	}

	[SerializeField] bool waitForShowAd;
	bool interstitialResult;
	Coroutine interstitialCoroutine;

	IEnumerator ShowAdCoroutine(string placement, Action<bool> success)
	{
		interstitialResult = false;

		if (IsInterstitialReady(placement))
		{
			waitForShowAd = true;

#if UNITY_EDITOR
			success(editorTestInterstitialSucces);
			yield return null;
#endif

#if !UNITY_EDITOR
			ShowInterstitial(placement);
			yield return new WaitUntil(() => !waitForShowAd);

			success(interstitialResult);
#endif
		}
		else
		{
			success(interstitialResult);
		}
	}

	public void ShowRewardedVideo(string placement)
	{
		if (!IsRewardedReady(placement))
			return;

		currentPlacement = placement;

		earnedReward = false;

#if UNITY_EDITOR
		if (editorTestRewardedSucces)
			OnRewarded?.Invoke();
		else
			OnRewardedFailed?.Invoke();
#endif

#if !UNITY_EDITOR
		if (IronSource.Agent.isRewardedVideoAvailable())
		{
			OnShow?.Invoke();

			IronSource.Agent.showRewardedVideo();
		}
#endif
	}

	// Banner
	void BannerAdLoadedEvent()
	{
		Debug.Log("IRONSOURCE BannerAdLoadedEvent");

#if GAMEANALYTICS
		GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.Banner, gameAnalyticsSDKName, currentPlacement);
#endif

		AnalyticEvents.ReportEvent("banner_bottom");
	}

	void BannerAdLoadFailedEvent(IronSourceError error)
	{
		Debug.Log("IRONSOURCE BannerAdLoadFailedEvent, code: " + error.getCode() + ", description : " + error.getDescription());
	}

	void BannerAdClickedEvent()
	{
		Debug.Log("IRONSOURCE BannerAdClickedEvent");

#if GAMEANALYTICS
		GameAnalytics.NewAdEvent(GAAdAction.Clicked, GAAdType.Banner, gameAnalyticsSDKName, currentPlacement);
#endif
	}

	void BannerAdScreenPresentedEvent()
	{
		Debug.Log("IRONSOURCE BannerAdScreenPresentedEvent");
	}

	void BannerAdScreenDismissedEvent()
	{
		Debug.Log("IRONSOURCE BannerAdScreenDismissedEvent");
	}

	void BannerAdLeftApplicationEvent()
	{
		Debug.Log("IRONSOURCE BannerAdLeftApplicationEvent");
	}

	// Iterstitial
	void InterstitialAdReadyEvent()
	{
		Debug.Log("IRONSOURCE InterstitialAdReadyEvent");
	}

	void InterstitialAdLoadFailedEvent(IronSourceError error)
	{
		Debug.Log("IRONSOURCE InterstitialAdLoadFailedEvent, code: " + error.getCode() + ", description : " + error.getDescription());
	}

	void InterstitialAdShowSucceededEvent()
	{
		IsAdOpen = true;

		waitForShowAd = false;

		Debug.Log("IRONSOURCE InterstitialAdShowSucceededEvent");

#if GAMEANALYTICS
		GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.Interstitial, gameAnalyticsSDKName, currentPlacement);
#endif
	}

	void InterstitialAdShowFailedEvent(IronSourceError error)
	{
		IsAdOpen = false;

		waitForShowAd = false;

		Debug.Log("IRONSOURCE InterstitialAdShowFailedEvent, code :  " + error.getCode() + ", description : " + error.getDescription());

#if GAMEANALYTICS
		GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.Interstitial, gameAnalyticsSDKName, currentPlacement);
#endif
	}

	void InterstitialAdClickedEvent()
	{
		Debug.Log("IRONSOURCE InterstitialAdClickedEvent");

#if GAMEANALYTICS
		GameAnalytics.NewAdEvent(GAAdAction.Clicked, GAAdType.Interstitial, gameAnalyticsSDKName, currentPlacement);
#endif
	}

	void InterstitialAdOpenedEvent()
	{
		Debug.Log("IRONSOURCE InterstitialAdOpenedEvent");

		LastAdWatch = DateTime.Now;

		RequestInterstitial();

		if (currentPlacement.Equals("Interstitial_restart"))
		{
			var parameters = new Dictionary<string, object>();
			parameters.Add("location", _gameManager.Location.id);
			parameters.Add("level", _gameManager.Level + 1);

			AnalyticEvents.ReportEvent("restart_interstitial", parameters);
		}
		else
		{
			var lvl = new Dictionary<string, object>();
			lvl.Add("level", _gameManager.Level + 1);

			AnalyticEvents.ReportEvent("Interstitial", lvl);
		}
	}

	void InterstitialAdClosedEvent()
	{
		IsAdOpen = false;

		Debug.Log("IRONSOURCE InterstitialAdClosedEvent");
	}

	// Rewarded
	void RewardedVideoAvailabilityChangedEvent(bool canShowAd)
	{
		Debug.Log("IRONSOURCE RewardedVideoAvailabilityChangedEvent, value = " + canShowAd);
	}

	void RewardedVideoAdOpenedEvent()
	{
		IsAdOpen = true;

		LastRewardedAdWatch = DateTime.Now;

		Debug.Log("IRONSOURCE RewardedVideoAdOpenedEvent");

#if GAMEANALYTICS
		GameAnalytics.StartTimer(currentPlacement);
#endif
	}

	bool earnedReward;

	void RewardedVideoAdRewardedEvent(IronSourcePlacement ssp)
	{
		Debug.Log("IRONSOURCE RewardedVideoAdRewardedEvent, amount = " + ssp.getRewardAmount() + " name = " + ssp.getRewardName());

		earnedReward = true;

#if GAMEANALYTICS
		GameAnalytics.NewAdEvent(GAAdAction.RewardReceived, GAAdType.RewardedVideo, gameAnalyticsSDKName, currentPlacement);
#endif
	}

	void RewardedVideoAdClosedEvent()
	{
		IsAdOpen = false;

		Debug.Log("IRONSOURCE RewardedVideoAdClosedEvent");

#if GAMEANALYTICS
		long elapsedTime = GameAnalytics.StopTimer(currentPlacement);

		GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.RewardedVideo, gameAnalyticsSDKName, currentPlacement, elapsedTime);
#endif

		if (earnedReward)
			OnRewarded?.Invoke();
		else
			OnRewardedFailed?.Invoke();
	}

	void RewardedVideoAdStartedEvent()
	{
		Debug.Log("IRONSOURCE RewardedVideoAdStartedEvent");
	}

	void RewardedVideoAdEndedEvent()
	{
		Debug.Log("IRONSOURCE RewardedVideoAdEndedEvent");
	}

	void RewardedVideoAdShowFailedEvent(IronSourceError error)
	{
		IsAdOpen = false;

		Debug.Log("IRONSOURCE RewardedVideoAdShowFailedEvent, code :  " + error.getCode() + ", description : " + error.getDescription());

#if GAMEANALYTICS
		GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.RewardedVideo, gameAnalyticsSDKName, currentPlacement);
#endif

		OnRewardedFailed?.Invoke();
	}

	void RewardedVideoAdClickedEvent(IronSourcePlacement ssp)
	{
		Debug.Log("IRONSOURCE RewardedVideoAdClickedEvent, name = " + ssp.getRewardName());
	}

	[Serializable]
	public class AdPlacement
	{
		public string name;
		public int completeLevelsToShow;
	}
}
*/
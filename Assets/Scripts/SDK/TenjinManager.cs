using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_IOS
using UnityEngine.iOS;
#endif

public class TenjinManager : MonoBehaviour
{
    /*
    [SerializeField] string key;

    static BaseTenjin baseTenjin;

    public static bool IsInitialized() 
    {
        return baseTenjin != null;
    }

    public void Initialize()
    {
        baseTenjin = Tenjin.getInstance(key);

        TenjinConnect();
        baseTenjin.GetDeeplink(DeferredDeeplinkCallback);
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            baseTenjin = Tenjin.getInstance(key);
            TenjinConnect();
        }
    }

    public void DeferredDeeplinkCallback(Dictionary<string, string> data)
    {
        bool clicked_tenjin_link = false;
        bool is_first_session = false;

        if (data.ContainsKey("clicked_tenjin_link"))
        {
            //clicked_tenjin_link is a BOOL to handle if a user clicked on a tenjin link
            clicked_tenjin_link = (data["clicked_tenjin_link"].ToLower() == "true");
            Debug.Log("===> DeferredDeeplinkCallback ---> clicked_tenjin_link: " + clicked_tenjin_link);
        }

        if (data.ContainsKey("is_first_session"))
        {
            //is_first_session is a BOOL to handle if this session for this user is the first session
            is_first_session = (data["is_first_session"].ToLower() == "true");
            Debug.Log("===> DeferredDeeplinkCallback ---> is_first_session: " + is_first_session);
        }

        if (data.ContainsKey("ad_network"))
        {
            //ad_network is a STRING that returns the name of the ad network
            Debug.Log("===> DeferredDeeplinkCallback ---> adNetwork: " + data["ad_network"]);
        }

        if (data.ContainsKey("campaign_id"))
        {
            //campaign_id is a STRING that returns the tenjin campaign id
            Debug.Log("===> DeferredDeeplinkCallback ---> campaignId: " + data["campaign_id"]);
        }

        if (data.ContainsKey("advertising_id"))
        {
            //advertising_id is a STRING that returns the advertising_id of the user
            Debug.Log("===> DeferredDeeplinkCallback ---> advertisingId: " + data["advertising_id"]);
        }

        if (data.ContainsKey("deferred_deeplink_url"))
        {
            //deferred_deeplink_url is a STRING that returns the deferred_deeplink of the campaign
            Debug.Log("===> DeferredDeeplinkCallback ---> deferredDeeplink: " + data["deferred_deeplink_url"]);
        }

        if (clicked_tenjin_link && is_first_session)
        {
            //use the deferred_deeplink_url to direct the user to a specific part of your app
            if (String.IsNullOrEmpty(data["deferred_deeplink_url"]) == false)
            {
            }
        }
    }

    public void TenjinConnect()
    {
#if UNITY_IOS
      if (new Version(Device.systemVersion).CompareTo(new Version("14.0")) >= 0) {
        // Tenjin wrapper for requestTrackingAuthorization
        baseTenjin.RequestTrackingAuthorizationWithCompletionHandler((status) => {
          Debug.Log("===> App Tracking Transparency Authorization Status: " + status);

          // Sends install/open event to Tenjin
          baseTenjin.Connect();

        });
      }
      else {
          baseTenjin.Connect();
      }
#elif UNITY_ANDROID
        baseTenjin.Connect();
#endif
    }

    public static void ReportEvent(string name)
    {
        baseTenjin.SendEvent(name);
    }

    public void ReportEvent(string name, string value)
    {
        baseTenjin.SendEvent(name, value);
    }
    */
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Networking;
using System;

public class Loader : MonoBehaviour
{
    bool isCheckConnection;
    bool connection;

    AsyncOperation operation;

    [SerializeField] Slider progressBar;
    [SerializeField] GameObject connectionPopup;

    bool isLoading;
    bool isReady;

    // Progress values.
    private float currentValue;
    private float targetValue;

    float smoothPercentage = 0.4f;
    float smoothPercentageMultiplier = 0.025f;

    // Multiplier for progress animation speed.
    [SerializeField]
    [Range(0, 1)]
    private float progressAnimationMultiplier = 0.25f;

    void Start()
    {
        StartCoroutine(LoadAsyncronously());
        StartCoroutine("CheckConnection");
    }

    private void Update()
    {
        if (isLoading) 
        {
            targetValue = operation.progress / 0.9f;

            if (targetValue <= smoothPercentage)
                currentValue = Mathf.MoveTowards(currentValue, smoothPercentage, smoothPercentageMultiplier * Time.deltaTime);
            else
                currentValue = Mathf.MoveTowards(currentValue, targetValue, progressAnimationMultiplier * Time.deltaTime);

            progressBar.value = currentValue;

            if (isReady && Mathf.Approximately(currentValue, 1))
            {
                SceneManager.MoveGameObjectToScene(
                    FirebaseManager.GetInstance().gameObject, 
                    SceneManager.GetSceneByBuildIndex(SceneManager.GetActiveScene().buildIndex + 1));

                Debug.Log($"Startup load scene {Time.time} seconds");
                operation.allowSceneActivation = true;
            }
        }
    }

    WaitForSeconds checkConnectionTimeout = new WaitForSeconds(1.0f);

    IEnumerator CheckConnection()
    {
        yield return null;

        yield return new WaitUntil(() => FirebaseManager.IsInitialized);
        Debug.Log($"Startup Firebase initialized {Time.time} seconds");
        yield return new WaitUntil(() => FirebaseManager.IsFetchedRemoteConfig);
        Debug.Log($"Startup Firebase fetched config {Time.time} seconds");
        yield return new WaitUntil(() => operation != null);

        bool internetRequired = false;

        // Disabled on current release
        //internetRequired = FirebaseManager.GetRemoteConfigBoolean("no_internet");

        if (internetRequired)
        {
            while (true)
            {
                using (UnityWebRequest www = UnityWebRequest.Get($"https://google.com"))
                {
                    www.timeout = 3;
                    yield return www.SendWebRequest();

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        connectionPopup.SetActive(true);
                    }
                    else
                    {
                        ContinueLoading();
                    }

                    yield return checkConnectionTimeout;
                }
            }
        }
        else 
        {
            ContinueLoading();
        }
    }

    private void ContinueLoading() 
    {
        connectionPopup.SetActive(false);
        StopCoroutine("CheckConnection");

        isReady = true;
    }

    IEnumerator LoadAsyncronously()
    {
        yield return new WaitForEndOfFrame();

        progressBar.value = currentValue = targetValue = 0;

        var currentScene = SceneManager.GetActiveScene();
        operation = SceneManager.LoadSceneAsync(currentScene.buildIndex + 1);
        operation.allowSceneActivation = false;

        isLoading = true;
    }
}


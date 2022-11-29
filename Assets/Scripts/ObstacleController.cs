using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ObstacleController : MonoBehaviour
{
    public Color prevColor;
    public Color startColor;
    public Color endColor;
    public Color color;

    [SerializeField] MeshRenderer[] renderers;
    [SerializeField] Flamer[] flamers;

    int currentRenderer;
    bool enableRenderers;
    bool enableLaserSteps;
    float enableRendererTime;

    public GameObject clue;
    public GameObject key;
    public GameObject poison;
    public GameObject projector;

    int currentStep;

    [SerializeField] float enableMovableLasersDelay;
    [SerializeField] bool movableLasersLoop;

    [SerializeField] float enableByStepDelay = 0.25f;

    [SerializeField] float changeFlamerStateDelay;
    [SerializeField] float changeFlamersStateDelay;

    float enableByStepDelayTime;

    public Step[] laserSteps;

    [SerializeField] MovableLaser[] movableLasers;
    [SerializeField] SecurityCamera[] securityCameras;

    GameObject[] cameras;

    public void Init()
    {
        Combine();
        Show(false);

        projector.SetActive(true);
    }

    public void Combine() 
    {
        var m = new List<MeshRenderer>();
        var ml = new List<MovableLaser>();
        var co = new List<GameObject>();

        foreach (Transform t in transform)
        {
            if (!t.CompareTag("Clue"))
            {
                if (t.TryGetComponent(out MeshRenderer renderer))
                    m.Add(renderer);

                if (t.TryGetComponent(out MovableLaser movableLaser))
                {
                    movableLaser.loop = movableLasersLoop;

                    ml.Add(movableLaser);
                }
            }

            if (!t.CompareTag("CameraObject"))
            {
                co.Add(t.gameObject);
            }
        }

        renderers = m.ToArray();
        movableLasers = ml.ToArray();
        cameras = co.ToArray();

        var f = new List<Flamer>();
        var a = new List<Animation>();

        foreach (Transform t in transform)
        {
            if (!t.CompareTag("Clue"))
            {
                if (t.TryGetComponent(out Flamer flamer))
                    f.Add(flamer);
            }
        }

        flamers = f.ToArray();
    }

    void Update()
    {
        if (enableFlamers) 
        {
            if (!changeFlamersState)
            {
                changeFlamersStateTime += Time.deltaTime;

                if (changeFlamersStateTime >= changeFlamersStateDelay)
                    changeFlamersState = true;
            }
            else
            {
                if (changeFlamerStateIndex < flamers.Length)
                {
                    changeFlamerStateTime += Time.deltaTime;

                    if (changeFlamerStateTime >= changeFlamerStateDelay)
                    {
                        switch (flamers[changeFlamerStateIndex].mode)
                        {
                            case Flamer.Mode.Apears:
                                flamers[changeFlamerStateIndex].Enable(true);
                                changeFlamerStateTime = 0;
                                break;
                            case Flamer.Mode.Disapears:
                                flamers[changeFlamerStateIndex].Enable(false);
                                changeFlamerStateTime = 0;
                                break;
                        }

                        changeFlamerStateIndex++;
                    }
                }
                else 
                {
                    enableFlamers = false;
                }
            }
        }
    }

    void LateUpdate()
    {
        if (laserSteps != null && enableLaserSteps && laserSteps.Length > 1)
        {
            enableByStepDelayTime -= Time.deltaTime;

            if (currentStep < laserSteps.Length + 1 && enableByStepDelayTime <= 0) 
            {
                enableByStepDelayTime = enableByStepDelay;

                foreach (var s in laserSteps)
                    foreach (var l in s.lasers)
                        l.SetActive(false);

                for (int i = 0; i < currentStep; i++)
                {
                    foreach (var laser in laserSteps[i].lasers)
                        laser.SetActive(true);
                }

                currentStep++;
            }
        }

        if (prevColor != color)
        {
            prevColor = color;

            foreach (var l in renderers)
            {
                l.material.color = color;
            }
        }

        if (renderers.Length > 0)
        {
            if (enableRenderers)
            {
                enableRendererTime -= Time.unscaledDeltaTime;

                if (enableRendererTime <= 0)
                {
                    enableRendererTime = 0.025f;

                    renderers[currentRenderer].enabled = true;
                    currentRenderer++;

                    if (currentRenderer >= renderers.Length)
                    {
                        enableRenderers = false;
                    }
                }
            }
        }
    }

    public void Show(bool show) 
    {
        if(clue)
            clue.SetActive(show);

        if (key)
            key.SetActive(show);

        if (poison)
            poison.SetActive(show);

        foreach (var c in cameras)
            c.SetActive(show);

        foreach (var sc in securityCameras)
            sc.gameObject.SetActive(show);

        if (show)
        {
            currentRenderer = 0;
            enableRenderers = true;

            Invoke("ShowMovableLasers", enableMovableLasersDelay);
        }
        else 
        {
            foreach (var r in renderers)
                r.enabled = false;

            foreach (var s in laserSteps)
                foreach (var l in s.lasers)
                    l.SetActive(false);

            foreach (var ml in movableLasers)
                ml.gameObject.SetActive(false);
        }
    }

    private void ShowMovableLasers() 
    {
        enableLaserSteps = true;

        foreach (var ml in movableLasers)
            ml.gameObject.SetActive(true);
    }

    bool enableFlamers;
    bool changeFlamersState;
    bool changeFlamerState;
    int changeFlamerStateIndex;
    float changeFlamersStateTime;
    float changeFlamerStateTime;

    public void EnableFlamers() 
    {
        enableFlamers = true;
        changeFlamersState = false;
        changeFlamersStateTime = 0;
    }

    [Serializable]
    public class Step 
    {
        public GameObject[] lasers;
    }
}

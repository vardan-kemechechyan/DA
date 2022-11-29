using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraDeathRenderer : MonoBehaviour
{
    [SerializeField] Turret leftTurret;
    [SerializeField] Turret rightTurret;
    [SerializeField] ParticleSystem hitParticles;

    WaitForSeconds startDelay = new WaitForSeconds(0.5f);
    WaitForSeconds switchGunDelay = new WaitForSeconds(0.25f);

    void Start()
    {
        ShowTurrets(false);

        GameManager.OnChangeGameState += OnChangeGameState;
    }

    public void Play() 
    {
        ShowTurrets(true);

        StopCoroutine("PlayCoroutine");
        StartCoroutine("PlayCoroutine");
    }

    IEnumerator PlayCoroutine() 
    {
        yield return startDelay;
        leftTurret.Fire();
        rightTurret.Fire(0.15f);
        yield return switchGunDelay;
        leftTurret.Fire();
        rightTurret.Fire(0.15f);

        yield return switchGunDelay;
        hitParticles.Play();
        yield return switchGunDelay;
        hitParticles.Play();
    }

    private void ShowTurrets(bool show) 
    {
        leftTurret.transform.parent.gameObject.SetActive(show);
        rightTurret.transform.parent.gameObject.SetActive(show);
    }

    private void OnChangeGameState(GameState state, GameState previousState)
    {
        switch (state)
        {
            case GameState.Start:
                ShowTurrets(false);
                break;
        }
    }
}

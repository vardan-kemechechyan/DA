using System.Collections;
using UnityEngine;

public class Key : MonoBehaviour
{
    [SerializeField] ParticleSystem aura;
    [SerializeField] ParticleSystem splash;
    [SerializeField] GameObject key;

    bool enter;

    private void OnTriggerEnter(Collider other)
    {
        if (!enter && other.CompareTag("Player"))
        {
            enter = true;

            aura.Stop();
            aura.Clear();
            splash.Play();
            key.SetActive(false);

            GameManager.OnHitKey(this);
        }
    }
}
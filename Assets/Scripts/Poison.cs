using System.Collections;
using UnityEngine;

public class Poison : MonoBehaviour
{
    [SerializeField] ParticleSystem gass;
    [SerializeField] ParticleSystem splash;
    [SerializeField] GameObject can;

    public void Explode() 
    {
        Destroy(can);
        gass.Stop();
        gass.Clear();
        splash.Play();
    }
}
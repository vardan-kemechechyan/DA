using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour
{
    [SerializeField] Animation shotAnimation;
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform bulletParent;

    public void Fire(float delay) 
    {
        Invoke("Fire", delay);
    }

    public void Fire() 
    {
        shotAnimation.Play();
        Destroy(Instantiate(bulletPrefab, bulletParent), 1.0f);
    }
}

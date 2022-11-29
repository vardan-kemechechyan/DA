using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flamer : MonoBehaviour
{
    public Mode mode;
    public ParticleSystem fire;
    public BoxCollider[] triggers;

    public enum Mode 
    {
        Apears,
        Disapears,
        Active
    }

    public void Enable(bool enable) 
    {
        if (enable) 
        {
            fire.Play();
        }
        else
        {
            fire.Stop();
            fire.Clear();
        }

        foreach (var t in triggers)
            t.enabled = enable;
    }
}

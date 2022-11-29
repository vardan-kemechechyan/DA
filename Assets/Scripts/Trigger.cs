using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Trigger : MonoBehaviour
{
    public enum Type 
    {
        Laser,
        Flamer,
        Camera,
        Poison
    }

    public Type type;

    Transform player;

    bool enter;

    [SerializeField] UnityEvent onEnter;

    private void OnTriggerEnter(Collider other)
    {
        if (!enter && other.CompareTag("Player")) 
        {
            FirebaseManager.SetCustomKey("current_trigger", type.ToString());

            enter = true;
            onEnter.Invoke();
            GameManager.OnHitTrigger(this);
        }
    }
}

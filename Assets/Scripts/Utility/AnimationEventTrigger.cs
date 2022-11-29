using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AnimationEventTrigger : MonoBehaviour
{
    [SerializeField] UnityEvent animationEvent;

    public void OnAnimationEvent() 
    {
        animationEvent.Invoke();
    }
}

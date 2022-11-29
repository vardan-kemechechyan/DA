using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.UI
{
    public class AnimationEventProvider : MonoBehaviour
    {
        [SerializeField] UnityEvent<string> animationEvent;

        public void Event(string id)
        {
            this.animationEvent.Invoke(id);
        }
    }
}
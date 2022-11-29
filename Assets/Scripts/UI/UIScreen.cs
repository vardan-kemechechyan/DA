using System;
using UnityEngine;

namespace UI 
{
    [RequireComponent(typeof(Canvas))]
    public abstract class UIScreen : MonoBehaviour
    {
        private Canvas canvas;
        public Action OnClose;

        public bool IsOpen { get; private set; }

        public virtual void Open()
        {
            CacheCanvas();
            gameObject.SetActive(true);
            canvas.enabled = true;
            IsOpen = true;
        }

        public virtual void Close()
        {
            CacheCanvas();
            canvas.enabled = false;
            gameObject.SetActive(false);
            IsOpen = false;

            OnClose?.Invoke();
        }

        private void CacheCanvas()
        {
            if (canvas != null)
                return;

            canvas = GetComponent<Canvas>();
        }
    }
}
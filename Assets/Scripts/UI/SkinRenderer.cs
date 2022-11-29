using System.Collections;
using UnityEngine;

namespace UI
{
    public class SkinRenderer : MonoBehaviour
    {
        public Camera cam;
        [SerializeField] Transform root;
        Animator animator;

        public void Setup(RenderTexture renderTexture, GameObject skin, bool animated) 
        {
            cam.targetTexture = renderTexture;
            animator = Instantiate(skin, root).GetComponent<Animator>();

            Animate(animated);
        }

        public void Animate(bool animate)
        {
            if (animate)
            {
                animator.speed = 1;
                animator.Play("Idle");
            }
            else
            {
                animator.speed = 0;
            }
        }

        public void Play(string animation) 
        {
            animator.Play(animation, 0 - 1);
        }
    }
}
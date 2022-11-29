using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class ClueRenderer : MonoBehaviour
    {
        public Camera cam;

        GameObject clue;

        [SerializeField] Animation anim;
        List<Animation> animations = new List<Animation>();

        public void Show(GameObject clue)
        {
            Destroy(this.clue);
            animations.Clear();

            if (!clue) return;

            this.clue = Instantiate(clue, transform);

            anim = this.clue.GetComponent<Animation>();

            foreach (Transform t in this.clue.transform.GetChild(0))
                animations.Add(t.GetComponent<Animation>());

            StopCoroutine("CombineClue");
            StartCoroutine("CombineClue");
        }

        public void Show(GameObject clue, int part)
        {
            Destroy(this.clue);
            animations.Clear();

            if (!clue) return;

            this.clue = Instantiate(clue, transform);

            anim = this.clue.GetComponent<Animation>();

            var p = this.clue.transform.GetChild(0).GetChild(part);

            foreach (Transform t in this.clue.transform.GetChild(0)) 
            {
                if (t.Equals(p)) 
                {
                    p.localPosition = new Vector3(0, 0, 0);
                    animations.Add(p.GetComponent<Animation>());
                }
                else t.gameObject.SetActive(false);
            }

            StopCoroutine("CombineClue");
            StartCoroutine("CombineClue");
        }

        WaitForSeconds combineDelay = new WaitForSeconds(0.25f);

        IEnumerator CombineClue() 
        {
            foreach (var a in animations)
                a.transform.localScale = new Vector3(0, 0, 0);
            
            foreach (var a in animations) 
            {
                yield return combineDelay;
                a.Play("ClueCombined");
            }
            
            yield return combineDelay;
            yield return combineDelay;

            anim.Play();
        }
    }
}
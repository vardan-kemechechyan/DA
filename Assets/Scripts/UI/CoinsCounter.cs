using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class CoinsCounter : MonoBehaviour
    {
        [SerializeField] private Text counterText;

        bool count;
        float lerpTime;
        int countFrom;
        int countTo;
        float countDuration;

        private void LateUpdate()
        {
            if (count)
            {
                if (lerpTime < countDuration)
                {
                    lerpTime += Time.deltaTime;
                    counterText.text = Mathf.CeilToInt(Mathf.Lerp((float)countFrom, (float)countTo, lerpTime / countDuration)).ToString();
                }
                else
                {
                    counterText.text = countTo.ToString();
                    count = false;
                }
            }
        }

        public void SetCounter(int value) 
        {
            counterText.text = value.ToString();
        }

        public void UpdateCounter(int to)
        {
            int.TryParse(counterText.text, out int from);

            if (from != to)
            {
                CountTo(from, to, 1.0f);
            }
            else
            {
                counterText.text = to.ToString();
            }
        }

        public void UpdateCounter(int from, int to)
        {
            if (from != to)
            {
                CountTo(from, to, 1.0f);
            }
            else
            {
                counterText.text = to.ToString();
            }
        }

        private void CountTo(int from, int to, float duration) 
        {
            lerpTime = 0;
            countFrom = from;
            countTo = to;
            countDuration = duration;
            count = true;
        }
    }
}
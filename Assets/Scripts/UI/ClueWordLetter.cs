using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ClueWordLetter : MonoBehaviour
{
    [SerializeField] Animation anim;
    [SerializeField] Image background;
    [SerializeField] Outline outline;
    [SerializeField] Text symbol;

    public string Value { get; private set; }

    public void Set(string value)
    {
        this.Value = value;
        this.symbol.text = value;

        background.enabled = !value.Equals(" ");
    }

    public void Set(string value, Color background, bool outline) 
    {
        this.background.color = background;
        this.outline.enabled = outline;

        Set(value);
    }

    public void Shuffle() 
    {
        shuffle = true;
    }

    public void PlayAnimation() 
    {
        anim.Play();
    }

    float shuffleDuration = 1.0f;
    float shuffleDelay = 0.05f;
    float shuffleTime;
    float shuffleStepTime;
    bool shuffle;

    string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    void Update() 
    {
        if (shuffle) 
        {
            shuffleTime += Time.deltaTime;

            if (shuffleTime < shuffleDuration)
            {
                shuffleStepTime += Time.deltaTime;

                if (shuffleStepTime >= shuffleDelay)
                {
                    shuffleStepTime = 0;
                    symbol.text = chars[UnityEngine.Random.Range(0, chars.Length)].ToString();
                }
            }
            else 
            {
                shuffle = false;
                symbol.text = Value;
            }
        }
    }
}
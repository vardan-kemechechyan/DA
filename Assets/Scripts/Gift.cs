using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Gift : MonoBehaviour
{
    [SerializeField] Text label;
    [SerializeField] ParticleSystem splash;
    [SerializeField] GameObject gift;
    [SerializeField] GameObject final;

    [SerializeField] Sprite[] icons;

    bool hit;

    public bool IsFinal { get; private set; }

    public int Reward { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        if (!hit && other.CompareTag("Player"))
        {
            hit = true;

            Destroy(gift.gameObject);
            Destroy(final.gameObject);

            splash.Play();

            GameManager.OnGift(this);
        }
    }

    public void Setup(bool isFinal, int reward) 
    {
        IsFinal = isFinal;

        Reward = reward;
        label.text = $"+{reward}";

        gift.SetActive(!isFinal);
        final.SetActive(isFinal);
    }
}

using System.Collections;
using UnityEngine;

public class Clue : MonoBehaviour
{
    [SerializeField] GameObject shine;
    [SerializeField] ParticleSystem splash;
    [SerializeField] GameObject tutorialLeft;
    [SerializeField] GameObject tutorialRight;

    public GameObject part;

    bool enter;

    public bool isLeftSide;

    private void OnDestroy()
    {
        // Do not unparent if letters not pooled
        // Unparrent part to avoid destroy pooled item
        //if (part)
        //    part.transform.SetParent(null);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enter && other.CompareTag("Player")) 
        {
            enter = true;

            part.SetActive(false);
            shine.SetActive(false);
            splash.Play();

            ShowTutorial(false);

            GameManager.OnHitClue(this);
        }
    }

    public void ShowTutorial(bool show) 
    {
        tutorialLeft.SetActive(false);
        tutorialRight.SetActive(false);

        if (show) 
        {
            if (isLeftSide) tutorialLeft.SetActive(true);
            else tutorialRight.SetActive(true);
        }
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UI;

public class BonusGameScreen : UIScreen
{
    [SerializeField] Button noThanksButton;

    public override void Open()
    {
        base.Open();

        noThanksButton.gameObject.SetActive(false);
        Invoke("ShowNoThanks", 2.0f);
    }

    private void ShowNoThanks() 
    {
        noThanksButton.gameObject.SetActive(true);
    }
}

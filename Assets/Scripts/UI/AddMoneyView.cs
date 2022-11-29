using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AddMoneyView : MonoBehaviour
{
    Animation anim;
    [SerializeField] Text text;

    void Start()
    {
        anim = GetComponent<Animation>();
        anim.gameObject.SetActive(false);
    }

    public void AddMoney(int amount) 
    {
        text.text = $"+{amount}";

        anim.gameObject.SetActive(true);
        anim.Play();

        displayTime = 1.0f;
        addMoney = true;
    }

    bool addMoney;
    float displayTime;

    private void LateUpdate()
    {
        if (addMoney) 
        {
            displayTime -= Time.deltaTime;

            if (displayTime < 0) 
            {
                anim.Stop();
                anim.gameObject.SetActive(false);
            }
        }
    }
}

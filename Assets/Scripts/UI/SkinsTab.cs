using System;
using UnityEngine;
using UnityEngine.UI;

public class SkinsTab : MonoBehaviour
{
    [SerializeField] Image bacground;
    [SerializeField] Text label;
    [SerializeField] Animation anim;

    Configuration.Skin.Rarity rarity;
    public Configuration.Skin.Rarity Rarity => rarity;

    SkinsScreen screen;

    public void Init(SkinsScreen screen, Configuration.Skin.Rarity rarity, Color color)
    {
        this.screen = screen;
        this.rarity = rarity;
        label.text = rarity.ToString().ToUpper();
        bacground.color = color;
    }

    public void Expand(bool expand) 
    {
        if (expand) anim.Play("SkinTabExpand");
        else anim.Play("SkinTabIdle");
    }
}

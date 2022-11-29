using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RoadmapItem : MonoBehaviour
{
    [SerializeField] Image image;
    [SerializeField] RectTransform rectTransform;
    [SerializeField] Image eventImage;
    [SerializeField] Animation flashing;

    public bool IsGiftLevel { get; private set; }

    public void Set(bool isGiftLevel, Color color, float height, Sprite eventIcon, bool flashing = false)
    {
        IsGiftLevel = isGiftLevel;
        image.color = color;
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, height);
        eventImage.sprite = eventIcon;
        eventImage.enabled = eventIcon;

        if (flashing) this.flashing.Play();
        else this.flashing.Stop();
    }
}

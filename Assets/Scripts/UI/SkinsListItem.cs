using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SkinsListItem : MonoBehaviour
    {
        [SerializeField] string id;
        [SerializeField] RawImage icon;
        [SerializeField] Image background;
        [SerializeField] Image unknownIcon;
        [SerializeField] Text unlockLabel;
        [SerializeField] Outline mainOutline;

        [SerializeField] Animation animations;
        public Animation Animations => animations;

        [SerializeField] Configuration.Skin skin;
        public Configuration.Skin Skin => skin;

        [SerializeField] SkinRenderer skinRenderer;
        [SerializeField] GameObject shine;

        [SerializeField] GameObject lockIcon;
        [SerializeField] GameObject label;

        [SerializeField] Text counterInner;
        [SerializeField] Text counterOuter;

        GameData gameData;

        Color startColor;
        Color selectedColor;

        bool selected;

        public bool IsUnknown()
        {
            var skinData = gameData.GetSkin(skin.id);

            if (!skinData.unlocked && skin.rarity == Configuration.Skin.Rarity.Epic)
                return true;

            if (skin.rarity == Configuration.Skin.Rarity.Rare)
                return !gameData.GetSkin(skin.id).explored;
            else
            {
                if (gameData.GetSkin(skin.id).explored)
                    return false;
                else
                    return gameData.CompletedLevelsCount() < skin.levelsToUnlock;
            }
        }

        public void Init(GameData gameData, Configuration.Skin skin, 
            SkinRenderer renderer, RenderTexture renderTexture, Color color)
        {
            this.skin = skin;
            this.gameData = gameData;
            skinRenderer = renderer;
            icon.texture = renderTexture;
            startColor = background.color;
            selectedColor = color;

            UpdateItem();
        }

        public void UpdateItem()
        {
            var s = gameData.GetSkin(skin.id);
            int videosToUnlock = skin.adsToUnlock - s.watchedAds;

            shine.SetActive(selected);
            lockIcon.SetActive(false);
            unknownIcon.gameObject.SetActive(false);
            icon.gameObject.SetActive(false);
            unlockLabel.gameObject.SetActive(false);

            if (videosToUnlock <= 0)
                label.SetActive(false);
            else if (skin.adsToUnlock > 0)
                label.SetActive(true);

            //counterInner.text = $"X{videosToUnlock}";
            counterOuter.text = $"{gameData.GetSkin(skin.id).watchedAds}/{skin.adsToUnlock}";

            if (skin.rarity == Configuration.Skin.Rarity.Rare)
                unlockLabel.text = $"BONUS\nGAME";
            else
                unlockLabel.text = $"LVL {skin.levelsToUnlock}";

            unlockLabel.gameObject.SetActive(skin.rarity != Configuration.Skin.Rarity.Epic && IsUnknown());

            if (!s.unlocked)
            {
                unknownIcon.gameObject.SetActive(IsUnknown());
                icon.gameObject.SetActive(!IsUnknown());
            }
            else
            {
                icon.gameObject.SetActive(true);
            }

            if(!label.activeInHierarchy)
                lockIcon.SetActive(!s.unlocked);
        }

        public void Show(bool show) 
        {
            skinRenderer.gameObject.SetActive(show);
            gameObject.SetActive(show);
        }

        public void Select(bool select)
        {
            selected = select;

            if (select) background.color = selectedColor;
            else background.color = startColor;

            if (select) mainOutline.effectColor = Extensions.ToColor("#42D151");
            else mainOutline.effectColor = Color.white;

            shine.SetActive(select);
            skinRenderer.Animate(select);
        }

        public void Play(string animation)
        {
            skinRenderer.Play(animation);
        }
    }
}
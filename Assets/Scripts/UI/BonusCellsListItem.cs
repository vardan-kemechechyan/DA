using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class BonusCellsListItem : MonoBehaviour
    {
        public int id;
        [SerializeField] Image icon;
        [SerializeField] GameObject skin;
        [SerializeField] RawImage skinTexture;
        [SerializeField] Animation anim;
        [SerializeField] Text value;

        SkinRenderer skinRenderer;

        public Configuration.BonusCase Bonus;

        bool isOpen;

        GameManager _gameManager;

        public void Init(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void SetBonus(Configuration.BonusCase bonus) 
        {
            Bonus = bonus;
        }

        public void Click()
        {
            if (!isOpen && _gameManager.IsBonusCaseInteractable && _gameManager.RemainingKeys > 0) 
            {
                _gameManager.GetBonusCase(Bonus);

                icon.raycastTarget = false;

                value.text = Bonus.type == Configuration.BonusCase.Type.Money && Bonus.money > 0 ? Bonus.money.ToString() : "";

                icon.gameObject.SetActive(Bonus.type != Configuration.BonusCase.Type.Skin);
                skin.SetActive(Bonus.type == Configuration.BonusCase.Type.Skin);

                if (Bonus.type == Configuration.BonusCase.Type.Skin)
                {
                    if (skinRenderer != null)
                        Destroy(skinRenderer.gameObject);

                    skinRenderer = Instantiate(_gameManager.Config.skinRenderer).GetComponent<SkinRenderer>();

                    var position = skinRenderer.transform.position;
                    position.x = 100 + (10 * id);

                    skinRenderer.transform.position = position;

                    var renderTexture = new RenderTexture(256, 256, -1, RenderTextureFormat.ARGB32);

                    skinTexture.texture = renderTexture;
                    skinRenderer.Setup(renderTexture, _gameManager.CurrentUnlockedSkin.prefab, false);
                }
                else if (Bonus.type == Configuration.BonusCase.Type.Money) 
                {
                    icon.sprite = _gameManager.Config.bonusCaseIcons[(int)Bonus.type];
                }

                anim.Play("BonusCellOpen");

                isOpen = true;
            }
        }

        private void OnDisable()
        {
            if(skinRenderer != null)
                Destroy(skinRenderer.gameObject);

            Destroy(gameObject);
        }
    }
}

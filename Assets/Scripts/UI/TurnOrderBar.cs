using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class TurnOrderBar : MonoBehaviour
    {
        [SerializeField] private HorizontalLayoutGroup container;
        [SerializeField] private GameObject iconPrefab;

        [Header("Visuals")]
        [SerializeField] private Sprite fallbackSprite;
        [SerializeField] private bool showActiveFirst = true;

        [Header("Active Visual")]
        [SerializeField] private float activeImageScale = 1.15f;

        [Header("Fallback Colors (if no portrait)")]
        [SerializeField] private Color playerColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color enemyColor = new Color(0.2f, 1f, 0.2f);

        private TurnOrderController _turn;
        private CharacterScript _active;

        public void Bind(TurnOrderController turn)
        {
            if (_turn != null) _turn.ForecastChanged -= Redraw;
            _turn = turn;
            if (_turn != null) _turn.ForecastChanged += Redraw;
            Redraw();
        }

        public void SetActive(CharacterScript active)
        {
            _active = active;
            Redraw();
        }

        private void OnDestroy()
        {
            if (_turn != null) _turn.ForecastChanged -= Redraw;
        }

        private void Redraw()
        {
            if (!container) return;

            foreach (Transform c in container.transform) Destroy(c.gameObject);

            var list = new List<CharacterScript>();

            if (_turn != null && _turn.Forecast != null)
                list.AddRange(_turn.Forecast);

            // Ensure active is represented in the bar
            if (_active != null)
            {
                int idx = list.IndexOf(_active);
                if (idx < 0)
                {
                    // active not in forecast => inject it
                    if (showActiveFirst) list.Insert(0, _active);
                    else list.Add(_active);
                }
                else if (showActiveFirst && idx != 0)
                {
                    // move active to front if desired
                    list.RemoveAt(idx);
                    list.Insert(0, _active);
                }
            }

            foreach (var u in list)
                CreateIcon(u, u == _active);
        }

        private void CreateIcon(CharacterScript unit, bool isActive)
        {
            if (!unit) return;
            var go = Instantiate(iconPrefab, container.transform);
            go.transform.localScale = Vector3.one;
            var img = go.GetComponentInChildren<Image>();
            if (img)
            {
                var portrait = unit.GetPortrait();
                if (portrait)
                {
                    img.sprite = portrait;
                    img.color = Color.white;
                    img.preserveAspect = true;
                }
                else
                {
                    img.sprite = fallbackSprite;
                    img.color = unit.IsEnemy ? enemyColor : playerColor;
                }
            }

            img.transform.localScale = isActive ? Vector3.one * activeImageScale : Vector3.one;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class SPStripUI : MonoBehaviour
    {
        [Header("Slots container")]
        [SerializeField] private Transform container;
        [Header("Child names inside each slot")]
        [SerializeField] private string emptyChildName = "SP_Empty";
        [SerializeField] private string fullChildName = "SP_Full";

        [Header("Options")]
        [Tooltip("Discover slots automatically from children when binding.")]
        [SerializeField] private bool autoDiscover = true;

        private CharacterScript bound;

        private struct Slot
        {
            public GameObject root;
            public GameObject empty;
            public GameObject full;
            public bool warned;
        }

        private readonly List<Slot> slots = new(9);

        public void Bind(CharacterScript unit)
        {
            Unsubscribe();

            bound = unit;

            if (autoDiscover) DiscoverSlots();
            RefreshAll();

            if (bound != null)
            {
                bound.OnSPChanged += OnSPChanged;
                bound.OnHPChanged += OnSPChanged;
            }
        }

        private void OnDestroy() => Unsubscribe();

        private void Unsubscribe()
        {
            if (bound != null)
            {
                bound.OnSPChanged -= OnSPChanged;
                bound.OnHPChanged -= OnSPChanged;
            }
        }

        private void OnSPChanged(CharacterScript _) => RefreshAll();

        private void DiscoverSlots()
        {
            slots.Clear();
            if (!container) container = transform;

            for (int i = 0; i < container.childCount; i++)
            {
                var slotRootT = container.GetChild(i);
                if (!slotRootT) continue;

                Transform emptyT = null;
                if (!string.IsNullOrEmpty(emptyChildName))
                    emptyT = slotRootT.Find(emptyChildName);

                Transform fullT = null;
                if (!string.IsNullOrEmpty(fullChildName))
                {
                    fullT = slotRootT.Find(fullChildName);

                    if (!fullT && emptyT)
                        fullT = emptyT.Find(fullChildName);

                    if (!fullT)
                    {
                        foreach (var t in slotRootT.GetComponentsInChildren<Transform>(true))
                        {
                            if (t.name == fullChildName) { fullT = t; break; }
                        }
                    }
                }

                var slot = new Slot
                {
                    root = slotRootT.gameObject,
                    empty = emptyT ? emptyT.gameObject : null,
                    full = fullT ? fullT.gameObject : null,
                    warned = false
                };

                slots.Add(slot);
                if (slots.Count == CharacterScript.SP_CAP) break;
            }
        }

        private void RefreshAll()
        {
            if (slots.Count == 0) return;

            int max = bound ? Mathf.Clamp(bound.maxSP, 0, CharacterScript.SP_CAP) : 0;
            int cur = bound ? Mathf.Clamp(bound.currentSP, 0, max) : 0;

            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                bool withinMax = i < max;

                if (s.root) s.root.SetActive(withinMax);

                if (s.empty) s.empty.SetActive(withinMax);

                if (s.full) s.full.SetActive(withinMax && i < cur);

                if (withinMax && !s.warned && (!s.empty || !s.full))
                {
                    s.warned = true;
                    Debug.LogWarning(
                        $"[SPStripUI] Slot {i + 1} in '{container.name}' missing child(s). " +
                        $"Found Empty={(s.empty != null)} Full={(s.full != null)}. " +
                        $"Expected names: '{emptyChildName}' and '{fullChildName}'.",
                        container
                    );
                }

                slots[i] = s;
            }
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace DataChampions
{
    /// <summary>
    /// Code-built UI kit: rounded Procedural Image panels, chunky game-show buttons with a lip, TMP labels.
    /// Works in edit mode (the builder bakes the screens into the scene) and at runtime (diagrams, tiles).
    /// </summary>
    public static class UIFactory
    {
        public static readonly Vector2 Reference = new Vector2(1920, 1080);
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        public static Canvas Canvas(Transform parent, string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            c.pixelPerfect = false;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = Reference;
            // Expand: the whole 1920x1080 layout always fits (4:3 tablets, phones, ultra-wide);
            // extra space goes to the edges, where corner-anchored HUD pieces follow it.
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        /// <summary>A nested canvas: its children re-batch on their own, so animated parts don't dirty the rest.</summary>
        public static RectTransform SubCanvas(Transform parent, string name, bool interactive = true)
        {
            var rt = Stretch(parent, name);
            rt.gameObject.AddComponent<Canvas>();
            if (interactive) rt.gameObject.AddComponent<GraphicRaycaster>();
            return rt;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            try { module.AssignDefaultActions(); }
            catch (System.Exception e) { Debug.LogWarning("[DataChampions] UI input defaults: " + e.Message); }
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? Center;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name, float inset = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        /// <summary>Rounded rectangle; radius &lt; 0 makes a pill / circle.</summary>
        public static ProceduralImage Rounded(RectTransform rt, Color color, float radius, float border = 0f)
        {
            ProceduralImageModifier mod;
            if (radius < 0) mod = rt.gameObject.AddComponent<RoundModifier>();
            else mod = rt.gameObject.AddComponent<UniformModifier>();
            var img = rt.gameObject.AddComponent<ProceduralImage>();
            img.color = color;
            img.BorderWidth = border;
            img.FalloffDistance = 1.5f;
            img.raycastTarget = false;
            if (mod is UniformModifier um) um.Radius = radius;
            return img;
        }

        /// <summary>Rectangle with its own radius per corner (x top-left, y top-right, z bottom-right, w bottom-left).</summary>
        public static ProceduralImage Corners(RectTransform rt, Color color, Vector4 radius, float border = 0f)
        {
            var mod = rt.gameObject.AddComponent<FreeModifier>();
            var img = rt.gameObject.AddComponent<ProceduralImage>();
            img.color = color;
            img.BorderWidth = border;
            img.FalloffDistance = 1.5f;
            img.raycastTarget = false;
            mod.Radius = radius;
            return img;
        }

        public static ProceduralImage RoundedRect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, float radius, float border = 0f)
        {
            var rt = Rect(parent, name, anchor, pos, size);
            return Rounded(rt, color, radius, border);
        }

        /// <summary>Panel = root (position it) with a drop shadow, a lip and a face. Content goes under the returned face.</summary>
        public static ProceduralImage Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color face, Color lip, float radius, float lipDepth = 14f, bool shadow = true)
        {
            var rt = Rect(parent, name, anchor, pos, size);
            if (shadow)
            {
                var sh = Stretch(rt, "Shadow");
                sh.offsetMin = new Vector2(6, -lipDepth - 12); sh.offsetMax = new Vector2(6, -lipDepth - 12);
                Rounded(sh, Palette.UiShadow, radius);
            }
            if (lipDepth > 0)
            {
                var l = Stretch(rt, "Lip");
                l.offsetMin = new Vector2(0, -lipDepth); l.offsetMax = new Vector2(0, -lipDepth);
                Rounded(l, lip, radius);
            }
            var f = Stretch(rt, "Face");
            return Rounded(f, face, radius);
        }

        public static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool outline = false)
        {
            var rt = Stretch(parent, name);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            var cfg = GameConfig.Load();
            if (cfg != null && cfg.font != null)
            {
                t.font = cfg.font;
                if (outline && cfg.fontOutline != null) t.fontSharedMaterial = cfg.fontOutline;
            }
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.richText = true;
            return t;
        }

        /// <summary>Label in its own positioned rect.</summary>
        public static TextMeshProUGUI Text(Transform parent, string name, string text, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool outline = false)
        {
            var rt = Rect(parent, name, anchor, pos, size);
            return Label(rt, "T", text, fontSize, color, align, outline);
        }

        /// <summary>Wrapping text that shrinks to fit its rect.</summary>
        public static TextMeshProUGUI Paragraph(Transform parent, string name, string text, Vector2 anchor, Vector2 pos, Vector2 size, float maxSize, float minSize, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var t = Text(parent, name, text, anchor, pos, size, maxSize, color, align);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.enableAutoSizing = true;
            t.fontSizeMax = maxSize;
            t.fontSizeMin = minSize;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        public static Image Icon(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pos, float size, Color? color = null)
        {
            var rt = Rect(parent, name, anchor, pos, new Vector2(size, size));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = color ?? Color.white;
            img.raycastTarget = false;
            return img;
        }

        public static Image Icon(Transform parent, string name, string sprite, Vector2 pos, float size, Color? color = null) =>
            Icon(parent, name, GameConfig.Get(sprite), Center, pos, size, color);

        /// <summary>Chunky toy button: darker lip under a bright face, shine band, bouncy DOTween feedback.</summary>
        public static Button Button(Transform parent, string name, string text, Vector2 anchor, Vector2 pos, Vector2 size,
            Color face, Color lip, float fontSize = 64f, Sprite icon = null, float lipDepth = 12f)
        {
            var rt = Rect(parent, name, anchor, pos, size);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            float radius = Mathf.Min(size.x, size.y) * 0.32f;
            var lipRt = Stretch(rt, "Lip");
            lipRt.offsetMin = new Vector2(0, -lipDepth); lipRt.offsetMax = new Vector2(0, -lipDepth);
            Rounded(lipRt, lip, radius);
            var faceRt = Stretch(rt, "Face");
            Rounded(faceRt, face, radius);
            float shineH = Mathf.Min(size.y * 0.24f, 36f);
            var shine = Rect(faceRt, "Shine", new Vector2(0.5f, 1f), new Vector2(0, -7), new Vector2(size.x - Mathf.Max(34f, radius * 1.2f), shineH), new Vector2(0.5f, 1f));
            Rounded(shine, new Color(1, 1, 1, 0.22f), shineH * 0.5f);

            float textLeft = 14f;
            if (icon != null)
            {
                float s = size.y * 0.66f;
                var ic = Icon(faceRt, "Icon", icon, new Vector2(0, 0.5f), new Vector2(size.y * 0.52f + 4f, 2f), s);
                textLeft = size.y * 0.9f;
                if (string.IsNullOrEmpty(text))
                {
                    ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = Center;
                    ic.rectTransform.anchoredPosition = new Vector2(0, 2);
                }
            }
            if (!string.IsNullOrEmpty(text))
            {
                var label = Label(faceRt, "Label", text, fontSize, Color.white, TextAlignmentOptions.Center, true);
                label.rectTransform.offsetMin = new Vector2(textLeft, 6);
                label.rectTransform.offsetMax = new Vector2(-14, 0);
            }
            var b = rt.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = hit;
            var fx = rt.gameObject.AddComponent<UIButtonFX>();
            fx.face = faceRt;
            return b;
        }

        /// <summary>Round icon-only button.</summary>
        public static Button RoundButton(Transform parent, string name, Sprite icon, Vector2 anchor, Vector2 pos, float diameter, Color face, Color lip)
        {
            var b = Button(parent, name, null, anchor, pos, new Vector2(diameter, diameter), face, lip, 40, icon, 9f);
            foreach (var p in b.GetComponentsInChildren<ProceduralImage>(true))
            {
                if (p.name == "Shine") { p.gameObject.SetActive(false); continue; }
                var um = p.GetComponent<UniformModifier>();
                if (um != null) um.Radius = diameter * 0.5f;
            }
            var ic = b.transform.Find("Face/Icon") as RectTransform;
            if (ic != null) ic.sizeDelta = Vector2.one * diameter * 0.56f;
            return b;
        }

        public static void SetButtonColors(Button b, Color face, Color lip)
        {
            var f = b.transform.Find("Face")?.GetComponent<ProceduralImage>();
            var l = b.transform.Find("Lip")?.GetComponent<ProceduralImage>();
            if (f != null) f.color = face;
            if (l != null) l.color = lip;
        }

        public static TextMeshProUGUI ButtonLabel(Button b) => b.transform.Find("Face/Label")?.GetComponent<TextMeshProUGUI>();

        public static Image Dim(Transform parent, float alpha)
        {
            var rt = Stretch(parent, "Dim");
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.06f, 0.02f, 0.18f, alpha);
            return img;
        }

        /// <summary>
        /// Edovu logo card in the bottom-right corner: the crisp pre-scaled sprite on a soft white card so the small
        /// tagline stays readable over the dark studio. Falls back to the original prefab if the sprite is missing.
        /// </summary>
        public static RectTransform EdovuLogo(Transform parent, GameObject prefab, Sprite crisp, float size = 132f)
        {
            const float Pad = 8f;
            if (crisp != null)
            {
                var card = Rect(parent, "EdovuLogo", new Vector2(1f, 0f), new Vector2(-14f, 14f), new Vector2(size + Pad * 2, size + Pad * 2), new Vector2(1f, 0f));
                var sh = Stretch(card, "Shadow");
                sh.offsetMin = new Vector2(0, -5); sh.offsetMax = new Vector2(0, -5);
                Rounded(sh, Palette.UiShadow, 24);
                Rounded(Stretch(card, "Card"), new Color(1f, 1f, 1f, 0.96f), 24);
                Icon(card, "Logo", crisp, Center, Vector2.zero, size);
                return card;
            }
            if (prefab == null) return null;
            var rt = (RectTransform)Object.Instantiate(prefab, parent, false).transform;
            rt.name = "EdovuLogo";
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-12f, 12f);
            foreach (var g in rt.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            return rt;
        }

        public static string FormatTime(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            if (s < 60) return s + "s";
            if (s < 3600) return (s / 60) + "m " + (s % 60) + "s";
            return (s / 3600) + "h " + (s / 60 % 60) + "m";
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
        }
    }
}

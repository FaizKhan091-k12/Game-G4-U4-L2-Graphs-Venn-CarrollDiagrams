using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DataChampions
{
    /// <summary>
    /// Draws the nine diagrams from the lesson images (the same data, redrawn as clean, bright game boards).
    /// Coordinates are in the 1000 x 740 diagram space, origin in the middle, y up.
    /// </summary>
    public static class DiagramFactory
    {
        const float W = 1000f, H = 740f;

        static DiagramView view;
        static RectTransform structure;   // tables/grids (fade in first)
        static RectTransform layer;       // items on top

        public static DiagramView Build(DiagramId id, RectTransform parent)
        {
            var host = UIFactory.Rect(parent, "Diagram_" + id, UIFactory.Center, Vector2.zero, new Vector2(W, H));
            host.gameObject.AddComponent<Canvas>();
            view = host.gameObject.AddComponent<DiagramView>();
            view.content = UIFactory.Stretch(host, "Content");
            structure = UIFactory.Stretch(view.content, "Structure");
            layer = UIFactory.Stretch(view.content, "Items");
            view.overlay = UIFactory.Stretch(host, "Overlay");
            view.AddIntro(structure, DiagramView.IntroKind.Fade, 0f);

            switch (id)
            {
                case DiagramId.Cookies: Cookies(); break;
                case DiagramId.Vegetables: Vegetables(); break;
                case DiagramId.VennNumbers: Venn(); break;
                case DiagramId.BallsBar: Bars(); break;
                case DiagramId.Trees: Trees(); break;
                case DiagramId.SeaAnimals: Sea(); break;
                case DiagramId.CarrollNumbers: CarrollNumbers(); break;
                case DiagramId.CarrollNames: CarrollNames(); break;
                case DiagramId.CarrollShapes: CarrollShapes(); break;
            }
            var v = view;
            view = null;
            return v;
        }

        // ------------------------------------------------------------------ drawing helpers

        static Rect R(float cx, float cy, float w, float h) => new Rect(cx - w / 2f, cy - h / 2f, w, h);
        static Rect LTRB(float l, float t, float r, float b) => new Rect(l, b, r - l, t - b);

        static RectTransform Box(Transform p, string name, Rect r, Color fill, float radius, float border = 0f)
        {
            var rt = UIFactory.Rect(p, name, UIFactory.Center, r.center, r.size);
            UIFactory.Rounded(rt, fill, radius, border);
            return rt;
        }

        static RectTransform BoxC(Transform p, string name, Rect r, Color fill, Vector4 radius)
        {
            var rt = UIFactory.Rect(p, name, UIFactory.Center, r.center, r.size);
            UIFactory.Corners(rt, fill, radius);
            return rt;
        }

        static void HLine(Transform p, float y, float x0, float x1, float thick, Color c) =>
            Box(p, "HLine", LTRB(x0, y + thick / 2, x1, y - thick / 2), c, 0f);

        static void VLine(Transform p, float x, float y0, float y1, float thick, Color c) =>
            Box(p, "VLine", LTRB(x - thick / 2, Mathf.Max(y0, y1), x + thick / 2, Mathf.Min(y0, y1)), c, 0f);

        static TextMeshProUGUI Txt(Transform p, string text, Vector2 pos, Vector2 size, float fontSize, Color c, bool outline = false,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var t = UIFactory.Text(p, "Text", text, UIFactory.Center, pos, size, fontSize, c, align, outline);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.enableAutoSizing = true;
            t.fontSizeMax = fontSize;
            t.fontSizeMin = fontSize * 0.6f;
            return t;
        }

        static RectTransform Pic(Transform p, string sprite, Vector2 pos, float size, float rot = 0f)
        {
            var img = UIFactory.Icon(p, sprite, GameConfig.Get(sprite), UIFactory.Center, pos, size);
            img.rectTransform.localRotation = Quaternion.Euler(0, 0, rot);
            return img.rectTransform;
        }

        /// <summary>Picture item: registered in a group and popped in during the intro.</summary>
        static RectTransform Item(string group, string sprite, Vector2 pos, float size, float delay, float rot = 0f)
        {
            var rt = Pic(layer, sprite, pos, size, rot);
            view.AddItem(group, rt);
            view.AddIntro(rt, DiagramView.IntroKind.Pop, delay, true);
            return rt;
        }

        static void Title(string text, float y, Color c)
        {
            var rt = UIFactory.Rect(structure, "Title", UIFactory.Center, new Vector2(0, y), new Vector2(520, 68));
            var lip = UIFactory.Stretch(rt, "Lip");
            lip.offsetMin = new Vector2(0, -7); lip.offsetMax = new Vector2(0, -7);
            UIFactory.Rounded(lip, Color.Lerp(c, Color.black, 0.25f), 34);
            UIFactory.Rounded(UIFactory.Stretch(rt, "Face"), c, 34);
            var tl = UIFactory.Label(rt, "T", text, 40, Color.white, TextAlignmentOptions.Center, true);
            tl.rectTransform.offsetMin = new Vector2(0, 4);
            var w = tl.GetPreferredValues(text);
            rt.sizeDelta = new Vector2(Mathf.Max(320f, w.x + 90f), 68f);
        }

        static void Table(Rect r)
        {
            Box(structure, "TableShadow", new Rect(r.x + 6, r.y - 10, r.width, r.height), Palette.UiShadow, 22f);
            Box(structure, "Table", r, Palette.Paper, 22f);
        }

        static void TableBorder(Rect r) => Box(structure, "Border", r, Palette.Line, 22f, 5f);

        static void Header(Rect r, string text, Color c, Vector4 radius, float fontSize = 38f)
        {
            var rt = BoxC(structure, "Header", r, c, radius);
            if (!string.IsNullOrEmpty(text)) Txt(rt, text, Vector2.zero, r.size - new Vector2(20, 8), fontSize, Color.white, true);
        }

        static void KeyBox(Vector2 pos, string sprite, string text)
        {
            var r = R(pos.x, pos.y, 470, 86);
            var rt = Box(structure, "Key", r, Palette.Hex(0xFFF1CF), 24f);
            Box(structure, "KeyBorder", r, Palette.Gold, 24f, 5f);
            Txt(rt, "Key:", new Vector2(-160, 0), new Vector2(120, 60), 38, Palette.Ink);
            Pic(rt, sprite, new Vector2(-62, 0), 66);
            Txt(rt, text, new Vector2(100, 0), new Vector2(250, 60), 38, Palette.Ink, false, TextAlignmentOptions.Left);
            view.AddRegion("key", r);
        }

        // ------------------------------------------------------------------ 1. favourite cookie pictogram

        static void Cookies()
        {
            Title("Favourite cookie", 330, Palette.Purple);
            var table = LTRB(-470, 285, 470, -195);
            Table(table);
            Box(structure, "RowTint1", LTRB(-470, 105, 470, 5), Palette.RowTint, 0f);
            BoxC(structure, "RowTint3", LTRB(-470, -95, 470, -195), Palette.RowTint, new Vector4(0, 0, 22, 22));
            Header(LTRB(-470, 285, 470, 205), null, Palette.HeaderA, new Vector4(22, 22, 0, 0));
            Txt(structure, "Cookie", new Vector2(-300, 245), new Vector2(320, 70), 38, Color.white, true);
            Txt(structure, "Number of students", new Vector2(170, 245), new Vector2(560, 70), 38, Color.white, true);
            for (int i = 1; i < 4; i++) HLine(structure, 205 - i * 100, -470, 470, 3, Palette.Line);
            VLine(structure, -130, 285, -195, 3, Palette.Line);
            TableBorder(table);

            string[] names = { "Peanut butter", "Chocolate chip", "Ginger snap", "Animal cracker" };
            int[] counts = { 5, 4, 2, 3 };
            float d = 0.25f;
            for (int row = 0; row < 4; row++)
            {
                float y = 155 - row * 100;
                var lbl = Txt(layer, names[row], new Vector2(-300, y), new Vector2(320, 80), 38, Palette.Ink);
                view.AddIntro(lbl.rectTransform.parent as RectTransform, DiagramView.IntroKind.Pop, 0.15f + row * 0.06f);
                for (int k = 0; k < counts[row]; k++)
                {
                    Item("cookies", "fl_cookie", new Vector2(-60 + k * 106, y), 86, d, Random.Range(-10f, 10f));
                    d += 0.05f;
                }
            }
            view.AddRegion("icons", LTRB(-130, 205, 470, -195));
            KeyBox(new Vector2(0, -282), "fl_cookie", "= 2 students");
        }

        // ------------------------------------------------------------------ 2. green / not green sorting table

        static void Vegetables()
        {
            var table = LTRB(-470, 300, 470, -300);
            Table(table);
            Header(LTRB(-470, 300, 0, 196), null, Palette.HeaderC, new Vector4(22, 0, 0, 0));
            Header(LTRB(0, 300, 470, 196), null, Palette.HeaderB, new Vector4(0, 22, 0, 0));
            Txt(structure, "Green vegetables", new Vector2(-235, 248), new Vector2(440, 80), 40, Color.white, true);
            Txt(structure, "Not green vegetables", new Vector2(235, 248), new Vector2(440, 80), 40, Color.white, true);
            VLine(structure, 0, 300, -300, 4, Palette.Line);
            TableBorder(table);

            string[] green = { "fl_broccoli", "fl_leafy_green", "fl_cucumber", "fl_bell_pepper" };
            string[] notGreen = { "fl_onion", "fl_garlic", "fl_eggplant", "fl_carrot" };
            Vector2[] spots = { new Vector2(-105, 110), new Vector2(100, 95), new Vector2(-95, -115), new Vector2(110, -125) };
            float[] rots = { -8f, 6f, 10f, -5f };
            float d = 0.3f;
            for (int i = 0; i < 4; i++) { Item("green", green[i], new Vector2(-235, -52) + spots[i], 168, d, rots[i]); d += 0.08f; }
            for (int i = 0; i < 4; i++) { Item("notgreen", notGreen[i], new Vector2(235, -52) + spots[(i + 1) % 4], 168, d, -rots[i]); d += 0.08f; }
            view.AddRegion("colGreen", LTRB(-470, 300, 0, -300));
            view.AddRegion("colNot", LTRB(0, 300, 470, -300));
        }

        // ------------------------------------------------------------------ 3/7. Venn diagram

        static void Venn()
        {
            var frame = LTRB(-485, 348, 485, -372);
            Table(frame);
            TableBorder(frame);
            var lt = Txt(layer, "Odd numbers", new Vector2(-250, 300), new Vector2(420, 60), 38, Palette.BlueLip);
            var rtx = Txt(layer, "Numbers greater than 10", new Vector2(245, 300), new Vector2(470, 60), 38, Palette.PinkLip);
            view.AddIntro(lt.rectTransform.parent as RectTransform, DiagramView.IntroKind.Pop, 0.35f);
            view.AddIntro(rtx.rectTransform.parent as RectTransform, DiagramView.IntroKind.Pop, 0.45f);

            const float r = 265f, cy = -20f, cxo = 155f;
            var left = UIFactory.Rect(structure, "CircleL", UIFactory.Center, new Vector2(-cxo, cy), new Vector2(r * 2, r * 2));
            UIFactory.Rounded(UIFactory.Stretch(left, "Fill"), Palette.WithA(Palette.VennLeft, 0.17f), -1);
            UIFactory.Rounded(UIFactory.Stretch(left, "Edge"), Palette.VennLeft, -1, 7f);
            var right = UIFactory.Rect(structure, "CircleR", UIFactory.Center, new Vector2(cxo, cy), new Vector2(r * 2, r * 2));
            UIFactory.Rounded(UIFactory.Stretch(right, "Fill"), Palette.WithA(Palette.VennRight, 0.17f), -1);
            UIFactory.Rounded(UIFactory.Stretch(right, "Edge"), Palette.VennRight, -1, 7f);
            view.AddIntro(left, DiagramView.IntroKind.Circle, 0.15f);
            view.AddIntro(right, DiagramView.IntroKind.Circle, 0.3f);

            float d = 0.7f;
            void Num(string group, int n, float x, float y)
            {
                var rt = UIFactory.Rect(layer, "N" + n, UIFactory.Center, new Vector2(x, y), new Vector2(68, 68));
                UIFactory.Rounded(rt, Palette.WithA(Color.white, 0.85f), -1);
                UIFactory.Label(rt, "T", n.ToString(), 44, Palette.Ink).rectTransform.offsetMin = new Vector2(0, 3);
                view.AddItem(group, rt);
                view.AddIntro(rt, DiagramView.IntroKind.Pop, d, true);
                d += 0.035f;
            }
            Num("leftOnly", 1, -305, 110); Num("leftOnly", 3, -190, 165); Num("leftOnly", 5, -320, -30); Num("leftOnly", 7, -295, -150); Num("leftOnly", 9, -180, -180);
            Num("both", 11, -40, 95); Num("both", 13, 40, 95); Num("both", 15, 0, -20); Num("both", 17, -40, -135); Num("both", 19, 40, -135);
            Num("rightOnly", 12, 190, 165); Num("rightOnly", 14, 305, 110); Num("rightOnly", 16, 320, -30); Num("rightOnly", 18, 180, -180); Num("rightOnly", 20, 295, -150);
            for (int i = 0; i < 5; i++) Num("outside", 2 + i * 2, -240 + i * 120, -326);

            view.AddRegion("leftCircle", R(-cxo, cy, r * 2, r * 2), true);
            view.AddRegion("rightCircle", R(cxo, cy, r * 2, r * 2), true);
        }

        // ------------------------------------------------------------------ 4. bar chart

        static void Bars()
        {
            float x0 = -330f, y0 = -215f, top = 305f, xEnd = 460f;
            float unit = (top - y0) / 6f;
            var frame = LTRB(-485, 350, 485, -372);
            Table(frame);
            TableBorder(frame);
            for (int v = 1; v <= 6; v++) HLine(structure, y0 + v * unit, x0, xEnd, 3, Palette.WithA(Palette.Line, 0.8f));
            for (int v = 0; v <= 6; v++) Txt(structure, v.ToString(), new Vector2(x0 - 36, y0 + v * unit), new Vector2(60, 50), 38, Palette.Ink);
            var yTitle = Txt(structure, "Number of balls", new Vector2(x0 - 104, (y0 + top) / 2f), new Vector2(360, 50), 32, Palette.InkSoft);
            ((RectTransform)yTitle.transform.parent).localRotation = Quaternion.Euler(0, 0, 90f);
            VLine(structure, x0, y0 - 3, top + 18, 6, Palette.Ink);
            HLine(structure, y0, x0 - 3, xEnd, 6, Palette.Ink);

            string[] names = { "Beach balls", "Volleyballs", "Basketballs", "Footballs" };
            string[] icons = { "beach_ball", "fl_volleyball", "fl_basketball", "fl_soccer_ball" };
            int[] values = { 5, 2, 4, 3 };
            float slot = (xEnd - x0) / 4f;
            view.axisX = x0;
            view.zeroY = y0;
            view.unit = unit;
            for (int i = 0; i < 4; i++)
            {
                float cx = x0 + slot * (i + 0.5f);
                var bar = UIFactory.Rect(layer, "Bar" + i, UIFactory.Center, new Vector2(cx, y0 + 3), new Vector2(118, values[i] * unit), new Vector2(0.5f, 0f));
                var lip = UIFactory.Stretch(bar, "Shade");
                lip.offsetMin = new Vector2(70, 0);
                UIFactory.Rounded(UIFactory.Stretch(bar, "Face"), Palette.Bars[i], 14);
                UIFactory.Rounded(lip, Palette.WithA(Color.black, 0.10f), 14);
                var shine = UIFactory.Stretch(bar, "Shine");
                shine.offsetMin = new Vector2(14, 10); shine.offsetMax = new Vector2(-88, -12);
                UIFactory.Rounded(shine, Palette.WithA(Color.white, 0.35f), 8);
                view.bars.Add(bar);
                view.barValues.Add(values[i]);
                view.AddItem("bars", bar);
                view.AddIntro(bar, DiagramView.IntroKind.Grow, 0.35f + i * 0.15f, true);
                var ic = Pic(layer, icons[i], new Vector2(cx, y0 - 50), 66);
                view.AddIntro(ic, DiagramView.IntroKind.Pop, 0.25f + i * 0.08f);
                var lbl = Txt(layer, names[i], new Vector2(cx, y0 - 116), new Vector2(slot, 50), 32, Palette.Ink);
                view.AddIntro(lbl.rectTransform.parent as RectTransform, DiagramView.IntroKind.Pop, 0.3f + i * 0.08f);
            }
            view.AddRegion("axis", LTRB(x0 - 70, top + 26, x0 + 4, y0 - 26));
        }

        // ------------------------------------------------------------------ 5. trees pictogram

        static void Trees()
        {
            Title("Trees planted", 332, Palette.Green);
            var table = LTRB(-470, 290, 470, -214);
            Table(table);
            string[] days = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
            int[] counts = { 3, 7, 6, 8, 4, 2 };
            for (int i = 1; i < 6; i += 2)
                BoxC(structure, "Tint", LTRB(-470, 290 - i * 84, 470, 290 - (i + 1) * 84), Palette.RowTint, i == 5 ? new Vector4(0, 0, 22, 22) : Vector4.zero);
            for (int i = 1; i < 6; i++) HLine(structure, 290 - i * 84, -470, 470, 3, Palette.Line);
            VLine(structure, -220, 290, -214, 3, Palette.Line);
            TableBorder(table);
            float d = 0.25f;
            for (int row = 0; row < 6; row++)
            {
                float y = 248 - row * 84;
                var lbl = Txt(layer, days[row], new Vector2(-345, y), new Vector2(230, 70), 36, Palette.Ink);
                view.AddIntro(lbl.rectTransform.parent as RectTransform, DiagramView.IntroKind.Pop, 0.12f + row * 0.05f);
                for (int k = 0; k < counts[row]; k++)
                {
                    Item("row" + row, "fl_evergreen_tree", new Vector2(-172 + k * 82, y), 70, d);
                    d += 0.03f;
                }
                view.AddRegion("row" + row, LTRB(-470, y + 42, 470, y - 42));
            }
            KeyBox(new Vector2(0, -287), "fl_evergreen_tree", "= 2 trees");
        }

        // ------------------------------------------------------------------ 6. sea animals table

        static void Sea()
        {
            Title("Sea animals", 322, Palette.Blue);
            var table = LTRB(-470, 262, 470, -250);
            Table(table);
            string[] names = { "Sharks", "Octopuses", "Starfish", "Crabs" };
            string[] icons = { "fl_shark", "fl_octopus", "starfish", "fl_crab" };
            int[] counts = { 4, 1, 5, 2 };
            BoxC(structure, "LabelCol", LTRB(-470, 262, -220, -250), Palette.Hex(0xE6F6FF), new Vector4(22, 0, 0, 22));
            for (int i = 1; i < 4; i++) HLine(structure, 262 - i * 128, -470, 470, 3, Palette.Line);
            VLine(structure, -220, 262, -250, 4, Palette.Line);
            for (int c = 1; c < 5; c++) VLine(structure, -220 + c * 138, 262, -250, 3, Palette.Line);
            TableBorder(table);
            float d = 0.25f;
            for (int row = 0; row < 4; row++)
            {
                float y = 198 - row * 128;
                var lbl = Txt(layer, names[row], new Vector2(-345, y), new Vector2(230, 70), 38, Palette.Ink);
                view.AddIntro(lbl.rectTransform.parent as RectTransform, DiagramView.IntroKind.Pop, 0.12f + row * 0.06f);
                for (int k = 0; k < counts[row]; k++)
                {
                    Item("all", icons[row], new Vector2(-151 + k * 138, y), 104, d, Random.Range(-6f, 6f));
                    d += 0.05f;
                }
            }
            view.AddRegion("table", table);
        }

        // ------------------------------------------------------------------ 8/9/10. Carroll diagrams

        const float GL = -470f, GR = 470f, GT = 325f, GB = -325f, RW = 250f, CH = 122f;
        static float ColX(int c) => c == 0 ? (GL + RW + (GL + RW + (GR - GL - RW) / 2f)) / 2f : (GL + RW + (GR - GL - RW) / 2f + GR) / 2f;
        static float RowY(int r) => r == 0 ? (GT - CH + (GT - CH + GB) / 2f) / 2f : ((GT - CH + GB) / 2f + GB) / 2f;
        static float ColSplit => GL + RW + (GR - GL - RW) / 2f;
        static float RowSplit => (GT - CH + GB) / 2f;

        static void CarrollGrid(string corner, string[] cols, string[] rows, string[] colIcons = null, string[] rowIcons = null, bool[] colNo = null, bool[] rowNo = null)
        {
            var grid = LTRB(GL, GT, GR, GB);
            Table(grid);
            float cs = ColSplit, rs = RowSplit;
            BoxC(structure, "ColHead", LTRB(GL + RW, GT, GR, GT - CH), Palette.HeaderA, new Vector4(0, 22, 0, 0));
            BoxC(structure, "RowHead", LTRB(GL, GT - CH, GL + RW, GB), Palette.HeaderB, new Vector4(0, 0, 0, 22));
            BoxC(structure, "Corner", LTRB(GL, GT, GL + RW, GT - CH), Palette.Hex(0xFFF1CF), new Vector4(22, 0, 0, 0));
            HLine(structure, GT - CH, GL, GR, 5, Palette.Line);
            HLine(structure, rs, GL, GR, 5, Palette.Line);
            VLine(structure, GL + RW, GT, GB, 5, Palette.Line);
            VLine(structure, cs, GT, GB, 5, Palette.Line);
            TableBorder(grid);
            if (!string.IsNullOrEmpty(corner)) Txt(structure, corner, new Vector2(GL + RW / 2f, GT - CH / 2f), new Vector2(RW - 20, CH - 16), 32, Palette.GoldLip);

            float delay = 0.18f;
            for (int c = 0; c < 2; c++)
            {
                var hdr = UIFactory.Rect(layer, "ColHeader" + c, UIFactory.Center, new Vector2(ColX(c), GT - CH / 2f), new Vector2(330, CH));
                if (colIcons != null)
                {
                    var ic = Pic(hdr, colIcons[c], new Vector2(-118, 0), 78);
                    if (colNo != null && colNo[c]) Pic(ic, "fl_prohibited", new Vector2(18, -18), 46);
                    Txt(hdr, cols[c], new Vector2(42, 0), new Vector2(240, CH - 14), 34, Color.white, true);
                }
                else Txt(hdr, cols[c], Vector2.zero, new Vector2(320, CH - 14), 38, Color.white, true);
                view.AddIntro(hdr, DiagramView.IntroKind.Pop, delay);
                delay += 0.08f;
            }
            for (int r = 0; r < 2; r++)
            {
                var hdr = UIFactory.Rect(layer, "RowHeader" + r, UIFactory.Center, new Vector2(GL + RW / 2f, RowY(r)), new Vector2(RW, 250));
                if (rowIcons != null)
                {
                    var ic = Pic(hdr, rowIcons[r], new Vector2(0, 46), 96);
                    if (rowNo != null && rowNo[r]) Pic(ic, "fl_prohibited", new Vector2(26, -26), 54);
                    Txt(hdr, rows[r], new Vector2(0, -58), new Vector2(RW - 24, 70), 36, Color.white, true);
                }
                else Txt(hdr, rows[r], Vector2.zero, new Vector2(RW - 24, 200), 40, Color.white, true);
                view.AddIntro(hdr, DiagramView.IntroKind.Pop, delay);
                delay += 0.08f;
            }

            view.AddRegion("row0", LTRB(GL, GT - CH, GR, rs));
            view.AddRegion("row1", LTRB(GL, rs, GR, GB));
            view.AddRegion("col0", LTRB(GL + RW, GT, cs, GB));
            view.AddRegion("col1", LTRB(cs, GT, GR, GB));
            view.AddRegion("cell00", LTRB(GL + RW, GT - CH, cs, rs));
            view.AddRegion("cell01", LTRB(cs, GT - CH, GR, rs));
            view.AddRegion("cell10", LTRB(GL + RW, rs, cs, GB));
            view.AddRegion("cell11", LTRB(cs, rs, GR, GB));
        }

        static Vector2 Cell(int r, int c) => new Vector2(ColX(c), RowY(r));

        static void CarrollNumbers()
        {
            CarrollGrid("Carroll diagram", new[] { "an even number", "not an even number" }, new[] { "a multiple of 3", "not a multiple of 3" });
            int[][] nums = { new[] { 6, 18, 24 }, new[] { 9, 21, 27 }, new[] { 10, 20, 40 }, new[] { 25, 35, 55 } };
            Vector2[] spots = { new Vector2(0, 62), new Vector2(-82, -58), new Vector2(82, -58) };
            float d = 0.6f;
            for (int cell = 0; cell < 4; cell++)
            {
                int r = cell / 2, c = cell % 2;
                for (int k = 0; k < 3; k++)
                {
                    var rt = UIFactory.Rect(layer, "Num", UIFactory.Center, Cell(r, c) + spots[k], new Vector2(96, 96));
                    var lip = UIFactory.Stretch(rt, "Lip");
                    lip.offsetMin = new Vector2(0, -6); lip.offsetMax = new Vector2(0, -6);
                    UIFactory.Rounded(lip, Palette.BlueLip, 18);
                    UIFactory.Rounded(UIFactory.Stretch(rt, "Face"), Color.white, 18);
                    UIFactory.Rounded(UIFactory.Stretch(rt, "Edge"), Palette.Blue, 18, 5f);
                    UIFactory.Label(rt, "T", nums[cell][k].ToString(), 48, Palette.Ink).rectTransform.offsetMin = new Vector2(0, 3);
                    view.AddItem("cell" + r + c, rt);
                    view.AddIntro(rt, DiagramView.IntroKind.Pop, d, true);
                    d += 0.05f;
                }
            }
        }

        static void CarrollNames()
        {
            CarrollGrid(null, new[] { "Can ride a bike", "Cannot ride a bike" }, new[] { "Can swim", "Cannot swim" },
                new[] { "fl_person_biking", "fl_person_biking" }, new[] { "fl_person_swimming", "fl_person_swimming" },
                new[] { false, true }, new[] { false, true });
            string[][] names =
            {
                new[] { "Lily", "Olivia", "Jack", "Tom" }, new[] { "Sarah", "Ruby", "Harry" },
                new[] { "James", "Alfie", "Emily" }, new[] { "Will", "Chloe", "Grace" }
            };
            Color[] tints = { Palette.Hex(0xE6F6FF), Palette.Hex(0xFFE9F1), Palette.Hex(0xE9FBEF), Palette.Hex(0xF1EAFF) };
            float d = 0.6f;
            for (int cell = 0; cell < 4; cell++)
            {
                int r = cell / 2, c = cell % 2;
                var list = names[cell];
                for (int k = 0; k < list.Length; k++)
                {
                    float y = (list.Length - 1) * 29f - k * 58f;
                    var rt = UIFactory.Rect(layer, list[k], UIFactory.Center, Cell(r, c) + new Vector2(0, y), new Vector2(210, 52));
                    UIFactory.Rounded(rt, tints[k % 4], 26);
                    UIFactory.Rounded(UIFactory.Stretch(rt, "Edge"), Palette.Line, 26, 3f);
                    UIFactory.Label(rt, "T", list[k], 36, Palette.Ink).rectTransform.offsetMin = new Vector2(0, 3);
                    view.AddItem("cell" + r + c, rt);
                    view.AddIntro(rt, DiagramView.IntroKind.Pop, d, true);
                    d += 0.05f;
                }
            }
        }

        static void CarrollShapes()
        {
            CarrollGrid(null, new[] { "4 corners or fewer", "More than 4 corners" }, new[] { "Symmetrical", "Not symmetrical" });
            var cells = new (string sprite, Vector2 pos, float size)[][]
            {
                new[] { ("shp_ellipse", new Vector2(-92, 62), 104f), ("shp_triangle", new Vector2(62, 68), 104f), ("shp_kite", new Vector2(-66, -70), 104f), ("shp_rect", new Vector2(92, -66), 100f) },
                new[] { ("shp_star", new Vector2(-72, 0), 130f), ("shp_pentagon", new Vector2(84, 0), 120f) },
                new[] { ("shp_sliver", new Vector2(-112, 58), 100f), ("shp_scalene1", new Vector2(8, 66), 104f), ("shp_quad", new Vector2(116, 52), 96f), ("shp_scalene2", new Vector2(-62, -72), 100f), ("shp_scalene3", new Vector2(80, -72), 96f) },
                new[] { ("shp_hexa", new Vector2(-88, 56), 108f), ("shp_penta_irr", new Vector2(82, 58), 104f), ("shp_hexb", new Vector2(0, -72), 112f) },
            };
            float d = 0.6f;
            for (int cell = 0; cell < 4; cell++)
            {
                int r = cell / 2, c = cell % 2;
                foreach (var s in cells[cell])
                {
                    Item("cell" + r + c, s.sprite, Cell(r, c) + s.pos, s.size, d);
                    d += 0.05f;
                }
            }
        }
    }
}

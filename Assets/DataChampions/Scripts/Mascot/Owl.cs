using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DataChampions
{
    /// <summary>
    /// Professor Hoot, the game-show host owl: built from rounded Procedural Image shapes plus a few painted
    /// sprites (beak, tufts, happy eyes). Breathes, blinks, looks around, talks while his voice line plays, and
    /// reacts: Happy, Cheer, Sad, Think, Wave, Point, Hop.
    /// </summary>
    public class Owl : MonoBehaviour
    {
        public RectTransform jumper, breath, head, headTilt, wingL, wingR, beakLower, capTassel;
        public RectTransform pupilL, pupilR, lidL, lidR;
        public GameObject eyesOpenL, eyesOpenR, eyesHappyL, eyesHappyR;
        public Image shadow;

        Vector2 look, lookTarget;
        float nextBlink, nextGlance, happyUntil;
        float lidRest;     // 0 open, ~0.45 sad
        Tween blinkTween, mood;
        bool talking;

        const float WingRest = 6f;

        void OnEnable()
        {
            nextBlink = Time.unscaledTime + Random.Range(1.5f, 3.5f);
            nextGlance = Time.unscaledTime + Random.Range(2f, 4f);
        }

        void Update()
        {
            float t = Time.unscaledTime;
            // breathing
            float b = Mathf.Sin(t * 2.6f);
            breath.localScale = new Vector3(1f - b * 0.012f, 1f + b * 0.02f, 1f);
            // tassel sway
            if (capTassel != null) capTassel.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.8f) * 10f);

            // talking: beak + a little head bob
            float level = GameAudio.I != null ? GameAudio.I.VoiceLevel() : 0f;
            talking = level > 0.01f;
            beakLower.localScale = new Vector3(1f, 0.25f + level * 1.1f, 1f);
            headTilt.anchoredPosition = new Vector2(0, level * 6f);

            // eyes wander
            if (t > nextGlance)
            {
                nextGlance = t + Random.Range(1.8f, 4.5f);
                lookTarget = Random.value < 0.45f ? Vector2.zero : new Vector2(Random.Range(-1f, 1f), Random.Range(-0.6f, 0.8f));
            }
            look = Vector2.Lerp(look, lookTarget, Time.unscaledDeltaTime * 8f);
            pupilL.anchoredPosition = pupilR.anchoredPosition = look * 13f;

            if (t > nextBlink && Time.unscaledTime > happyUntil)
            {
                nextBlink = t + Random.Range(2.2f, 5f);
                Blink();
            }
            if (happyUntil > 0f && t > happyUntil)
            {
                happyUntil = 0f;
                SetHappyEyes(false);
            }
        }

        public void LookAt(Vector2 dir) => lookTarget = Vector2.ClampMagnitude(dir, 1f);

        void Blink()
        {
            blinkTween?.Kill();
            var s = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            s.Append(lidL.DOScaleY(1f, 0.07f)).Join(lidR.DOScaleY(1f, 0.07f));
            s.Append(lidL.DOScaleY(lidRest, 0.09f)).Join(lidR.DOScaleY(lidRest, 0.09f));
            blinkTween = s;
        }

        void SetHappyEyes(bool on)
        {
            eyesOpenL.SetActive(!on);
            eyesOpenR.SetActive(!on);
            eyesHappyL.SetActive(on);
            eyesHappyR.SetActive(on);
        }

        /// <summary>Eyelid closure 0..1; tilt &gt; 0 droops the outer corners (sad), 0 is level.</summary>
        void SetLids(float v, float time = 0.2f, float tilt = 0f)
        {
            lidRest = v;
            lidL.DOKill(); lidR.DOKill();
            lidL.DOScaleY(v, time).SetUpdate(true);
            lidR.DOScaleY(v, time).SetUpdate(true);
            lidL.DOLocalRotate(new Vector3(0, 0, tilt), time).SetUpdate(true);
            lidR.DOLocalRotate(new Vector3(0, 0, -tilt), time).SetUpdate(true);
        }

        void ResetPose()
        {
            mood?.Kill();
            jumper.DOKill();
            headTilt.DOKill();
            wingL.DOKill();
            wingR.DOKill();
            jumper.anchoredPosition = Vector2.zero;
            jumper.localScale = Vector3.one;
            jumper.localRotation = Quaternion.identity;
            headTilt.localRotation = Quaternion.identity;
            wingL.localRotation = Quaternion.Euler(0, 0, -WingRest);
            wingR.localRotation = Quaternion.Euler(0, 0, WingRest);
            if (shadow != null) shadow.rectTransform.localScale = Vector3.one;
        }

        // ------------------------------------------------------------------ moods

        public void Idle()
        {
            ResetPose();
            SetLids(0f);
            SetHappyEyes(false);
            happyUntil = 0f;
        }

        public void Hop(float height = 40f)
        {
            ResetPose();
            var s = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            s.Append(jumper.DOScale(new Vector3(1.08f, 0.9f, 1), 0.1f));
            s.Append(jumper.DOAnchorPosY(height, 0.22f).SetEase(Ease.OutQuad)).Join(jumper.DOScale(new Vector3(0.95f, 1.06f, 1), 0.22f));
            s.Append(jumper.DOAnchorPosY(0, 0.2f).SetEase(Ease.InQuad)).Join(jumper.DOScale(Vector3.one, 0.2f));
            s.Append(jumper.DOScale(new Vector3(1.06f, 0.94f, 1), 0.08f)).Append(jumper.DOScale(Vector3.one, 0.14f).SetEase(Ease.OutBack));
            mood = s;
        }

        public void Happy()
        {
            ResetPose();
            SetLids(0f, 0.1f);
            SetHappyEyes(true);
            happyUntil = Time.unscaledTime + 1.6f;
            var s = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            s.Append(jumper.DOScale(new Vector3(1.1f, 0.88f, 1), 0.1f));
            s.Append(jumper.DOAnchorPosY(70, 0.25f).SetEase(Ease.OutQuad)).Join(jumper.DOScale(new Vector3(0.94f, 1.08f, 1), 0.25f));
            s.Join(wingL.DOLocalRotate(new Vector3(0, 0, -95f), 0.18f)).Join(wingR.DOLocalRotate(new Vector3(0, 0, 95f), 0.18f));
            s.Append(jumper.DOAnchorPosY(0, 0.22f).SetEase(Ease.InQuad)).Join(jumper.DOScale(Vector3.one, 0.22f));
            s.Append(jumper.DOScale(new Vector3(1.08f, 0.92f, 1), 0.08f)).Append(jumper.DOScale(Vector3.one, 0.16f).SetEase(Ease.OutBack));
            s.Append(wingL.DOLocalRotate(new Vector3(0, 0, -70f), 0.12f).SetLoops(4, LoopType.Yoyo));
            s.Join(wingR.DOLocalRotate(new Vector3(0, 0, 70f), 0.12f).SetLoops(4, LoopType.Yoyo));
            s.Append(wingL.DOLocalRotate(new Vector3(0, 0, -WingRest), 0.2f)).Join(wingR.DOLocalRotate(new Vector3(0, 0, WingRest), 0.2f));
            mood = s;
        }

        public void Cheer()
        {
            ResetPose();
            SetLids(0f, 0.1f);
            SetHappyEyes(true);
            happyUntil = Time.unscaledTime + 2.4f;
            var s = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            s.Append(jumper.DOScale(new Vector3(1.12f, 0.86f, 1), 0.12f));
            s.Append(jumper.DOAnchorPosY(120, 0.32f).SetEase(Ease.OutQuad));
            s.Join(jumper.DOLocalRotate(new Vector3(0, 0, 360f), 0.6f, RotateMode.FastBeyond360).SetEase(Ease.InOutCubic));
            s.Join(wingL.DOLocalRotate(new Vector3(0, 0, -120f), 0.2f)).Join(wingR.DOLocalRotate(new Vector3(0, 0, 120f), 0.2f));
            s.Insert(0.44f, jumper.DOAnchorPosY(0, 0.28f).SetEase(Ease.InQuad));
            s.Append(jumper.DOScale(new Vector3(1.1f, 0.9f, 1), 0.08f)).Append(jumper.DOScale(Vector3.one, 0.18f).SetEase(Ease.OutBack));
            s.Append(wingL.DOLocalRotate(new Vector3(0, 0, -80f), 0.12f).SetLoops(6, LoopType.Yoyo));
            s.Join(wingR.DOLocalRotate(new Vector3(0, 0, 80f), 0.12f).SetLoops(6, LoopType.Yoyo));
            s.Append(wingL.DOLocalRotate(new Vector3(0, 0, -WingRest), 0.2f)).Join(wingR.DOLocalRotate(new Vector3(0, 0, WingRest), 0.2f));
            mood = s;
        }

        public void Sad()
        {
            ResetPose();
            SetHappyEyes(false);
            SetLids(0.5f, 0.25f, 16f);
            lookTarget = new Vector2(0, -0.7f);
            var s = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            s.Append(jumper.DOScale(new Vector3(1.05f, 0.93f, 1), 0.3f).SetEase(Ease.OutQuad));
            s.Join(headTilt.DOLocalRotate(new Vector3(0, 0, -9f), 0.3f));
            s.Join(wingL.DOLocalRotate(new Vector3(0, 0, 10f), 0.3f)).Join(wingR.DOLocalRotate(new Vector3(0, 0, -10f), 0.3f));
            s.AppendInterval(1.4f);
            s.Append(jumper.DOScale(Vector3.one, 0.3f)).Join(headTilt.DOLocalRotate(Vector3.zero, 0.3f));
            s.Join(wingL.DOLocalRotate(new Vector3(0, 0, -WingRest), 0.3f)).Join(wingR.DOLocalRotate(new Vector3(0, 0, WingRest), 0.3f));
            s.AppendCallback(() => { SetLids(0f); lookTarget = Vector2.zero; });
            mood = s;
        }

        public void Think()
        {
            ResetPose();
            SetHappyEyes(false);
            SetLids(0.15f);
            lookTarget = new Vector2(-0.7f, 0.7f);
            var s = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            s.Append(headTilt.DOLocalRotate(new Vector3(0, 0, 10f), 0.35f).SetEase(Ease.OutBack));
            s.Join(wingR.DOLocalRotate(new Vector3(0, 0, -38f), 0.35f).SetEase(Ease.OutBack));
            s.AppendInterval(1.6f);
            s.Append(headTilt.DOLocalRotate(Vector3.zero, 0.3f)).Join(wingR.DOLocalRotate(new Vector3(0, 0, WingRest), 0.3f));
            s.AppendCallback(() => { SetLids(0f); lookTarget = Vector2.zero; });
            mood = s;
        }

        public void Wave()
        {
            ResetPose();
            var s = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            s.Append(wingR.DOLocalRotate(new Vector3(0, 0, 110f), 0.22f).SetEase(Ease.OutBack));
            s.Append(wingR.DOLocalRotate(new Vector3(0, 0, 80f), 0.16f).SetLoops(6, LoopType.Yoyo).SetEase(Ease.InOutSine));
            s.Append(wingR.DOLocalRotate(new Vector3(0, 0, WingRest), 0.25f));
            s.Insert(0f, headTilt.DOLocalRotate(new Vector3(0, 0, -6f), 0.3f).SetLoops(2, LoopType.Yoyo));
            mood = s;
        }

        /// <summary>Raise a wing towards the screen side (+1 right, -1 left), e.g. at the board or the diagram.</summary>
        public void Point(int side)
        {
            ResetPose();
            lookTarget = new Vector2(side, 0.1f);
            var wing = side > 0 ? wingR : wingL;
            float a = side > 0 ? 100f : -100f;
            var s = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            s.Append(wing.DOLocalRotate(new Vector3(0, 0, a), 0.25f).SetEase(Ease.OutBack));
            s.Join(headTilt.DOLocalRotate(new Vector3(0, 0, -side * 6f), 0.25f));
            s.Append(wing.DOLocalRotate(new Vector3(0, 0, a * 0.85f), 0.2f).SetLoops(4, LoopType.Yoyo));
            s.AppendInterval(0.5f);
            s.Append(wing.DOLocalRotate(new Vector3(0, 0, side > 0 ? WingRest : -WingRest), 0.3f)).Join(headTilt.DOLocalRotate(Vector3.zero, 0.3f));
            s.AppendCallback(() => lookTarget = Vector2.zero);
            mood = s;
        }

        /// <summary>Pop in from below (screen entrances).</summary>
        public void Enter(float delay = 0f)
        {
            ResetPose();
            jumper.anchoredPosition = new Vector2(0, -420);
            var s = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            s.AppendInterval(delay);
            s.Append(jumper.DOAnchorPosY(30, 0.38f).SetEase(Ease.OutCubic));
            s.Append(jumper.DOAnchorPosY(0, 0.18f).SetEase(Ease.InQuad));
            s.Append(jumper.DOScale(new Vector3(1.08f, 0.92f, 1), 0.08f)).Append(jumper.DOScale(Vector3.one, 0.16f).SetEase(Ease.OutBack));
            mood = s;
        }

        // ------------------------------------------------------------------ build

        static ImageRef Part(Transform parent, string name, Vector2 pos, Vector2 size, Color c, float radius, Vector2? pivot = null)
        {
            var rt = UIFactory.Rect(parent, name, UIFactory.Center, pos, size, pivot);
            UIFactory.Rounded(rt, c, radius);
            return new ImageRef { rt = rt };
        }

        static RectTransform Sprite(Transform parent, string name, string sprite, Vector2 pos, Vector2 size, Color c, float rot = 0f, Vector2? pivot = null)
        {
            var rt = UIFactory.Rect(parent, name, UIFactory.Center, pos, size, pivot);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = GameConfig.Get(sprite);
            img.color = c;
            img.raycastTarget = false;
            rt.localRotation = Quaternion.Euler(0, 0, rot);
            return rt;
        }

        struct ImageRef { public RectTransform rt; }

        /// <summary>Builds the owl. Root pivot is at the feet; the unscaled owl is about 380 x 470.</summary>
        public static Owl Build(Transform parent, string name, Vector2 anchor, Vector2 pos, float scale)
        {
            var root = UIFactory.Rect(parent, name, anchor, pos, new Vector2(380, 470), new Vector2(0.5f, 0f));
            root.localScale = new Vector3(scale, scale, 1f);
            var owl = root.gameObject.AddComponent<Owl>();

            owl.shadow = UIFactory.Icon(root, "Shadow", GameConfig.Get("soft"), new Vector2(0.5f, 0f), new Vector2(0, 6), 300, new Color(0.05f, 0f, 0.15f, 0.45f));
            owl.shadow.preserveAspect = false;
            owl.shadow.rectTransform.sizeDelta = new Vector2(300, 60);

            owl.jumper = UIFactory.Rect(root, "Jumper", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(380, 470), new Vector2(0.5f, 0f));
            owl.breath = UIFactory.Rect(owl.jumper, "Breath", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(380, 470), new Vector2(0.5f, 0f));
            var b = owl.breath;
            // local space: origin at bottom centre -> use anchor bottom-centre for parts
            Vector2 A(float x, float y) => new Vector2(x, y);

            // wings (behind body), pivot at the shoulder
            owl.wingL = Sprite(b, "WingL", "wing", A(-130, 262), new Vector2(110, 190), Palette.OwlDark, 0, new Vector2(0.75f, 0.92f));
            owl.wingL.anchorMin = owl.wingL.anchorMax = new Vector2(0.5f, 0f);
            owl.wingL.anchoredPosition = A(-130, 262);
            owl.wingR = Sprite(b, "WingR", "wing", A(130, 262), new Vector2(110, 190), Palette.OwlDark, 0, new Vector2(0.75f, 0.92f));
            owl.wingR.anchorMin = owl.wingR.anchorMax = new Vector2(0.5f, 0f);
            owl.wingR.localScale = new Vector3(-1, 1, 1);
            owl.wingR.anchoredPosition = A(130, 262);
            owl.wingL.localRotation = Quaternion.Euler(0, 0, -WingRest);
            owl.wingR.localRotation = Quaternion.Euler(0, 0, WingRest);

            // feet
            var footL = Part(b, "FootL", A(-50, 18), new Vector2(64, 30), Palette.OwlBeak, 15).rt;
            var footR = Part(b, "FootR", A(50, 18), new Vector2(64, 30), Palette.OwlBeak, 15).rt;
            foreach (var f in new[] { footL, footR }) { f.anchorMin = f.anchorMax = new Vector2(0.5f, 0f); }

            // body
            var body = Part(b, "Body", A(0, 175), new Vector2(300, 330), Palette.OwlBody, 150).rt;
            body.anchorMin = body.anchorMax = new Vector2(0.5f, 0f);
            body.anchoredPosition = A(0, 175);
            var belly = Part(body, "Belly", A(0, -38), new Vector2(206, 210), Palette.OwlBelly, 103).rt;
            for (int i = 0; i < 3; i++)
            {
                var sc = Sprite(belly, "Feather" + i, "scallop", A(-48 + i * 48, 10), new Vector2(54, 30), Palette.WithA(Palette.OwlBody, 0.35f));
                sc.anchoredPosition = A(-48 + i * 48, 22);
            }
            for (int i = 0; i < 2; i++)
                Sprite(belly, "FeatherB" + i, "scallop", A(-24 + i * 48, -18), new Vector2(54, 30), Palette.WithA(Palette.OwlBody, 0.35f));

            // bow tie
            var bow = UIFactory.Rect(body, "BowTie", UIFactory.Center, A(0, 44), new Vector2(120, 60));
            Sprite(bow, "L", "tri", A(-30, 0), new Vector2(56, 52), Palette.OwlBow, 90);
            Sprite(bow, "R", "tri", A(30, 0), new Vector2(56, 52), Palette.OwlBow, -90);
            Part(bow, "Knot", Vector2.zero, new Vector2(26, 26), Palette.Hex(0xD92E4E), 13);

            // head group (pivot at the neck so tilts look natural)
            owl.head = UIFactory.Rect(b, "Head", new Vector2(0.5f, 0f), A(0, 230), new Vector2(380, 260), new Vector2(0.5f, 0f));
            owl.headTilt = UIFactory.Rect(owl.head, "Tilt", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(380, 260), new Vector2(0.5f, 0f));
            var h = owl.headTilt;
            Vector2 H(float x, float y) => new Vector2(x, y - 130f); // head-local: y measured from the neck

            Sprite(h, "TuftL", "tuft", H(-112, 228), new Vector2(76, 96), Palette.OwlDark, 22);
            Sprite(h, "TuftR", "tuft", H(112, 228), new Vector2(76, 96), Palette.OwlDark, -22);
            var headBlob = Part(h, "HeadBlob", H(0, 120), new Vector2(316, 236), Palette.OwlBody, 118).rt;
            headBlob.SetAsFirstSibling();
            Part(h, "DiscL", H(-66, 118), new Vector2(146, 146), Palette.OwlFace, 73);
            Part(h, "DiscR", H(66, 118), new Vector2(146, 146), Palette.OwlFace, 73);
            var cheekL = UIFactory.Icon(h, "CheekL", GameConfig.Get("soft"), UIFactory.Center, H(-118, 72), 70, Palette.WithA(Palette.Pink, 0.45f));
            var cheekR = UIFactory.Icon(h, "CheekR", GameConfig.Get("soft"), UIFactory.Center, H(118, 72), 70, Palette.WithA(Palette.Pink, 0.45f));

            BuildEye(h, "EyeL", H(-64, 122), out owl.eyesOpenL, out owl.eyesHappyL, out owl.pupilL, out owl.lidL);
            BuildEye(h, "EyeR", H(64, 122), out owl.eyesOpenR, out owl.eyesHappyR, out owl.pupilR, out owl.lidR);

            Sprite(h, "Beak", "beak", H(0, 78), new Vector2(54, 50), Palette.OwlBeak, 0);
            owl.beakLower = Sprite(h, "BeakLower", "beak", H(0, 58), new Vector2(34, 26), Palette.OwlBeakDark, 0, new Vector2(0.5f, 1f));
            owl.beakLower.anchoredPosition = H(0, 62);
            owl.beakLower.SetSiblingIndex(owl.beakLower.GetSiblingIndex() - 1);

            // mortarboard cap
            var cap = UIFactory.Rect(h, "Cap", UIFactory.Center, H(0, 236), new Vector2(240, 120));
            Part(cap, "Band", A(0, -14), new Vector2(150, 46), Palette.OwlCap, 14);
            var flat = UIFactory.Rect(cap, "Flat", UIFactory.Center, A(0, 16), new Vector2(200, 200));
            flat.localScale = new Vector3(1f, 0.36f, 1f);
            var board = Part(flat, "Board", Vector2.zero, new Vector2(160, 160), Palette.OwlCap, 10).rt;
            board.localRotation = Quaternion.Euler(0, 0, 45f);
            Part(cap, "Button", A(0, 18), new Vector2(18, 18), Palette.Gold, 9);
            owl.capTassel = UIFactory.Rect(cap, "Tassel", UIFactory.Center, A(4, 18), new Vector2(10, 80), new Vector2(0.5f, 1f));
            var cord = UIFactory.Rect(owl.capTassel, "Cord", new Vector2(0.5f, 1f), A(60, 0), new Vector2(8, 70), new Vector2(0.5f, 1f));
            UIFactory.Rounded(cord, Palette.Gold, 4);
            var cordTop = UIFactory.Rect(owl.capTassel, "CordTop", new Vector2(0.5f, 1f), A(30, 0), new Vector2(64, 7), new Vector2(0.5f, 1f));
            UIFactory.Rounded(cordTop, Palette.Gold, 3);
            var end = UIFactory.Rect(owl.capTassel, "End", new Vector2(0.5f, 1f), A(60, -66), new Vector2(22, 30), new Vector2(0.5f, 1f));
            UIFactory.Rounded(end, Palette.GoldLip, 8);

            return owl;
        }

        static void BuildEye(Transform parent, string name, Vector2 pos, out GameObject open, out GameObject happy, out RectTransform pupil, out RectTransform lid)
        {
            var eye = UIFactory.Rect(parent, name, UIFactory.Center, pos, new Vector2(110, 110));
            var openRt = UIFactory.Rect(eye, "Open", UIFactory.Center, Vector2.zero, new Vector2(110, 110));
            var white = UIFactory.Rect(openRt, "White", UIFactory.Center, Vector2.zero, new Vector2(100, 100));
            UIFactory.Rounded(white, Color.white, 50);
            var ring = UIFactory.Rect(openRt, "Ring", UIFactory.Center, Vector2.zero, new Vector2(104, 104));
            UIFactory.Rounded(ring, Palette.Gold, 52, 5f);
            pupil = UIFactory.Rect(openRt, "Pupil", UIFactory.Center, Vector2.zero, new Vector2(52, 52));
            UIFactory.Rounded(pupil, Palette.OwlPupil, 26);
            var hl = UIFactory.Rect(pupil, "Shine", UIFactory.Center, new Vector2(9, 10), new Vector2(16, 16));
            UIFactory.Rounded(hl, Color.white, 8);
            lid = UIFactory.Rect(openRt, "Lid", new Vector2(0.5f, 1f), new Vector2(0, 2), new Vector2(112, 112), new Vector2(0.5f, 1f));
            UIFactory.Rounded(lid, Palette.OwlBody, 56);
            lid.localScale = new Vector3(1f, 0f, 1f);
            open = openRt.gameObject;

            var happyRt = UIFactory.Rect(eye, "Happy", UIFactory.Center, new Vector2(0, 6), new Vector2(90, 60));
            var img = happyRt.gameObject.AddComponent<Image>();
            img.sprite = GameConfig.Get("happy_eye");
            img.color = Palette.OwlPupil;
            img.raycastTarget = false;
            happy = happyRt.gameObject;
            happy.SetActive(false);
        }
    }
}

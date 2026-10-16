using UnityEngine;
using UnityEngine.Rendering.Universal;
using Queenless.Rules;

namespace Queenless
{
    public sealed class DroneView : MonoBehaviour
    {
        SpriteRenderer shadow;
        SpriteRenderer wingL;
        SpriteRenderer wingR;
        SpriteRenderer body;
        SpriteRenderer stripeA;
        SpriteRenderer stripeB;
        SpriteRenderer head;
        SpriteRenderer eye;
        SpriteRenderer spark;
        SpriteRenderer chevron;
        Light2D glow;
        bool built;
        bool sliding;
        float slideT;
        Vector3 slideFrom;
        Vector3 slideTo;
        bool isMind;
        bool sparkFly;
        float sparkT;
        Vector3 sparkFrom;
        Dir march;
        int mapY;

        public void Build()
        {
            if (built)
                return;
            built = true;
            shadow = Make("shadow", SpriteFactory.Soft, 1.1f, 0.38f, new Vector3(0f, -0.28f, 0f), Palette.WithAlpha(Palette.Ink, 0.45f));
            wingL = Make("wingL", SpriteFactory.Soft, 0.55f, 0.28f, new Vector3(-0.28f, 0.05f, 0f), Palette.WithAlpha(Color.white, 0.28f));
            wingR = Make("wingR", SpriteFactory.Soft, 0.55f, 0.28f, new Vector3(0.28f, 0.05f, 0f), Palette.WithAlpha(Color.white, 0.28f));
            wingL.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
            wingR.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
            body = Make("body", SpriteFactory.Disc, 0.62f, 0.78f, new Vector3(0f, -0.02f, 0f), Palette.Body);
            stripeA = Make("stripeA", SpriteFactory.Rounded, 0.38f, 0.08f, new Vector3(0f, 0.02f, 0f), Palette.Ink);
            stripeB = Make("stripeB", SpriteFactory.Rounded, 0.32f, 0.07f, new Vector3(0f, -0.12f, 0f), Palette.Ink);
            head = Make("head", SpriteFactory.Disc, 0.28f, 0.26f, new Vector3(0f, 0.28f, 0f), Palette.Body);
            eye = Make("eye", SpriteFactory.Disc, 0.12f, 0.12f, new Vector3(0f, 0.3f, 0f), Palette.Core);
            var sparkGo = new GameObject("spark");
            sparkGo.transform.SetParent(transform, false);
            spark = sparkGo.AddComponent<SpriteRenderer>();
            spark.sprite = SpriteFactory.Disc;
            spark.color = Palette.Core;
            spark.transform.localScale = new Vector3(0.18f, 0.18f, 1f);
            glow = sparkGo.AddComponent<Light2D>();
            glow.lightType = Light2D.LightType.Point;
            glow.color = Palette.Honey;
            glow.intensity = 0.35f;
            glow.pointLightOuterRadius = 2.2f;
            glow.pointLightInnerRadius = 0.2f;
            chevron = Make("chevron", SpriteFactory.Chevron, 0.34f, 0.34f, new Vector3(0f, 0.42f, 0f), Palette.Honey);
            chevron.enabled = false;
        }

        SpriteRenderer Make(string name, Sprite sprite, float sx, float sy, Vector3 local, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = local;
            go.transform.localScale = new Vector3(sx, sy, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            return renderer;
        }

        public void Snap(Vector3 world, Drone drone)
        {
            sliding = false;
            transform.position = world;
            Apply(drone);
        }

        public void Slide(Vector3 from, Vector3 to, Drone drone)
        {
            slideFrom = from;
            slideTo = to;
            slideT = 0f;
            sliding = true;
            transform.position = from;
            Apply(drone);
        }

        public void Apply(Drone drone)
        {
            mapY = drone.Y;
            march = drone.Mind ? Dir.None : drone.March;
            bool wasMind = isMind;
            isMind = drone.Mind;
            body.color = isMind ? Palette.Honey : Palette.Body;
            head.color = isMind ? Palette.Honey : Palette.Body;
            eye.enabled = isMind;
            eye.color = Palette.Core;
            spark.gameObject.SetActive(isMind);
            glow.enabled = isMind;
            if (isMind && !wasMind)
                sparkFly = false;
            chevron.enabled = march != Dir.None;
            if (march != Dir.None)
            {
                float angle = 0f;
                Vector3 offset = Vector3.zero;
                if (march == Dir.Up) { angle = 0f; offset = new Vector3(0f, 0.48f, 0f); }
                else if (march == Dir.Right) { angle = -90f; offset = new Vector3(0.48f, 0f, 0f); }
                else if (march == Dir.Down) { angle = 180f; offset = new Vector3(0f, -0.48f, 0f); }
                else if (march == Dir.Left) { angle = 90f; offset = new Vector3(-0.48f, 0f, 0f); }
                chevron.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                chevron.transform.localPosition = offset;
                chevron.color = Palette.WithAlpha(Palette.Honey, 0.9f);
            }
            int order = 40 + mapY * 5;
            shadow.sortingOrder = order;
            wingL.sortingOrder = order + 1;
            wingR.sortingOrder = order + 1;
            body.sortingOrder = order + 2;
            stripeA.sortingOrder = order + 3;
            stripeB.sortingOrder = order + 3;
            head.sortingOrder = order + 4;
            eye.sortingOrder = order + 5;
            spark.sortingOrder = order + 6;
            chevron.sortingOrder = order + 7;
        }

        public void FlySparkFrom(Vector3 world)
        {
            if (!isMind)
                return;
            sparkFly = true;
            sparkT = 0f;
            sparkFrom = world;
            spark.gameObject.SetActive(true);
        }

        public Vector3 SparkWorld
        {
            get { return spark != null ? spark.transform.position : transform.position; }
        }

        void Update()
        {
            if (sliding)
            {
                slideT += Time.deltaTime / 0.12f;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(slideT));
                transform.position = Vector3.Lerp(slideFrom, slideTo, k);
                if (slideT >= 1f)
                    sliding = false;
            }

            float flap = Mathf.Sin(Time.time * 7f + mapY) * 8f;
            wingL.transform.localRotation = Quaternion.Euler(0f, 0f, 28f + flap);
            wingR.transform.localRotation = Quaternion.Euler(0f, 0f, -28f - flap);

            if (!isMind || spark == null)
                return;
            float ang = Time.time * 2.6f + mapY;
            Vector3 orbit = transform.position + new Vector3(Mathf.Cos(ang) * 0.26f, 0.62f + Mathf.Sin(ang) * 0.08f, 0f);
            if (sparkFly)
            {
                sparkT += Time.deltaTime / 0.15f;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(sparkT));
                spark.transform.position = Vector3.Lerp(sparkFrom, orbit, k);
                if (sparkT >= 1f)
                    sparkFly = false;
            }
            else
            {
                spark.transform.position = orbit;
            }
        }
    }
}

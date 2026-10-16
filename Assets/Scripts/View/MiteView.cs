using UnityEngine;
using Queenless.Rules;

namespace Queenless
{
    public sealed class MiteView : MonoBehaviour
    {
        SpriteRenderer body;
        SpriteRenderer legA;
        SpriteRenderer legB;
        bool built;
        bool sliding;
        float slideT;
        Vector3 slideFrom;
        Vector3 slideTo;

        public void Build()
        {
            if (built)
                return;
            built = true;
            body = Piece("body", SpriteFactory.Disc, new Vector3(0.42f, 0.28f, 1f), Vector3.zero);
            legA = Piece("legA", SpriteFactory.Rounded, new Vector3(0.28f, 0.08f, 1f), new Vector3(-0.12f, -0.12f, 0f));
            legB = Piece("legB", SpriteFactory.Rounded, new Vector3(0.28f, 0.08f, 1f), new Vector3(0.12f, -0.12f, 0f));
            body.color = Palette.Mite;
            legA.color = Palette.Mite;
            legB.color = Palette.Mite;
            body.sortingOrder = 80;
            legA.sortingOrder = 79;
            legB.sortingOrder = 79;
        }

        SpriteRenderer Piece(string name, Sprite sprite, Vector3 scale, Vector3 local)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            return renderer;
        }

        public void Snap(Vector3 world)
        {
            sliding = false;
            transform.position = world;
        }

        public void Slide(Vector3 from, Vector3 to)
        {
            slideFrom = from;
            slideTo = to;
            slideT = 0f;
            sliding = true;
            transform.position = from;
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

            bool frame = Mathf.FloorToInt(Time.time * 8f) % 2 == 0;
            legA.transform.localRotation = Quaternion.Euler(0f, 0f, frame ? 18f : -10f);
            legB.transform.localRotation = Quaternion.Euler(0f, 0f, frame ? -18f : 10f);
            body.transform.localScale = frame ? new Vector3(0.44f, 0.26f, 1f) : new Vector3(0.38f, 0.3f, 1f);
        }
    }
}

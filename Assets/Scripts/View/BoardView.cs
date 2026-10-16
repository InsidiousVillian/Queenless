using System.Collections.Generic;
using UnityEngine;
using Queenless.Rules;

namespace Queenless
{
    public sealed class BoardView : MonoBehaviour
    {
        sealed class Tile
        {
            public int X;
            public int Y;
            public CellKind Kind;
            public SpriteRenderer Floor;
            public SpriteRenderer Mark;
            public SpriteRenderer Fill;
            public float Door;
            public float DoorTarget;
        }

        readonly List<Tile> tiles = new List<Tile>();
        readonly Dictionary<int, DroneView> drones = new Dictionary<int, DroneView>();
        readonly List<MiteView> mites = new List<MiteView>();
        readonly List<SpriteRenderer> ghosts = new List<SpriteRenderer>();
        SpriteRenderer miteNext;
        int chamber;
        bool padsBright;
        bool settled = true;

        public static Vector3 World(int x, int y)
        {
            return new Vector3(x, -y, 0f);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Build(HiveRules rules, int chamberNumber)
        {
            SpriteFactory.Ensure();
            chamber = chamberNumber;
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
            tiles.Clear();
            drones.Clear();
            mites.Clear();
            ghosts.Clear();

            for (int y = 0; y < rules.Height; y++)
            {
                for (int x = 0; x < rules.Width; x++)
                    tiles.Add(MakeTile(rules, x, y));
            }

            for (int i = 0; i < rules.Drones.Count; i++)
            {
                var go = new GameObject("Drone " + rules.Drones[i].Id);
                go.transform.SetParent(transform, false);
                var view = go.AddComponent<DroneView>();
                view.Build();
                drones[rules.Drones[i].Id] = view;
            }

            for (int i = 0; i < rules.Mites.Count; i++)
            {
                var go = new GameObject("Mite");
                go.transform.SetParent(transform, false);
                var view = go.AddComponent<MiteView>();
                view.Build();
                mites.Add(view);
            }

            for (int i = 0; i < 4; i++)
            {
                var ghost = NewRenderer("ghost", SpriteFactory.Disc, 12);
                ghost.color = Palette.WithAlpha(Palette.Body, 0.35f);
                ghost.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
                ghost.enabled = false;
                ghosts.Add(ghost);
            }

            miteNext = NewRenderer("mite-next", SpriteFactory.Rounded, 14);
            miteNext.color = Palette.WithAlpha(Palette.Mite, 0.35f);
            miteNext.transform.localScale = new Vector3(0.92f, 0.92f, 1f);
            miteNext.enabled = false;

            Show();
            Settle(rules);
        }

        public void Settle(HiveRules rules)
        {
            settled = true;
            for (int i = 0; i < rules.Drones.Count; i++)
            {
                var drone = rules.Drones[i];
                DroneView view;
                if (!drones.TryGetValue(drone.Id, out view))
                    continue;
                view.Snap(World(drone.X, drone.Y), drone);
            }
            for (int i = 0; i < rules.Mites.Count && i < mites.Count; i++)
                mites[i].Snap(World(rules.Mites[i].X, rules.Mites[i].Y));
            Refresh(rules);
        }

        public float Animate(HiveRules before, HiveRules after)
        {
            settled = false;
            float time = 0f;
            Vector3 oldSpark = Vector3.zero;
            int oldMind = before.Mind().Id;
            DroneView oldView;
            if (drones.TryGetValue(oldMind, out oldView))
                oldSpark = oldView.SparkWorld;

            for (int i = 0; i < after.Drones.Count; i++)
            {
                var drone = after.Drones[i];
                var was = before.DroneById(drone.Id);
                DroneView view;
                if (!drones.TryGetValue(drone.Id, out view))
                    continue;
                Vector3 a = World(was.X, was.Y);
                Vector3 b = World(drone.X, drone.Y);
                if (was.X != drone.X || was.Y != drone.Y)
                {
                    view.Slide(a, b, drone);
                    time = 0.12f;
                }
                else
                {
                    view.Snap(b, drone);
                }
            }

            int newMind = after.Mind().Id;
            if (newMind != oldMind)
            {
                DroneView now;
                if (drones.TryGetValue(newMind, out now))
                    now.FlySparkFrom(oldSpark);
            }

            for (int i = 0; i < after.Mites.Count && i < mites.Count; i++)
            {
                var was = before.Mites[i];
                var now = after.Mites[i];
                if (was.X != now.X || was.Y != now.Y)
                {
                    mites[i].Slide(World(was.X, was.Y), World(now.X, now.Y));
                    time = 0.12f;
                }
                else
                {
                    mites[i].Snap(World(now.X, now.Y));
                }
            }

            HideForecast();
            return time;
        }

        public void Refresh(HiveRules rules)
        {
            padsBright = CloseToChorus(rules);
            for (int i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                bool occupied = rules.DroneAt(tile.X, tile.Y) != null;
                if (tile.Kind == CellKind.GoldSeal || tile.Kind == CellKind.TealSeal)
                {
                    if (tile.Fill != null)
                        tile.Fill.enabled = occupied;
                }
                if (tile.Kind == CellKind.GoldDoor)
                    tile.DoorTarget = rules.GoldOpen() ? 0.22f : 1f;
                if (tile.Kind == CellKind.TealDoor)
                    tile.DoorTarget = rules.TealOpen() ? 0.22f : 1f;
            }
            RefreshForecast(rules);
        }

        void RefreshForecast(HiveRules rules)
        {
            for (int i = 0; i < ghosts.Count; i++)
                ghosts[i].enabled = false;
            if (!settled)
            {
                miteNext.enabled = false;
                return;
            }

            var forecast = rules.MarchGhosts();
            for (int i = 0; i < forecast.Count && i < ghosts.Count; i++)
            {
                ghosts[i].enabled = true;
                ghosts[i].transform.position = World(forecast[i].X, forecast[i].Y);
                var drone = rules.DroneById(forecast[i].Id);
                ghosts[i].color = Palette.WithAlpha(drone != null && drone.Mind ? Palette.Honey : Palette.Body, 0.35f);
            }

            if (rules.Mites.Count == 0)
            {
                miteNext.enabled = false;
                return;
            }
            int nx;
            int ny;
            rules.MiteNext(rules.Mites[0], out nx, out ny);
            miteNext.enabled = true;
            miteNext.transform.position = World(nx, ny);
            miteNext.color = Palette.WithAlpha(Palette.Mite, 0.35f);
        }

        void HideForecast()
        {
            for (int i = 0; i < ghosts.Count; i++)
                ghosts[i].enabled = false;
            if (miteNext != null)
                miteNext.enabled = false;
        }

        void Update()
        {
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 2.4f);
            float haze = 0.22f + 0.12f * Mathf.Sin(Time.time * 1.3f);
            for (int i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                if (tile.Kind == CellKind.GoldDoor || tile.Kind == CellKind.TealDoor)
                {
                    tile.Door = Mathf.MoveTowards(tile.Door, tile.DoorTarget, Time.deltaTime * 5f);
                    if (tile.Mark != null)
                    {
                        tile.Mark.transform.localScale = new Vector3(0.92f * tile.Door, 0.92f * tile.Door, 1f);
                        var color = tile.Kind == CellKind.GoldDoor ? Palette.Gold : Palette.Teal;
                        tile.Mark.color = Color.Lerp(Palette.Floor, color, Mathf.Lerp(0.45f, 1f, tile.Door));
                        tile.Mark.sortingOrder = tile.Door > 0.6f ? 18 : 6;
                    }
                }
                else if (tile.Kind == CellKind.Silence && tile.Mark != null)
                {
                    var color = Palette.Silence;
                    color.a = haze;
                    tile.Mark.color = color;
                    tile.Mark.transform.localScale = new Vector3(1.05f + haze * 0.15f, 1.05f + haze * 0.15f, 1f);
                }
                else if (tile.Kind == CellKind.Exit && tile.Mark != null)
                {
                    tile.Mark.color = Palette.WithAlpha(Palette.Exit, 0.55f + 0.35f * pulse);
                    tile.Mark.transform.localScale = new Vector3(0.72f + pulse * 0.08f, 0.72f + pulse * 0.08f, 1f);
                }
                else if (tile.Kind == CellKind.Pad && tile.Mark != null)
                {
                    float bright = padsBright ? 1f : 0.55f;
                    tile.Mark.color = Palette.WithAlpha(Palette.Pad, bright);
                    tile.Mark.transform.localScale = Vector3.one * (padsBright ? 0.82f + pulse * 0.06f : 0.72f);
                }
            }
        }

        Tile MakeTile(HiveRules rules, int x, int y)
        {
            var kind = rules.At(x, y);
            var tile = new Tile { X = x, Y = y, Kind = kind, Door = 1f, DoorTarget = 1f };
            var root = new GameObject("cell " + x + "," + y);
            root.transform.SetParent(transform, false);
            root.transform.position = World(x, y);

            bool crack = chamber == 2 && kind == CellKind.Wall && CrackBetween(rules, x, y);
            if (kind == CellKind.Wall)
            {
                tile.Floor = Add(root.transform, "wall", SpriteFactory.Rounded, Palette.Wall, 0.98f, 8);
                var lip = Add(root.transform, "lip", SpriteFactory.Rounded, Palette.WallHighlight, 1f, 9);
                lip.transform.localScale = new Vector3(0.86f, 0.16f, 1f);
                lip.transform.localPosition = new Vector3(0f, 0.34f, 0f);
                if (crack)
                {
                    var mark = Add(root.transform, "crack", SpriteFactory.Crack, Color.white, 0.9f, 10);
                    tile.Mark = mark;
                }
            }
            else
            {
                float mix = ((x * 3 + y * 5) % 4) / 4f;
                var floor = Color.Lerp(Palette.Floor, Palette.FloorAlt, mix);
                tile.Floor = Add(root.transform, "floor", SpriteFactory.Rounded, floor, 0.9f, 0);
            }

            if (kind == CellKind.Silence)
            {
                tile.Mark = Add(root.transform, "haze", SpriteFactory.Soft, Palette.WithAlpha(Palette.Silence, 0.35f), 1.2f, 3);
            }
            else if (kind == CellKind.GoldSeal || kind == CellKind.TealSeal)
            {
                var color = kind == CellKind.GoldSeal ? Palette.Gold : Palette.Teal;
                tile.Mark = Add(root.transform, "ring", SpriteFactory.Ring, color, 0.78f, 4);
                tile.Fill = Add(root.transform, "fill", SpriteFactory.Disc, color, 0.28f, 5);
                tile.Fill.enabled = false;
            }
            else if (kind == CellKind.GoldDoor || kind == CellKind.TealDoor)
            {
                var color = kind == CellKind.GoldDoor ? Palette.Gold : Palette.Teal;
                tile.Mark = Add(root.transform, "door", SpriteFactory.Rounded, color, 0.92f, 18);
                tile.DoorTarget = kind == CellKind.GoldDoor ? (rules.GoldOpen() ? 0.22f : 1f) : (rules.TealOpen() ? 0.22f : 1f);
                tile.Door = tile.DoorTarget;
            }
            else if (kind == CellKind.Exit)
            {
                tile.Mark = Add(root.transform, "exit", SpriteFactory.Disc, Palette.Exit, 0.72f, 4);
            }
            else if (kind == CellKind.Pad)
            {
                tile.Mark = Add(root.transform, "pad", SpriteFactory.Ring, Palette.Pad, 0.72f, 4);
            }

            return tile;
        }

        static bool CrackBetween(HiveRules rules, int x, int y)
        {
            if (!rules.Inside(x, y - 1) || !rules.Inside(x, y + 1))
                return false;
            return rules.At(x, y - 1) != CellKind.Wall && rules.At(x, y + 1) != CellKind.Wall;
        }

        static bool CloseToChorus(HiveRules rules)
        {
            if (!rules.Chorus)
                return false;
            for (int i = 0; i < rules.Drones.Count; i++)
            {
                var drone = rules.Drones[i];
                if (rules.At(drone.X, drone.Y) == CellKind.Pad)
                    continue;
                bool near = false;
                if (NearPad(rules, drone.X + 1, drone.Y)) near = true;
                if (NearPad(rules, drone.X - 1, drone.Y)) near = true;
                if (NearPad(rules, drone.X, drone.Y + 1)) near = true;
                if (NearPad(rules, drone.X, drone.Y - 1)) near = true;
                if (!near)
                    return false;
            }
            return true;
        }

        static bool NearPad(HiveRules rules, int x, int y)
        {
            return rules.Inside(x, y) && rules.At(x, y) == CellKind.Pad;
        }

        SpriteRenderer Add(Transform parent, string name, Sprite sprite, Color color, float scale, int order)
        {
            var renderer = NewRenderer(name, sprite, order);
            renderer.transform.SetParent(parent, false);
            renderer.color = color;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
            return renderer;
        }

        SpriteRenderer NewRenderer(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}

using System;
using System.Collections.Generic;

namespace Queenless.Rules
{
    public enum Dir
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 3,
        Right = 4
    }

    public enum CellKind
    {
        Wall,
        Floor,
        Silence,
        GoldSeal,
        TealSeal,
        GoldDoor,
        TealDoor,
        Exit,
        Pad
    }

    public enum Outcome
    {
        Ok,
        Win,
        Caught,
        Refused
    }

    public struct RuleResult
    {
        public Outcome Outcome;
        public string Reason;
        public bool Changed;

        public static RuleResult Ok(bool changed)
        {
            return new RuleResult { Outcome = Outcome.Ok, Reason = "", Changed = changed };
        }

        public static RuleResult Win()
        {
            return new RuleResult { Outcome = Outcome.Win, Reason = "", Changed = true };
        }

        public static RuleResult Caught()
        {
            return new RuleResult { Outcome = Outcome.Caught, Reason = "The mite takes the glow.", Changed = true };
        }

        public static RuleResult Refused(string reason)
        {
            return new RuleResult { Outcome = Outcome.Refused, Reason = reason, Changed = false };
        }
    }

    public sealed class Drone
    {
        public int Id;
        public int X;
        public int Y;
        public Dir March;
        public bool Mind;

        public Drone Copy()
        {
            return new Drone { Id = Id, X = X, Y = Y, March = March, Mind = Mind };
        }
    }

    public sealed class Mite
    {
        public int X;
        public int Y;
        public int Min;
        public int Max;
        public bool Horizontal;
        public int Dir;

        public Mite Copy()
        {
            return new Mite
            {
                X = X,
                Y = Y,
                Min = Min,
                Max = Max,
                Horizontal = Horizontal,
                Dir = Dir
            };
        }
    }

    public sealed class MiteSpec
    {
        public int X;
        public int Y;
        public int Min;
        public int Max;
        public bool Horizontal;
        public int Dir;

        public MiteSpec(int x, int y, int min, int max, bool horizontal, int dir)
        {
            X = x;
            Y = y;
            Min = min;
            Max = max;
            Horizontal = horizontal;
            Dir = dir;
        }
    }

    public struct MarchGhost
    {
        public int Id;
        public int X;
        public int Y;
    }

    /// <summary>
    /// Pure hive rules. No Unity types. One action in, one result out.
    /// </summary>
    public sealed class HiveRules
    {
        public int Width;
        public int Height;
        public bool Chorus;
        public CellKind[] Cells;
        public List<Drone> Drones = new List<Drone>();
        public List<Mite> Mites = new List<Mite>();

        public CellKind At(int x, int y)
        {
            return Cells[y * Width + x];
        }

        public bool Inside(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        public Drone Mind()
        {
            for (int i = 0; i < Drones.Count; i++)
            {
                if (Drones[i].Mind)
                    return Drones[i];
            }
            return null;
        }

        public Drone DroneById(int id)
        {
            for (int i = 0; i < Drones.Count; i++)
            {
                if (Drones[i].Id == id)
                    return Drones[i];
            }
            return null;
        }

        public Drone DroneAt(int x, int y)
        {
            for (int i = 0; i < Drones.Count; i++)
            {
                if (Drones[i].X == x && Drones[i].Y == y)
                    return Drones[i];
            }
            return null;
        }

        public Mite MiteAt(int x, int y)
        {
            for (int i = 0; i < Mites.Count; i++)
            {
                if (Mites[i].X == x && Mites[i].Y == y)
                    return Mites[i];
            }
            return null;
        }

        public bool GoldOpen()
        {
            return SealsHeld(CellKind.GoldSeal);
        }

        public bool TealOpen()
        {
            return SealsHeld(CellKind.TealSeal);
        }

        public bool HasKind(CellKind kind)
        {
            for (int i = 0; i < Cells.Length; i++)
            {
                if (Cells[i] == kind)
                    return true;
            }
            return false;
        }

        public bool AllOnPads()
        {
            if (Drones.Count == 0)
                return false;
            for (int i = 0; i < Drones.Count; i++)
            {
                if (At(Drones[i].X, Drones[i].Y) != CellKind.Pad)
                    return false;
            }
            return true;
        }

        public static void Step(Dir dir, ref int x, ref int y)
        {
            if (dir == Dir.Up) y -= 1;
            else if (dir == Dir.Down) y += 1;
            else if (dir == Dir.Left) x -= 1;
            else if (dir == Dir.Right) x += 1;
        }

        public static void AdvanceMite(Mite mite)
        {
            if (mite.Horizontal)
            {
                int next = mite.X + mite.Dir;
                if (next < mite.Min || next > mite.Max)
                {
                    mite.Dir = -mite.Dir;
                    next = mite.X + mite.Dir;
                }
                mite.X = next;
            }
            else
            {
                int next = mite.Y + mite.Dir;
                if (next < mite.Min || next > mite.Max)
                {
                    mite.Dir = -mite.Dir;
                    next = mite.Y + mite.Dir;
                }
                mite.Y = next;
            }
        }

        public void MiteNext(Mite mite, out int x, out int y)
        {
            var copy = mite.Copy();
            AdvanceMite(copy);
            x = copy.X;
            y = copy.Y;
        }

        public List<MarchGhost> MarchGhosts()
        {
            var plan = PlanFollow();
            var ghosts = new List<MarchGhost>();
            for (int i = 0; i < plan.Count; i++)
            {
                if (!plan[i].Moves)
                    continue;
                ghosts.Add(new MarchGhost { Id = plan[i].Drone.Id, X = plan[i].ToX, Y = plan[i].ToY });
            }
            return ghosts;
        }

        public RuleResult Move(Dir dir)
        {
            if (dir == Dir.None)
                return RuleResult.Refused("The way is blocked.");

            var mind = Mind();
            int nx = mind.X;
            int ny = mind.Y;
            Step(dir, ref nx, ref ny);
            if (!Inside(nx, ny) || Blocks(nx, ny))
                return RuleResult.Refused("The way is blocked.");

            var cell = At(nx, ny);
            if (cell == CellKind.Silence)
                return RuleResult.Refused("The mind cannot enter the silence.");
            if (DroneAt(nx, ny) != null)
                return RuleResult.Refused("The way is blocked.");

            int ox = mind.X;
            int oy = mind.Y;
            Dir oldMarch = mind.March;

            mind.X = nx;
            mind.Y = ny;
            mind.March = dir;
            if (IsHold(cell))
                mind.March = Dir.None;

            if (MiteAt(nx, ny) != null)
                return RuleResult.Caught();

            if (AnyDroneInClosedDoor())
            {
                mind.X = ox;
                mind.Y = oy;
                mind.March = oldMarch;
                return RuleResult.Refused("The seal would close on a body.");
            }

            if (cell == CellKind.Exit)
                return RuleResult.Win();

            return FinishTime(true, ox, oy);
        }

        public RuleResult Stay()
        {
            var mind = Mind();
            if (mind.March == Dir.None)
                return RuleResult.Ok(false);
            mind.March = Dir.None;
            if (Chorus && AllOnPads())
                return RuleResult.Win();
            return RuleResult.Ok(true);
        }

        public RuleResult Cycle(int delta)
        {
            var ordered = OrderedDrones();
            int idx = 0;
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].Mind)
                {
                    idx = i;
                    break;
                }
            }
            int next = idx + delta;
            int count = ordered.Count;
            next %= count;
            if (next < 0)
                next += count;
            return Inhabit(ordered[next].Id);
        }

        public RuleResult Inhabit(int id)
        {
            var target = DroneById(id);
            if (target == null)
                return RuleResult.Refused("The way is blocked.");
            if (target.Mind)
                return RuleResult.Ok(false);
            if (At(target.X, target.Y) == CellKind.Silence)
                return RuleResult.Refused("You cannot find them in the silence.");
            for (int i = 0; i < Drones.Count; i++)
                Drones[i].Mind = false;
            target.Mind = true;
            if (Chorus && AllOnPads())
                return RuleResult.Win();
            return RuleResult.Ok(true);
        }

        public RuleResult Follow()
        {
            var backup = Capture();
            var plan = PlanFollow();
            for (int i = 0; i < plan.Count; i++)
            {
                if (plan[i].ClearMarch)
                    plan[i].Drone.March = Dir.None;
            }
            for (int i = 0; i < plan.Count; i++)
            {
                if (!plan[i].Moves)
                    continue;
                plan[i].Drone.X = plan[i].ToX;
                plan[i].Drone.Y = plan[i].ToY;
            }
            for (int i = 0; i < plan.Count; i++)
            {
                if (!plan[i].Moves)
                    continue;
                if (IsHold(At(plan[i].Drone.X, plan[i].Drone.Y)))
                    plan[i].Drone.March = Dir.None;
            }

            if (AnyDroneInClosedDoor())
            {
                Restore(backup);
                return RuleResult.Refused("The seal would close on a body.");
            }

            var mind = Mind();
            return FinishTime(false, mind.X, mind.Y);
        }

        public RuleResult Wait()
        {
            var mind = Mind();
            return FinishTime(false, mind.X, mind.Y);
        }

        public HiveRules Clone()
        {
            var copy = new HiveRules();
            copy.Width = Width;
            copy.Height = Height;
            copy.Chorus = Chorus;
            copy.Cells = Cells;
            copy.Drones = new List<Drone>(Drones.Count);
            for (int i = 0; i < Drones.Count; i++)
                copy.Drones.Add(Drones[i].Copy());
            copy.Mites = new List<Mite>(Mites.Count);
            for (int i = 0; i < Mites.Count; i++)
                copy.Mites.Add(Mites[i].Copy());
            return copy;
        }

        public static HiveRules Parse(string map, bool chorus, MiteSpec[] mites)
        {
            if (map == null)
                throw new InvalidOperationException("This chamber failed to load.");

            var lines = map.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var rows = new List<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length == 0)
                    continue;
                rows.Add(lines[i]);
            }
            if (rows.Count == 0)
                throw new InvalidOperationException("This chamber failed to load.");

            int width = rows[0].Length;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Length != width)
                    throw new InvalidOperationException("This chamber failed to load.");
            }

            var rules = new HiveRules();
            rules.Width = width;
            rules.Height = rows.Count;
            rules.Chorus = chorus;
            rules.Cells = new CellKind[width * rows.Count];

            for (int y = 0; y < rows.Count; y++)
            {
                string row = rows[y];
                for (int x = 0; x < width; x++)
                {
                    char ch = row[x];
                    CellKind kind;
                    switch (ch)
                    {
                        case '#': kind = CellKind.Wall; break;
                        case '.': kind = CellKind.Floor; break;
                        case 'S': kind = CellKind.Silence; break;
                        case 'p': kind = CellKind.GoldSeal; break;
                        case 'q': kind = CellKind.TealSeal; break;
                        case 'd': kind = CellKind.GoldDoor; break;
                        case 'e': kind = CellKind.TealDoor; break;
                        case 'X': kind = CellKind.Exit; break;
                        case '*': kind = CellKind.Pad; break;
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                            kind = CellKind.Floor;
                            rules.Drones.Add(new Drone
                            {
                                Id = ch - '0',
                                X = x,
                                Y = y,
                                March = Dir.None,
                                Mind = ch == '1'
                            });
                            break;
                        default:
                            throw new InvalidOperationException("This chamber failed to load.");
                    }
                    rules.Cells[y * width + x] = kind;
                }
            }

            if (rules.Mind() == null)
                throw new InvalidOperationException("This chamber failed to load.");

            rules.Drones.Sort(delegate (Drone a, Drone b) { return a.Id.CompareTo(b.Id); });

            if (mites != null)
            {
                for (int i = 0; i < mites.Length; i++)
                {
                    var spec = mites[i];
                    rules.Mites.Add(new Mite
                    {
                        X = spec.X,
                        Y = spec.Y,
                        Min = spec.Min,
                        Max = spec.Max,
                        Horizontal = spec.Horizontal,
                        Dir = spec.Dir
                    });
                }
            }

            return rules;
        }

        RuleResult FinishTime(bool mindMoved, int fromX, int fromY)
        {
            if (TickCatches(mindMoved, fromX, fromY))
                return RuleResult.Caught();
            if (Chorus && AllOnPads())
                return RuleResult.Win();
            return RuleResult.Ok(true);
        }

        bool TickCatches(bool mindMoved, int fromX, int fromY)
        {
            var mind = Mind();
            int n = Mites.Count;
            var ox = new int[n];
            var oy = new int[n];
            for (int i = 0; i < n; i++)
            {
                ox[i] = Mites[i].X;
                oy[i] = Mites[i].Y;
                AdvanceMite(Mites[i]);
            }
            for (int i = 0; i < n; i++)
            {
                if (Mites[i].X == mind.X && Mites[i].Y == mind.Y)
                    return true;
                if (mindMoved && ox[i] == mind.X && oy[i] == mind.Y && Mites[i].X == fromX && Mites[i].Y == fromY)
                    return true;
            }
            return false;
        }

        bool Blocks(int x, int y)
        {
            var kind = At(x, y);
            if (kind == CellKind.Wall)
                return true;
            if (kind == CellKind.GoldDoor && !SealsHeld(CellKind.GoldSeal))
                return true;
            if (kind == CellKind.TealDoor && !SealsHeld(CellKind.TealSeal))
                return true;
            return false;
        }

        bool AnyDroneInClosedDoor()
        {
            bool gold = SealsHeld(CellKind.GoldSeal);
            bool teal = SealsHeld(CellKind.TealSeal);
            for (int i = 0; i < Drones.Count; i++)
            {
                var kind = At(Drones[i].X, Drones[i].Y);
                if (kind == CellKind.GoldDoor && !gold)
                    return true;
                if (kind == CellKind.TealDoor && !teal)
                    return true;
            }
            return false;
        }

        bool SealsHeld(CellKind seal)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (At(x, y) != seal)
                        continue;
                    if (DroneAt(x, y) == null)
                        return false;
                }
            }
            return true;
        }

        static bool IsHold(CellKind kind)
        {
            return kind == CellKind.GoldSeal || kind == CellKind.TealSeal || kind == CellKind.Pad;
        }

        List<Drone> OrderedDrones()
        {
            var ordered = new List<Drone>(Drones);
            ordered.Sort(delegate (Drone a, Drone b) { return a.Id.CompareTo(b.Id); });
            return ordered;
        }

        struct Intent
        {
            public Drone Drone;
            public int ToX;
            public int ToY;
            public bool Moves;
            public bool ClearMarch;
        }

        List<Intent> PlanFollow()
        {
            var intents = new List<Intent>();
            for (int i = 0; i < Drones.Count; i++)
            {
                var drone = Drones[i];
                if (drone.Mind || drone.March == Dir.None)
                    continue;

                int tx = drone.X;
                int ty = drone.Y;
                Step(drone.March, ref tx, ref ty);
                var intent = new Intent { Drone = drone, ToX = tx, ToY = ty };

                if (!Inside(tx, ty) || At(tx, ty) == CellKind.Wall || At(tx, ty) == CellKind.Exit)
                {
                    intent.ClearMarch = true;
                    intent.Moves = false;
                }
                else if (Blocks(tx, ty))
                {
                    intent.Moves = false;
                }
                else
                {
                    intent.Moves = true;
                }
                intents.Add(intent);
            }

            bool changed = true;
            while (changed)
            {
                changed = false;
                var stop = new List<int>();
                for (int i = 0; i < intents.Count; i++)
                {
                    if (!intents[i].Moves)
                        continue;

                    int claims = 0;
                    for (int j = 0; j < intents.Count; j++)
                    {
                        if (!intents[j].Moves)
                            continue;
                        if (intents[j].ToX == intents[i].ToX && intents[j].ToY == intents[i].ToY)
                            claims++;
                    }

                    bool blockedBody = false;
                    var occ = DroneAt(intents[i].ToX, intents[i].ToY);
                    if (occ != null)
                    {
                        bool movingAway = false;
                        for (int j = 0; j < intents.Count; j++)
                        {
                            if (intents[j].Moves && intents[j].Drone == occ)
                                movingAway = true;
                        }
                        if (!movingAway)
                            blockedBody = true;
                    }

                    if (claims > 1 || blockedBody)
                        stop.Add(i);
                }

                for (int s = 0; s < stop.Count; s++)
                {
                    var intent = intents[stop[s]];
                    intent.Moves = false;
                    intents[stop[s]] = intent;
                    changed = true;
                }
            }

            return intents;
        }

        struct Backup
        {
            public int[] Id;
            public int[] X;
            public int[] Y;
            public Dir[] March;
            public bool[] Mind;
            public int[] Mx;
            public int[] My;
            public int[] MDir;
        }

        Backup Capture()
        {
            var backup = new Backup();
            backup.Id = new int[Drones.Count];
            backup.X = new int[Drones.Count];
            backup.Y = new int[Drones.Count];
            backup.March = new Dir[Drones.Count];
            backup.Mind = new bool[Drones.Count];
            for (int i = 0; i < Drones.Count; i++)
            {
                backup.Id[i] = Drones[i].Id;
                backup.X[i] = Drones[i].X;
                backup.Y[i] = Drones[i].Y;
                backup.March[i] = Drones[i].March;
                backup.Mind[i] = Drones[i].Mind;
            }
            backup.Mx = new int[Mites.Count];
            backup.My = new int[Mites.Count];
            backup.MDir = new int[Mites.Count];
            for (int i = 0; i < Mites.Count; i++)
            {
                backup.Mx[i] = Mites[i].X;
                backup.My[i] = Mites[i].Y;
                backup.MDir[i] = Mites[i].Dir;
            }
            return backup;
        }

        void Restore(Backup backup)
        {
            for (int i = 0; i < Drones.Count; i++)
            {
                var drone = DroneById(backup.Id[i]);
                drone.X = backup.X[i];
                drone.Y = backup.Y[i];
                drone.March = backup.March[i];
                drone.Mind = backup.Mind[i];
            }
            for (int i = 0; i < Mites.Count; i++)
            {
                Mites[i].X = backup.Mx[i];
                Mites[i].Y = backup.My[i];
                Mites[i].Dir = backup.MDir[i];
            }
        }
    }
}

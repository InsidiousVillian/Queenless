using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Queenless.Rules
{
    public static class RuleTests
    {
        public static int Run(string levelDirectory, out string report)
        {
            var failures = new List<string>();
            CheckMiteBounce(failures);
            CheckMapsRectangular(failures);
            CheckLevelFiles(levelDirectory, failures);
            for (int n = 1; n <= LevelLibrary.Count; n++)
                CheckAuthor(n, failures);
            CheckSilenceRefused(failures);
            CheckSwitchInSilence(failures);
            CheckDoorStaysShut(failures);
            CheckSealClosesOnBody(failures);
            CheckSameCellKeepsMarch(failures);
            CheckSwap(failures);
            CheckChorusNotEarly(failures);
            CheckUndoSnapshot(failures);
            CheckEmptyBodySurvivesMite(failures);

            var sb = new StringBuilder();
            if (failures.Count == 0)
            {
                sb.AppendLine("PASS  " + LevelLibrary.Count + " author solutions and rule checks.");
                report = sb.ToString();
                return 0;
            }

            sb.AppendLine("FAIL  " + failures.Count + " check(s).");
            for (int i = 0; i < failures.Count; i++)
                sb.AppendLine(failures[i]);
            report = sb.ToString();
            return 1;
        }

        static void CheckMiteBounce(List<string> failures)
        {
            int[] expect = { 8, 7, 6, 5, 4, 3, 2, 3, 4, 5, 6, 7, 8, 9, 8 };
            var mite = new Mite { X = 8, Y = 0, Min = 2, Max = 9, Horizontal = true, Dir = -1 };
            var got = new List<int> { mite.X };
            for (int i = 0; i < expect.Length - 1; i++)
            {
                HiveRules.AdvanceMite(mite);
                got.Add(mite.X);
            }
            for (int i = 0; i < expect.Length; i++)
            {
                if (got[i] != expect[i])
                {
                    failures.Add("Mite bounce: expected " + Join(expect) + " got " + Join(got));
                    return;
                }
            }
        }

        static void CheckMapsRectangular(List<string> failures)
        {
            for (int n = 1; n <= LevelLibrary.Count; n++)
            {
                try
                {
                    var rules = LevelLibrary.Create(n);
                    if (rules.Mind() == null)
                        failures.Add("Chamber " + n + " has no mind.");
                }
                catch (Exception ex)
                {
                    failures.Add("Chamber " + n + " failed to parse: " + ex.Message);
                }
            }
        }

        static void CheckLevelFiles(string levelDirectory, List<string> failures)
        {
            if (string.IsNullOrEmpty(levelDirectory) || !Directory.Exists(levelDirectory))
            {
                failures.Add("Level directory missing: " + levelDirectory);
                return;
            }

            for (int n = 1; n <= LevelLibrary.Count; n++)
            {
                string path = Path.Combine(levelDirectory, n.ToString("00") + ".txt");
                if (!File.Exists(path))
                {
                    failures.Add("Missing map file " + path);
                    continue;
                }
                string disk = File.ReadAllText(path).Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n');
                string embedded = LevelLibrary.Map(n).Replace("\r\n", "\n").TrimEnd('\n');
                if (disk != embedded)
                    failures.Add("Map file " + path + " does not match the pasted chamber.");
            }
        }

        static void CheckAuthor(int number, List<string> failures)
        {
            var chamber = LevelLibrary.Get(number);
            var rules = LevelLibrary.Create(number);
            var trace = new StringBuilder();
            RuleResult result = RuleResult.Ok(false);
            string[] steps = chamber.Solution.Split(',');
            for (int i = 0; i < steps.Length; i++)
            {
                string token = steps[i].Trim();
                result = Apply(rules, token);
                trace.AppendLine(token + " -> " + result.Outcome + " " + result.Reason);
                trace.AppendLine(Dump(rules));
                if (result.Outcome == Outcome.Refused || result.Outcome == Outcome.Caught)
                    break;
                if (result.Outcome == Outcome.Win && i != steps.Length - 1)
                {
                    failures.Add("Chamber " + number + " won before the last step.\n" + trace);
                    return;
                }
            }

            if (result.Outcome != Outcome.Win)
                failures.Add("Chamber " + number + " " + chamber.Name + " author solution did not win (" + result.Outcome + " " + result.Reason + ").\n" + trace);
        }

        static void CheckSilenceRefused(List<string> failures)
        {
            var rules = LevelLibrary.Create(4);
            Apply(rules, "right");
            Apply(rules, "right");
            var result = Apply(rules, "right");
            var mind = rules.Mind();
            if (result.Outcome != Outcome.Refused || result.Reason != "The mind cannot enter the silence.")
                failures.Add("Silence step was not refused: " + result.Outcome + " " + result.Reason);
            if (mind.X != 3 || mind.Y != 1)
                failures.Add("Mind moved into silence at " + mind.X + "," + mind.Y);
        }

        static void CheckSwitchInSilence(List<string> failures)
        {
            var rules = LevelLibrary.Create(4);
            Apply(rules, "right");
            Apply(rules, "right");
            Apply(rules, "cycle");
            Apply(rules, "follow");
            var result = Apply(rules, "cycle");
            if (result.Outcome != Outcome.Refused || result.Reason != "You cannot find them in the silence.")
                failures.Add("Switch into silence: " + result.Outcome + " " + result.Reason);
            if (rules.Mind().Id != 2)
                failures.Add("Mind left the body after a refused switch.");
        }

        static void CheckDoorStaysShut(List<string> failures)
        {
            var rules = LevelLibrary.Create(6);
            string[] steps = "right, cycle, right, cycle, right, right, right, right, follow, follow, follow".Split(',');
            for (int i = 0; i < steps.Length; i++)
                Apply(rules, steps[i].Trim());
            if (rules.GoldOpen())
                failures.Add("Procession opened the door before both seals were held.");
            var down = Apply(rules, "down");
            if (down.Outcome != Outcome.Refused)
                failures.Add("Procession allowed a step through a door with one seal free.");
            Apply(rules, "follow");
            if (!rules.GoldOpen())
                failures.Add("Procession door stayed shut after both seals were held.");
        }

        static void CheckSealClosesOnBody(List<string> failures)
        {
            const string map =
                "#####\n" +
                "#1p.#\n" +
                "#.d.#\n" +
                "#.2.#\n" +
                "#####";
            var rules = HiveRules.Parse(map, false, null);
            Apply(rules, "right");
            Apply(rules, "cycle");
            Apply(rules, "up");
            Apply(rules, "cycle");
            var result = Apply(rules, "left");
            if (result.Outcome != Outcome.Refused || result.Reason != "The seal would close on a body.")
                failures.Add("Seal close: " + result.Outcome + " " + result.Reason + "\n" + Dump(rules));
            var holder = rules.DroneById(1);
            var inside = rules.DroneById(2);
            if (holder.X != 2 || holder.Y != 1 || inside.X != 2 || inside.Y != 2)
                failures.Add("Seal close moved someone anyway.\n" + Dump(rules));
        }

        static void CheckSameCellKeepsMarch(List<string> failures)
        {
            const string map =
                "######\n" +
                "#1.23#\n" +
                "######";
            var rules = HiveRules.Parse(map, false, null);
            rules.Inhabit(3);
            rules.DroneById(1).March = Dir.Right;
            rules.DroneById(2).March = Dir.Left;
            rules.Follow();
            var a = rules.DroneById(1);
            var b = rules.DroneById(2);
            if (a.X != 1 || a.March != Dir.Right || b.X != 3 || b.March != Dir.Left)
                failures.Add("Same-cell follow did not keep both marches.\n" + Dump(rules));
        }

        static void CheckSwap(List<string> failures)
        {
            const string map =
                "#######\n" +
                "#12..3#\n" +
                "#######";
            var rules = HiveRules.Parse(map, false, null);
            rules.Inhabit(3);
            rules.DroneById(1).March = Dir.Right;
            rules.DroneById(2).March = Dir.Left;
            rules.Follow();
            if (rules.DroneById(1).X != 2 || rules.DroneById(2).X != 1)
                failures.Add("Swap follow failed.\n" + Dump(rules));
        }

        static void CheckChorusNotEarly(List<string> failures)
        {
            var rules = LevelLibrary.Create(9);
            string[] steps = LevelLibrary.Get(9).Solution.Split(',');
            for (int i = 0; i < steps.Length - 1; i++)
            {
                var result = Apply(rules, steps[i].Trim());
                if (result.Outcome == Outcome.Win)
                {
                    failures.Add("Chorus won before every body was on a pad, at step " + steps[i].Trim());
                    return;
                }
            }
            if (rules.AllOnPads())
                failures.Add("Chorus pads were full before the last follow.");
            var last = Apply(rules, steps[steps.Length - 1].Trim());
            if (last.Outcome != Outcome.Win || !rules.AllOnPads())
                failures.Add("Chorus last follow did not win.\n" + Dump(rules));
        }

        static void CheckUndoSnapshot(List<string> failures)
        {
            var rules = LevelLibrary.Create(7);
            var before = rules.Clone();
            rules.Move(Dir.Right);
            if (rules.Mites[0].X == before.Mites[0].X)
                failures.Add("Mite did not tick on move.");
            if (before.Mites[0].X != 8 || before.Mites[0].Dir != -1 || before.Mind().X != 1)
                failures.Add("Clone did not keep the pre-move mite and mind.");
            if (rules.Mind().X != 2)
                failures.Add("Move did not step the mind.");
        }

        static void CheckEmptyBodySurvivesMite(List<string> failures)
        {
            var rules = LevelLibrary.Create(8);
            string[] steps = LevelLibrary.Get(8).Solution.Split(',');
            for (int i = 0; i < steps.Length; i++)
            {
                var result = Apply(rules, steps[i].Trim());
                if (result.Outcome == Outcome.Caught)
                {
                    failures.Add("Empty body was caught by the mite on '" + steps[i].Trim() + "'.\n" + Dump(rules));
                    return;
                }
            }
        }

        public static RuleResult Apply(HiveRules rules, string token)
        {
            switch (token)
            {
                case "up": return rules.Move(Dir.Up);
                case "down": return rules.Move(Dir.Down);
                case "left": return rules.Move(Dir.Left);
                case "right": return rules.Move(Dir.Right);
                case "cycle": return rules.Cycle(1);
                case "cycle-": return rules.Cycle(-1);
                case "follow": return rules.Follow();
                case "wait": return rules.Wait();
                case "stay": return rules.Stay();
                default: throw new InvalidOperationException("Unknown step " + token);
            }
        }

        public static string Dump(HiveRules rules)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < rules.Drones.Count; i++)
            {
                var d = rules.Drones[i];
                sb.Append("d" + d.Id + (d.Mind ? "*" : "") + "@" + d.X + "," + d.Y + " " + d.March + "  ");
            }
            for (int i = 0; i < rules.Mites.Count; i++)
            {
                var m = rules.Mites[i];
                sb.Append("mite@" + m.X + "," + m.Y + " dir" + m.Dir + "  ");
            }
            sb.Append(rules.GoldOpen() ? "gold-open" : "gold-shut");
            return sb.ToString();
        }

        static string Join(IList<int> values)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(values[i]);
            }
            return sb.ToString();
        }
    }
}

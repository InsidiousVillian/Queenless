namespace Queenless.Rules
{
    public sealed class ChamberDef
    {
        public int Number;
        public string Name;
        public string Quote;
        public string Hint;
        public string Map;
        public bool Chorus;
        public MiteSpec[] Mites;
        public string Solution;
    }

    public static class LevelLibrary
    {
        public const string Map1 =
            "#######\n" +
            "#..1..#\n" +
            "#.....#\n" +
            "#..X..#\n" +
            "#######";

        public const string Map2 =
            "#######\n" +
            "#1....#\n" +
            "#######\n" +
            "#2...X#\n" +
            "#######";

        public const string Map3 =
            "#########\n" +
            "#1..2..p#\n" +
            "####d####\n" +
            "#...X...#\n" +
            "#########";

        public const string Map4 =
            "###########\n" +
            "#1..SSSp..#\n" +
            "###########\n" +
            "#2........#\n" +
            "#####d#####\n" +
            "#....X....#\n" +
            "###########";

        public const string Map5 =
            "#########\n" +
            "#1...3p.#\n" +
            "####d####\n" +
            "#....2q.#\n" +
            "####e####\n" +
            "#......X#\n" +
            "#########";

        public const string Map6 =
            "###########\n" +
            "#1..SSp...#\n" +
            "###########\n" +
            "#2..SSp...#\n" +
            "###########\n" +
            "#3........#\n" +
            "#####d#####\n" +
            "#....X....#\n" +
            "###########";

        public const string Map7 =
            "############\n" +
            "#1........X#\n" +
            "####.#######\n" +
            "############";

        public const string Map8 =
            "##############\n" +
            "#1.SSSSp######\n" +
            "##############\n" +
            "#2...........#\n" +
            "######d#######\n" +
            "#.....X......#\n" +
            "##############";

        public const string Map9 =
            "#########\n" +
            "#1.SS*..#\n" +
            "#########\n" +
            "#2.SS*..#\n" +
            "#########\n" +
            "#3.*....#\n" +
            "#########\n" +
            "#4.*....#\n" +
            "#########";

        static readonly ChamberDef[] Chambers =
        {
            new ChamberDef
            {
                Number = 1,
                Name = "First Light",
                Quote = "You are awake. Move this body into the gold cell.",
                Hint = "WASD or arrows.",
                Map = Map1,
                Solution = "down, down"
            },
            new ChamberDef
            {
                Number = 2,
                Name = "Left Behind",
                Quote = "Not every body can be saved. Tab moves your mind into one that can still walk.",
                Hint = "Tab or E. Q goes back.",
                Map = Map2,
                Solution = "cycle, right, right, right, right"
            },
            new ChamberDef
            {
                Number = 3,
                Name = "The Seal",
                Quote = "A gold seal holds a gold door open, and only while a body stands on it.",
                Hint = "Leave someone on the seal. Walk the other through.",
                Map = Map3,
                Solution = "right, right, cycle, right, right, right, cycle, right, down, down"
            },
            new ChamberDef
            {
                Number = 4,
                Name = "Silence",
                Quote = "The purple silence unmakes any mind that touches it. Face a body into it, leave them, and press F. An empty body can cross. Space tells a body to stay, if they are facing the wrong way.",
                Hint = "Aim the upper body. Tab away. F steps them through.",
                Map = Map4,
                Solution = "right, right, cycle, right, right, right, right, follow, follow, follow, follow, down, down"
            },
            new ChamberDef
            {
                Number = 5,
                Name = "Every Body",
                Quote = "Gold opens gold. Teal opens teal. You will need every body. A seal cancels a walking order, so whoever you leave there stays.",
                Hint = "One body on each seal. The last one carries the thought out.",
                Map = Map5,
                Solution = "cycle-, right, cycle-, right, cycle-, right, right, right, down, down, down, down, right, right, right"
            },
            new ChamberDef
            {
                Number = 6,
                Name = "Procession",
                Quote = "Send both upper bodies through the silence. A body stops when it steps on a seal, so the one who arrives first will wait.",
                Hint = "Line them up facing right. F until both seals are held.",
                Map = Map6,
                Solution = "right, cycle, right, cycle, right, right, right, right, follow, follow, follow, follow, down, down"
            },
            new ChamberDef
            {
                Number = 7,
                Name = "What Hunts",
                Quote = "Mites hunt the glow. They cannot enter the alcove. W waits one beat. Let it pass, then go.",
                Hint = "Step into the alcove. Wait. Cross after it has gone by.",
                Map = Map7,
                Mites = new[] { new MiteSpec(8, 1, 2, 9, true, -1) },
                Solution = "right, right, right, down, wait, up, right, right, right, right, right, right"
            },
            new ChamberDef
            {
                Number = 8,
                Name = "Empty Things",
                Quote = "The upper hall is silence, and a mite hunts it. Leave a body there and let them walk. The mite does not see a body with no mind.",
                Hint = "Aim the upper body. F until they hold the seal. Take the lower hall yourself.",
                Map = Map8,
                Mites = new[] { new MiteSpec(4, 1, 3, 6, true, 1) },
                Solution = "right, cycle, follow, follow, follow, follow, follow, right, right, right, right, right, down, down"
            },
            new ChamberDef
            {
                Number = 9,
                Name = "Chorus",
                Quote = "Leave no one out. The queen cell answers only when every body is standing on a resonance pad.",
                Hint = "Aim the upper bodies into the silence. Stand the others on pads. F three times.",
                Map = Map9,
                Chorus = true,
                Solution = "right, cycle, right, cycle, right, cycle, right, right, follow, follow, follow"
            }
        };

        public static int Count
        {
            get { return Chambers.Length; }
        }

        public static ChamberDef Get(int number)
        {
            return Chambers[number - 1];
        }

        public static string Map(int number)
        {
            return Chambers[number - 1].Map;
        }

        public static HiveRules Create(int number)
        {
            var chamber = Get(number);
            return HiveRules.Parse(chamber.Map, chamber.Chorus, chamber.Mites);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Queenless.Rules;

namespace Queenless
{
    public enum Flow
    {
        Title,
        HowTo,
        Chambers,
        Credits,
        Intro,
        Play,
        Caught,
        Clear,
        Ending,
        Pause,
        Missing
    }

    public sealed class GameFlow : MonoBehaviour
    {
        enum Cmd
        {
            None,
            Move,
            Stay,
            Next,
            Prev,
            Follow,
            Wait,
            Undo,
            Restart,
            Inhabit
        }

        struct Queued
        {
            public Cmd Cmd;
            public Dir Dir;
            public int Id;
        }

        const string ClearedKey = "Queenless.Cleared";

        QueenlessInput input;
        HiveUi ui;
        HiveAudio audio;
        BoardView board;
        Camera cam;
        HiveRules rules;
        readonly Stack<HiveRules> history = new Stack<HiveRules>();

        int chamber = 1;
        int cleared;
        string chamberName = "";
        string quote = "";
        Flow flow = Flow.Title;
        Flow pausedFrom = Flow.Play;
        Flow howToReturn = Flow.Title;
        Flow afterBusy = Flow.Play;
        bool busy;
        float busyUntil;
        Queued queued;
        bool usedSwitch;
        bool usedFollow;
        bool usedWait;
        Vector3 camBase;
        float shake;
        int lastWidth;
        int lastHeight;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<GameFlow>() != null)
                return;
            var go = new GameObject("Queenless");
            go.AddComponent<GameFlow>();
        }

        void Awake()
        {
            SpriteFactory.Ensure();
            cam = Camera.main;
            if (cam == null)
            {
                var cameraGo = new GameObject("Main Camera");
                cameraGo.tag = "MainCamera";
                cam = cameraGo.AddComponent<Camera>();
                cameraGo.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Background;

            audio = gameObject.AddComponent<HiveAudio>();
            audio.Init();
            input = new QueenlessInput();
            ui = new HiveUi();
            ui.Build(this);
            var boardGo = new GameObject("Board");
            board = boardGo.AddComponent<BoardView>();
            board.Hide();
            cleared = PlayerPrefs.GetInt(ClearedKey, 0);
            GoTitle();
        }

        void OnDestroy()
        {
            if (input != null)
                input.Dispose();
        }

        void Update()
        {
            ui.Tick(Coach());
            TickCamera();
            if (Screen.width != lastWidth || Screen.height != lastHeight)
            {
                lastWidth = Screen.width;
                lastHeight = Screen.height;
                ui.Layout();
                if (rules != null && flow != Flow.Title)
                    Frame();
            }

            if (busy)
            {
                if (Time.time < busyUntil)
                {
                    if (flow == Flow.Play && afterBusy == Flow.Play)
                        CaptureQueue();
                    return;
                }
                busy = false;
                if (rules != null)
                    board.Settle(rules);
                if (afterBusy != Flow.Play)
                {
                    var next = afterBusy;
                    afterBusy = Flow.Play;
                    queued = default;
                    Go(next);
                    return;
                }
                FlushQueue();
                return;
            }

            if (flow == Flow.Title)
                return;

            if (flow != Flow.Play)
            {
                HandleModalKeys();
                return;
            }

            HandlePlayKeys();
        }

        public void EnterHive()
        {
            Load(1, true);
        }

        public void ShowHowTo()
        {
            howToReturn = flow == Flow.Pause ? Flow.Pause : Flow.Title;
            Go(Flow.HowTo);
        }

        public void ShowChambers()
        {
            Go(Flow.Chambers);
        }

        public void ShowCredits()
        {
            Go(Flow.Credits);
        }

        public void ToggleMute()
        {
            audio.ToggleMute();
            ui.SetMute(audio.Muted);
            if (flow == Flow.Pause)
                Go(Flow.Pause);
        }

        public void OnMind() { Try(Cmd.Next, Dir.None, 0); }
        public void OnStay() { Try(Cmd.Stay, Dir.None, 0); }
        public void OnFollow() { Try(Cmd.Follow, Dir.None, 0); }
        public void OnWait() { Try(Cmd.Wait, Dir.None, 0); }
        public void OnUndo() { Try(Cmd.Undo, Dir.None, 0); }
        public void OnRestart() { Try(Cmd.Restart, Dir.None, 0); }

        void Load(int number, bool intro)
        {
            chamber = number;
            var def = LevelLibrary.Get(number);
            chamberName = def.Name;
            quote = def.Quote;
            try
            {
                rules = LevelLibrary.Create(number);
            }
            catch (System.Exception)
            {
                rules = null;
                board.Hide();
                Go(Flow.Missing);
                return;
            }
            history.Clear();
            queued = default;
            busy = false;
            afterBusy = Flow.Play;
            board.Build(rules, number);
            ui.SetChamber(number, def.Name, def.Hint, RememberWait);
            ui.SetVerbs(RememberSwitch, RememberOrders, RememberOrders, RememberWait);
            Frame();
            Go(intro ? Flow.Intro : Flow.Play);
        }

        void GoTitle()
        {
            flow = Flow.Title;
            board.Hide();
            audio.PlayBed();
            ui.ShowTitle(audio.Muted);
        }

        void Go(Flow next)
        {
            flow = next;
            if (next == Flow.Title)
            {
                GoTitle();
                return;
            }

            bool hive = next == Flow.Play || next == Flow.Intro || next == Flow.Caught
                || next == Flow.Clear || next == Flow.Ending || next == Flow.Pause;
            bool howToOverPause = next == Flow.HowTo && howToReturn == Flow.Pause;
            if (hive || howToOverPause)
            {
                board.Show();
                ui.ShowPlay();
                ui.SetVerbs(RememberSwitch, RememberOrders, RememberOrders, RememberWait);
            }
            else
            {
                board.Hide();
                ui.ShowTitle(audio.Muted);
            }

            switch (next)
            {
                case Flow.Intro:
                    ui.ShowIntro(chamberName, quote, DismissIntro);
                    break;
                case Flow.Play:
                    break;
                case Flow.Caught:
                    ui.ShowCaught(UndoAction, RestartAction);
                    break;
                case Flow.Clear:
                    ui.ShowClear(NextChamber);
                    break;
                case Flow.Ending:
                    audio.PlayEndingBed();
                    ui.ShowEnding(ShowChambers);
                    break;
                case Flow.Pause:
                    ui.ShowPause(audio.Muted, Resume, RestartAction, ShowHowTo, ToggleMute, GoTitle);
                    break;
                case Flow.HowTo:
                    ui.ShowHowTo(Back);
                    break;
                case Flow.Credits:
                    ui.ShowCredits(Back);
                    break;
                case Flow.Chambers:
                    ui.ShowChambers(cleared, PickChamber, Back);
                    break;
                case Flow.Missing:
                    ui.ShowMissing(GoTitle);
                    break;
            }
        }

        void DismissIntro()
        {
            if (flow != Flow.Intro)
                return;
            Go(Flow.Play);
        }

        void NextChamber()
        {
            if (chamber >= LevelLibrary.Count)
            {
                Go(Flow.Ending);
                return;
            }
            Load(chamber + 1, true);
        }

        void PickChamber(int number)
        {
            Load(number, true);
        }

        void Resume()
        {
            Go(pausedFrom == Flow.Pause ? Flow.Play : pausedFrom);
        }

        void Back()
        {
            if (flow == Flow.HowTo && howToReturn == Flow.Pause)
            {
                Go(Flow.Pause);
                return;
            }
            GoTitle();
        }

        void OpenPause()
        {
            if (flow == Flow.Pause)
            {
                Resume();
                return;
            }
            pausedFrom = flow;
            Go(Flow.Pause);
        }

        void HandleModalKeys()
        {
            if (Pressed(input.Pause))
            {
                if (flow == Flow.Pause)
                    Resume();
                else if (flow == Flow.HowTo || flow == Flow.Chambers || flow == Flow.Credits || flow == Flow.Missing)
                    Back();
                else
                    OpenPause();
                return;
            }

            bool dismiss = Pressed(input.Confirm) || Pressed(input.Stay);
            if (!dismiss)
            {
                if (flow == Flow.Caught && Pressed(input.Undo))
                    UndoAction();
                else if (flow == Flow.Caught && Pressed(input.Restart))
                    RestartAction();
                return;
            }

            switch (flow)
            {
                case Flow.Intro: DismissIntro(); break;
                case Flow.Clear: NextChamber(); break;
                case Flow.Ending: ShowChambers(); break;
                case Flow.Pause: Resume(); break;
                case Flow.HowTo:
                case Flow.Chambers:
                case Flow.Credits:
                case Flow.Missing:
                    Back();
                    break;
                case Flow.Caught:
                    UndoAction();
                    break;
            }
        }

        void HandlePlayKeys()
        {
            if (Pressed(input.Pause))
            {
                OpenPause();
                return;
            }
            if (Pressed(input.Undo))
            {
                UndoAction();
                return;
            }
            if (Pressed(input.Restart))
            {
                RestartAction();
                return;
            }
            if (Pressed(input.Wait))
            {
                if (RememberWait)
                {
                    usedWait = true;
                    Do(delegate { return rules.Wait(); }, true);
                    return;
                }
                if (!WPressed())
                    Forgotten();
            }
            if (Pressed(input.NextBody))
            {
                Try(Cmd.Next, Dir.None, 0);
                return;
            }
            if (Pressed(input.PreviousBody))
            {
                Try(Cmd.Prev, Dir.None, 0);
                return;
            }
            if (Pressed(input.Follow))
            {
                Try(Cmd.Follow, Dir.None, 0);
                return;
            }
            if (Pressed(input.Stay))
            {
                Try(Cmd.Stay, Dir.None, 0);
                return;
            }

            Dir dir = ReadMove(true);
            if (dir != Dir.None)
            {
                Do(delegate { return rules.Move(dir); }, true);
                return;
            }

            if (Clicked())
                HandleClick();
        }

        void Try(Cmd cmd, Dir dir, int id)
        {
            if (flow != Flow.Play && cmd != Cmd.Undo && cmd != Cmd.Restart)
                return;
            if (busy)
            {
                queued = new Queued { Cmd = cmd, Dir = dir, Id = id };
                return;
            }
            switch (cmd)
            {
                case Cmd.Move: Do(delegate { return rules.Move(dir); }, true); break;
                case Cmd.Stay:
                    if (!RememberOrders) { Forgotten(); return; }
                    Do(delegate { return rules.Stay(); }, false);
                    break;
                case Cmd.Next:
                    if (!RememberSwitch) { Forgotten(); return; }
                    usedSwitch = true;
                    Do(delegate { return rules.Cycle(1); }, false);
                    break;
                case Cmd.Prev:
                    if (!RememberSwitch) { Forgotten(); return; }
                    usedSwitch = true;
                    Do(delegate { return rules.Cycle(-1); }, false);
                    break;
                case Cmd.Follow:
                    if (!RememberOrders) { Forgotten(); return; }
                    usedFollow = true;
                    Do(delegate { return rules.Follow(); }, true);
                    break;
                case Cmd.Wait:
                    if (!RememberWait) { Forgotten(); return; }
                    usedWait = true;
                    Do(delegate { return rules.Wait(); }, true);
                    break;
                case Cmd.Inhabit:
                    if (!RememberSwitch) { Forgotten(); return; }
                    usedSwitch = true;
                    Do(delegate { return rules.Inhabit(id); }, false);
                    break;
                case Cmd.Undo: UndoAction(); break;
                case Cmd.Restart: RestartAction(); break;
            }
        }

        void Do(System.Func<RuleResult> action, bool animate)
        {
            if (rules == null || flow != Flow.Play)
                return;
            var before = rules.Clone();
            RuleResult result = action();
            if (result.Outcome == Outcome.Refused)
            {
                bool silence = result.Reason == "The mind cannot enter the silence."
                    || result.Reason == "You cannot find them in the silence.";
                ui.Flash(result.Reason, silence);
                if (silence)
                {
                    audio.Thud();
                    shake = 0.14f;
                }
                return;
            }
            if (!result.Changed && result.Outcome == Outcome.Ok)
                return;

            history.Push(before);
            Sounds(before, rules);
            float duration = 0f;
            if (animate)
                duration = board.Animate(before, rules);
            else
                board.Settle(rules);

            afterBusy = Flow.Play;
            if (result.Outcome == Outcome.Caught)
            {
                audio.Caught();
                afterBusy = Flow.Caught;
                duration = Mathf.Max(duration, 0.12f);
            }
            else if (result.Outcome == Outcome.Win)
            {
                cleared |= 1 << chamber;
                PlayerPrefs.SetInt(ClearedKey, cleared);
                PlayerPrefs.Save();
                if (chamber == 9)
                    afterBusy = Flow.Ending;
                else
                {
                    audio.Clear();
                    afterBusy = Flow.Clear;
                }
                duration = Mathf.Max(duration, 0.2f);
            }
            else
            {
                ui.RestoreHint();
            }

            if (duration > 0f)
            {
                busy = true;
                busyUntil = Time.time + duration;
                queued = default;
            }
            else if (afterBusy != Flow.Play)
            {
                var next = afterBusy;
                afterBusy = Flow.Play;
                Go(next);
            }
        }

        void Sounds(HiveRules before, HiveRules after)
        {
            bool goldBefore = before.HasKind(CellKind.GoldDoor) && before.GoldOpen();
            bool goldAfter = after.HasKind(CellKind.GoldDoor) && after.GoldOpen();
            if (goldBefore != goldAfter)
                audio.Door(goldAfter);
            bool tealBefore = before.HasKind(CellKind.TealDoor) && before.TealOpen();
            bool tealAfter = after.HasKind(CellKind.TealDoor) && after.TealOpen();
            if (tealBefore != tealAfter)
                audio.Door(tealAfter);

            var was = before.Mind();
            var now = after.Mind();
            if (was.Id != now.Id)
                audio.Switch();
            else if (was.X != now.X || was.Y != now.Y)
                audio.Step();
            else
            {
                for (int i = 0; i < after.Drones.Count; i++)
                {
                    var a = after.Drones[i];
                    var b = before.DroneById(a.Id);
                    if (b.X != a.X || b.Y != a.Y)
                        audio.FollowStep();
                }
            }

            bool miteMoved = false;
            for (int i = 0; i < after.Mites.Count; i++)
            {
                if (after.Mites[i].X != before.Mites[i].X || after.Mites[i].Y != before.Mites[i].Y)
                    miteMoved = true;
            }
            if (miteMoved)
                audio.Skitter();
        }

        void UndoAction()
        {
            if (busy)
            {
                queued = new Queued { Cmd = Cmd.Undo };
                return;
            }
            if (flow != Flow.Play && flow != Flow.Caught)
                return;
            if (history.Count == 0)
                return;
            rules = history.Pop();
            board.Settle(rules);
            ui.RestoreHint();
            if (flow == Flow.Caught)
                Go(Flow.Play);
        }

        void RestartAction()
        {
            if (flow != Flow.Play && flow != Flow.Caught && flow != Flow.Pause)
                return;
            Load(chamber, false);
        }

        void Forgotten()
        {
            ui.Flash(HiveCopy.Forgotten, false);
        }

        void HandleClick()
        {
            int x;
            int y;
            if (!Pick(out x, out y))
                return;
            var drone = rules.DroneAt(x, y);
            if (drone != null)
            {
                Try(Cmd.Inhabit, Dir.None, drone.Id);
                return;
            }
            var mind = rules.Mind();
            int dx = x - mind.X;
            int dy = y - mind.Y;
            if (Mathf.Abs(dx) + Mathf.Abs(dy) != 1)
                return;
            Dir dir = Dir.None;
            if (dx == 1) dir = Dir.Right;
            else if (dx == -1) dir = Dir.Left;
            else if (dy == 1) dir = Dir.Down;
            else if (dy == -1) dir = Dir.Up;
            Try(Cmd.Move, dir, 0);
        }

        bool Pick(out int x, out int y)
        {
            x = 0;
            y = 0;
            if (Mouse.current == null || cam == null)
                return false;
            Vector2 screen = Mouse.current.position.ReadValue();
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            x = Mathf.RoundToInt(world.x);
            y = Mathf.RoundToInt(-world.y);
            return rules != null && rules.Inside(x, y);
        }

        void CaptureQueue()
        {
            if (Pressed(input.Undo)) queued = new Queued { Cmd = Cmd.Undo };
            else if (Pressed(input.Restart)) queued = new Queued { Cmd = Cmd.Restart };
            else if (Pressed(input.NextBody)) queued = new Queued { Cmd = Cmd.Next };
            else if (Pressed(input.PreviousBody)) queued = new Queued { Cmd = Cmd.Prev };
            else if (Pressed(input.Follow)) queued = new Queued { Cmd = Cmd.Follow };
            else if (Pressed(input.Stay)) queued = new Queued { Cmd = Cmd.Stay };
            else if (Pressed(input.Wait) && RememberWait) queued = new Queued { Cmd = Cmd.Wait };
            else
            {
                Dir dir = ReadMove(true);
                if (dir != Dir.None)
                    queued = new Queued { Cmd = Cmd.Move, Dir = dir };
            }
            if (Clicked())
            {
                int x;
                int y;
                if (Pick(out x, out y))
                {
                    var drone = rules.DroneAt(x, y);
                    if (drone != null)
                        queued = new Queued { Cmd = Cmd.Inhabit, Id = drone.Id };
                    else
                    {
                        var mind = rules.Mind();
                        int dx = x - mind.X;
                        int dy = y - mind.Y;
                        if (Mathf.Abs(dx) + Mathf.Abs(dy) == 1)
                        {
                            Dir dir = dx == 1 ? Dir.Right : dx == -1 ? Dir.Left : dy == 1 ? Dir.Down : Dir.Up;
                            queued = new Queued { Cmd = Cmd.Move, Dir = dir };
                        }
                    }
                }
            }
        }

        void FlushQueue()
        {
            if (flow != Flow.Play)
                return;
            if (queued.Cmd != Cmd.None)
            {
                var next = queued;
                queued = default;
                Try(next.Cmd, next.Dir, next.Id);
                return;
            }
            Dir held = ReadMove(false);
            if (held != Dir.None)
                Try(Cmd.Move, held, 0);
        }

        Dir ReadMove(bool pressedOnly)
        {
            if (input.Move == null)
                return Dir.None;
            Dir found = Dir.None;
            int count = 0;
            foreach (var control in input.Move.controls)
            {
                var button = control as ButtonControl;
                if (button == null)
                    continue;
                bool active = pressedOnly ? button.wasPressedThisFrame : button.isPressed;
                if (!active)
                    continue;
                string path = button.path;
                Dir dir = Dir.None;
                if (path.EndsWith("/w"))
                {
                    if (RememberWait)
                        continue;
                    dir = Dir.Up;
                }
                else if (path.EndsWith("/upArrow")) dir = Dir.Up;
                else if (path.EndsWith("/s") || path.EndsWith("/downArrow")) dir = Dir.Down;
                else if (path.EndsWith("/a") || path.EndsWith("/leftArrow")) dir = Dir.Left;
                else if (path.EndsWith("/d") || path.EndsWith("/rightArrow")) dir = Dir.Right;
                if (dir == Dir.None)
                    continue;
                if (found != Dir.None && found != dir)
                    return Dir.None;
                found = dir;
                count++;
            }
            return count > 0 ? found : Dir.None;
        }

        bool WPressed()
        {
            return Keyboard.current != null && Keyboard.current.wKey.wasPressedThisFrame;
        }

        static bool Pressed(UnityEngine.InputSystem.InputAction action)
        {
            return action != null && action.WasPressedThisFrame();
        }

        bool Clicked()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return false;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return false;
            return true;
        }

        int Coach()
        {
            if (flow != Flow.Play && flow != Flow.Intro)
                return 0;
            if (chamber == 2 && !usedSwitch) return 1;
            if (chamber == 4 && !usedFollow) return 2;
            if (chamber == 7 && !usedWait) return 3;
            return 0;
        }

        bool RememberSwitch { get { return chamber >= 2; } }
        bool RememberOrders { get { return chamber >= 4; } }
        bool RememberWait { get { return chamber >= 7; } }

        void Frame()
        {
            if (rules == null || cam == null)
                return;
            float uiTop = 0.11f;
            float uiBottom = Screen.width < 780 ? 0.38f : 0.27f;
            float usable = 1f - uiTop - uiBottom;
            float halfH = rules.Height * 0.5f + 1f;
            float halfW = rules.Width * 0.5f + 1f;
            float sizeY = halfH / Mathf.Max(0.2f, usable);
            float sizeX = halfW / Mathf.Max(0.2f, cam.aspect);
            cam.orthographicSize = Mathf.Max(sizeY, sizeX);
            float cx = (rules.Width - 1) * 0.5f;
            float cy = -(rules.Height - 1) * 0.5f;
            float usableCenter = (uiBottom + (1f - uiTop)) * 0.5f;
            float delta = usableCenter - 0.5f;
            camBase = new Vector3(cx, cy - delta * cam.orthographicSize * 2f, -10f);
            if (shake <= 0f)
                cam.transform.position = camBase;
        }

        void TickCamera()
        {
            if (cam == null)
                return;
            if (shake > 0f)
            {
                shake -= Time.deltaTime;
                float mag = Mathf.Max(0f, shake) * 0.28f;
                cam.transform.position = camBase + new Vector3(Mathf.Sin(Time.time * 90f) * mag, Mathf.Cos(Time.time * 73f) * mag, 0f);
            }
            else if (flow != Flow.Title)
            {
                cam.transform.position = camBase;
            }
        }

        [ContextMenu("Run Author Tests")]
        void RunAuthorTests()
        {
            string report;
            string dir = System.IO.Path.Combine(Application.dataPath, "Levels");
            RuleTests.Run(dir, out report);
            Debug.Log(report);
        }
    }
}

using LiveSplit.Model;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
namespace LiveSplit.APixelStory {
    public class APixelStoryComponent : IComponent {
        public string ComponentName { get { return "A Pixel Story Autosplitter"; } }
        protected TimerModel Model { get; set; }
        public IDictionary<string, Action> ContextMenuControls { get { return null; } }
        private APixelStoryMemory mem;
        private int currentSplit = 0;
        internal static string[] keys = { "Time", "Generation", "Scene", "Map", "Section", "IsDead", "MemoryCount", "Fadebox", "Deathrun" };
        internal static Dictionary<string, string> rooms = new Dictionary<string, string>() {
            {"Gen1Chal1","#1 "}, {"Gen1Chal2","#2 "}, {"Gen1Chal3","#3 "}, {"Gen1Chal4","#4 "}, {"Gen1Chal5","#5 "},
            {"Gen2Chal1","#6 "}, {"Gen2Chal2","#7 "}, {"Gen2Chal3","#8 "}, {"Gen2Chal4","#9 "}, {"Gen2Chal5","#10"},
            {"Gen3Chal1","#11"}, {"Gen3Chal2","#12"}, {"Gen3Chal3","#13"}, {"Gen3Chal4","#14"}, {"Gen3Chal5","#15"}
        };
        private Dictionary<string, string> currentValues = new Dictionary<string, string>();
        private Dictionary<string, TimeSpan> challengeTimes = new Dictionary<string, TimeSpan>();

        public APixelStoryComponent() {
            mem = new APixelStoryMemory();
            foreach (string key in keys) {
                currentValues[key] = "";
            }
        }

        private void GetValues() {
            if (!mem.HookProcess()) {
                if (currentSplit > 0) {
                    if (Model != null) { Model.Reset(); }
                }
                return;
            }

            bool shouldSplit = false;
            switch (currentSplit) {
                //Start
                case 0: shouldSplit = mem.GetScene() == "IntroCinematic"; break;
                //Firewall
                case 1: shouldSplit = mem.GetMapLevelName() == "Gen1Core1Narrative"; break;
                //Generation 2 & Valley
                case 2: shouldSplit = mem.GetScene() == "UpgradeRoom" ||
                    mem.GetSolveCount("Dispenser1") == 1; break;
                //Registry & Generation 2
                case 3: shouldSplit = mem.GetMapLevelName() == "Gen2Core1Narrative" ||
                    (mem.GetScene() == "UpgradeRoom" && mem.GetSolveCount("Dispenser2") == 1); break;
                //Generation 3 & Registry
                case 4: shouldSplit = (mem.GetScene() == "UpgradeRoom" && mem.GetSolveCount("Dispenser2") == 0) ||
                    (mem.GetMapLevelName() == "Gen2Core1Narrative" && mem.GetSolveCount("Dispenser3") == 1); break;
                //Recycle Bin & Generation 3
                case 5: shouldSplit = mem.GetMapLevelName() == "Gen3Core1Narrative" ||
                    (shouldSplit = mem.GetScene() == "UpgradeRoom" && mem.GetSolveCount("Dispenser4") == 1); break;
                //Generation 4 & Recycle Bin
                case 6: shouldSplit = (mem.GetScene() == "UpgradeRoom" && mem.GetSolveCount("Dispenser4") == 0) ||
                    (mem.GetMapLevelName() == "Gen3Core1Narrative" && mem.GetSolveCount("Dispenser4") == 1); break;
                //Core Ending & Forest Complete
                case 7: shouldSplit = (currentValues["Fadebox"] == "fadingGameIn" && mem.GetMapLevelName() == "Gen4Core1" && mem.GetFadeboxState() == FadeboxState.fadingGameOut) ||
                    mem.GetSolveCount("Dispenser5") == 1; break;
                //Generation 4
                case 8: shouldSplit = mem.GetScene() == "UpgradeRoom" && mem.GetSolveCount("Dispenser6") == 1; break;
                //Core Ending
                case 9: shouldSplit = currentValues["Fadebox"] == "fadingGameIn" && mem.GetMapLevelName() == "Gen4Core1" && mem.GetFadeboxState() == FadeboxState.fadingGameOut; break;
                //Ragequit
                case 10: shouldSplit = mem.GetSolveCount("Dispenser7") + mem.GetSolveCount("Dispenser8") == 1; break;
                //Padthrower
                case 11: shouldSplit = mem.GetSolveCount("Dispenser7") + mem.GetSolveCount("Dispenser8") == 2; break;
                //Horrible Truth
                case 12: shouldSplit = mem.GetLevelsCount("Gen4Core2", 2) == 1; break;
            }

            if (Model != null && Model.CurrentState.Run.Count == 15 && mem.IsDeathrun()) {
                string scene = mem.GetScene();
                shouldSplit = currentValues["Scene"] != scene;
                if (shouldSplit && scene != "MainMenu" && currentSplit < 15) {
                    IRun run = Model.CurrentState.Run;

                    challengeTimes.Clear();
                    TimeSpan start = TimeSpan.Zero;
                    for (int i = 0; i < run.Count; i++) {
                        ISegment seg = run[i];
                        if (seg.PersonalBestSplitTime.RealTime.HasValue) {
                            challengeTimes.Add(seg.Name, (seg.PersonalBestSplitTime.RealTime.Value - start));
                            start = seg.PersonalBestSplitTime.RealTime.Value;
                            if (!seg.SplitTime.RealTime.HasValue) {
                                seg.PersonalBestSplitTime = new Time();
                            }
                        } else {
                            challengeTimes.Add(seg.Name, TimeSpan.Zero);
                        }
                    }

                    string roomNumber = rooms[scene];
                    for (int i = run.Count - 1; i > currentSplit; i--) {
                        ISegment seg = run[i];
                        if (seg.Name.IndexOf(roomNumber) >= 0) {
                            run.RemoveAt(i);
                            run.Insert(currentSplit, seg);
                            break;
                        }
                    }

                    start = TimeSpan.Zero;
                    for (int i = 0; i < run.Count; i++) {
                        ISegment seg = run[i];
                        if (seg.PersonalBestSplitTime.RealTime.HasValue) {
                            start = seg.PersonalBestSplitTime.RealTime.Value;
                        } else {
                            seg.PersonalBestSplitTime = new Time(GetSplitTime(i), null);
                        }
                    }
                }
            }

            if (currentSplit > 0 && mem.GetScene() == "MainMenu") {
                if (Model != null) { Model.Reset(); }
            } else if (shouldSplit) {
                if (currentSplit == 0) {
                    if (Model != null) { Model.Start(); }
                } else {
                    if (Model != null) { Model.Split(); }
                }
            }

            string prev = "", curr = "";
            foreach (string key in keys) {
                prev = currentValues[key];
                switch (key) {
                    case "Generation": curr = mem.GetCurrentGen().ToString(); break;
                    case "Scene": curr = mem.GetScene(); break;
                    case "Map": curr = mem.GetMapLevelName(); break;
                    case "Section": curr = mem.GetCurrentSection().ToString(); break;
                    case "Time": curr = mem.GetGameTime().ToString("0.00"); break;
                    case "IsDead": curr = mem.GetIsDead().ToString(); break;
                    case "MemoryCount": curr = mem.GetCollectableCount("Chip").ToString(); break;
                    case "Fadebox": curr = mem.GetFadeboxState().ToString(); break;
                    case "Deathrun": curr = mem.IsDeathrun().ToString(); break;
                }
                if (!prev.Equals(curr)) {
                    WriteLog(DateTime.Now.ToString(@"HH\:mm\:ss.fff") + " | " + (Model != null ? Model.CurrentState.CurrentTime.RealTime.Value.ToString("G").Substring(3, 11) : DateTime.Now.ToString(@"HH\:mm\:ss.fff")) + ": " + key + ": ".PadRight(13 - key.Length, ' ') + prev.PadLeft(25, ' ') + " -> " + curr);

                    currentValues[key] = curr;
                }
            }
        }
        private TimeSpan GetSplitTime(int index) {
            IRun run = Model.CurrentState.Run;
            TimeSpan total = TimeSpan.Zero;
            for (int i = index; i >= 0; i--) {
                ISegment seg = run[i];
                total += challengeTimes[seg.Name];
            }
            return total;
        }

        public void Update(IInvalidator invalidator, LiveSplitState lvstate, float width, float height, LayoutMode mode) {
            if (Model == null) {
                Model = new TimerModel() { CurrentState = lvstate };
                lvstate.OnReset += OnReset;
                lvstate.OnPause += OnPause;
                lvstate.OnResume += OnResume;
                lvstate.OnStart += OnStart;
                lvstate.OnSplit += OnSplit;
                lvstate.OnUndoSplit += OnUndoSplit;
                lvstate.OnSkipSplit += OnSkipSplit;
            }

            GetValues();
        }

        public void OnReset(object sender, TimerPhase e) {
            currentSplit = 0;
            WriteLog("---------Reset----------------------------------");
        }
        public void OnResume(object sender, EventArgs e) {
            WriteLog("---------Resumed--------------------------------");
        }
        public void OnPause(object sender, EventArgs e) {
            WriteLog("---------Paused---------------------------------");
        }
        public void OnStart(object sender, EventArgs e) {
            currentSplit++;
            WriteLog("---------New Game-------------------------------");
        }
        public void OnUndoSplit(object sender, EventArgs e) {
            currentSplit--;
            WriteLog(DateTime.Now.ToString(@"HH\:mm\:ss.fff") + " | " + Model.CurrentState.CurrentTime.RealTime.Value.ToString("G").Substring(3, 11) + ": CurrentSplit: " + currentSplit.ToString().PadLeft(24, ' '));
        }
        public void OnSkipSplit(object sender, EventArgs e) {
            currentSplit++;
            WriteLog(DateTime.Now.ToString(@"HH\:mm\:ss.fff") + " | " + Model.CurrentState.CurrentTime.RealTime.Value.ToString("G").Substring(3, 11) + ": CurrentSplit: " + currentSplit.ToString().PadLeft(24, ' '));
        }
        public void OnSplit(object sender, EventArgs e) {
            currentSplit++;
            WriteLog(DateTime.Now.ToString(@"HH\:mm\:ss.fff") + " | " + Model.CurrentState.CurrentTime.RealTime.Value.ToString("G").Substring(3, 11) + ": CurrentSplit: " + currentSplit.ToString().PadLeft(24, ' '));
        }
        private void WriteLog(string data) {
            //using (StreamWriter wr = new StreamWriter("_APixelStory.log", true)) {
            //    wr.WriteLine(data);
            //}
        }

        public Control GetSettingsControl(LayoutMode mode) { return null; }
        public void SetSettings(XmlNode settings) { }
        public XmlNode GetSettings(XmlDocument document) { return null; }
        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion) { }
        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion) { }
        public float HorizontalWidth { get { return 0; } }
        public float MinimumHeight { get { return 0; } }
        public float MinimumWidth { get { return 0; } }
        public float PaddingBottom { get { return 0; } }
        public float PaddingLeft { get { return 0; } }
        public float PaddingRight { get { return 0; } }
        public float PaddingTop { get { return 0; } }
        public float VerticalHeight { get { return 0; } }
        public void Dispose() { }
    }
}
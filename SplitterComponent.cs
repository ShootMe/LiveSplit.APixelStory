#if !Info
using LiveSplit.Model;
using LiveSplit.UI;
using LiveSplit.UI.Components;
#endif
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
namespace LiveSplit.APixelStory {
#if !Info
	public class SplitterComponent : UI.Components.IComponent {
		public TimerModel Model { get; set; }
#else
	public class SplitterComponent {
#endif
		public string ComponentName { get { return "A Pixel Story Autosplitter"; } }
		public IDictionary<string, Action> ContextMenuControls { get { return null; } }
		private static string LOGFILE = "_APixelStory.log";
		internal static string[] keys = { "CurrentSplit", "State", "Time", "Generation", "Scene", "Map", "Section", "IsDead", "MemoryCount", "Fadebox", "Deathrun", "RoomsCompleted", "Loading" };
		private SplitterMemory mem;
		private int currentSplit = -1, lastLogCheck = 0;
		private bool hasLog = false;
		private Dictionary<string, string> currentValues = new Dictionary<string, string>();
#if !Info
		internal static Dictionary<string, string> rooms = new Dictionary<string, string>() {
			{"Gen1Chal1","#1 "}, {"Gen1Chal2","#2 "}, {"Gen1Chal3","#3 "}, {"Gen1Chal4","#4 "}, {"Gen1Chal5","#5 "},
			{"Gen2Chal1","#6 "}, {"Gen2Chal2","#7 "}, {"Gen2Chal3","#8 "}, {"Gen2Chal4","#9 "}, {"Gen2Chal5","#10"},
			{"Gen3Chal1","#11"}, {"Gen3Chal2","#12"}, {"Gen3Chal3","#13"}, {"Gen3Chal4","#14"}, {"Gen3Chal5","#15"}
		};
		private string previousScene, previousMap;
		private FadeboxState previousFadebox;
		private bool isLoading = false;
		public SplitterComponent(LiveSplitState state) {
#else
		public SplitterComponent() {
#endif
			mem = new SplitterMemory();
			foreach (string key in keys) {
				currentValues[key] = "";
			}

#if !Info
			if (state != null) {
				Model = new TimerModel() { CurrentState = state };
				Model.InitializeGameTime();
				Model.CurrentState.IsGameTimePaused = true;
				state.OnReset += OnReset;
				state.OnPause += OnPause;
				state.OnResume += OnResume;
				state.OnStart += OnStart;
				state.OnSplit += OnSplit;
				state.OnUndoSplit += OnUndoSplit;
				state.OnSkipSplit += OnSkipSplit;
			}
#endif
		}

		public void GetValues() {
			if (!mem.HookProcess()) { return; }

#if !Info
			HandleSplits();
#endif

			LogValues();
		}
#if !Info
		private void HandleSplits() {
			bool shouldSplit = false;

			FadeboxState fadebox = mem.GetFadeboxState();
			string scene = mem.GetScene();
			string mapName = mem.GetMapLevelName();
			switch (currentSplit) {
				//Start
				case -1: shouldSplit = scene == "IntroCinematic"; break;
				//Firewall
				case 0: shouldSplit = mapName == "Gen1Core1Narrative"; break;
				//Generation 2 & Valley
				case 1: shouldSplit = scene == "UpgradeRoom" || mem.GetSolveCount("Dispenser1") == 1; break;
				//Registry & Generation 2
				case 2: shouldSplit = mapName == "Gen2Core1Narrative" || (scene == "UpgradeRoom" && mem.GetSolveCount("Dispenser2") == 1); break;
				//Generation 3 & Registry
				case 3: shouldSplit = (scene == "UpgradeRoom" && mem.GetSolveCount("Dispenser2") == 0) || (mapName == "Gen2Core1Narrative" && mem.GetSolveCount("Dispenser3") == 1); break;
				//Recycle Bin & Generation 3
				case 4: shouldSplit = mapName == "Gen3Core1Narrative" || (shouldSplit = scene == "UpgradeRoom" && mem.GetSolveCount("Dispenser4") == 1); break;
				//Generation 4 & Recycle Bin
				case 5: shouldSplit = (scene == "UpgradeRoom" && mem.GetSolveCount("Dispenser4") == 0) || (mapName == "Gen3Core1Narrative" && mem.GetSolveCount("Dispenser4") == 1); break;
				//Core Ending & Forest Complete
				case 6: shouldSplit = (previousFadebox == FadeboxState.fadingGameIn && mapName == "Gen4Core1" && mem.GetFadeboxState() == FadeboxState.fadingGameOut) || mem.GetSolveCount("Dispenser5") == 1; break;
				//Generation 4
				case 7: shouldSplit = scene == "UpgradeRoom" && mem.GetSolveCount("Dispenser6") == 1; break;
				//Core Ending
				case 8: shouldSplit = previousFadebox == FadeboxState.fadingGameIn && mapName == "Gen4Core1" && mem.GetFadeboxState() == FadeboxState.fadingGameOut; break;
				//Ragequit
				case 9: shouldSplit = mem.GetSolveCount("Dispenser7") + mem.GetSolveCount("Dispenser8") == 1; break;
				//Padthrower
				case 10: shouldSplit = mem.GetSolveCount("Dispenser7") + mem.GetSolveCount("Dispenser8") == 2; break;
				//Horrible Truth
				case 11: shouldSplit = mem.GetLevelsCount("Gen4Core2", 2) == 1; break;
			}
			previousFadebox = fadebox;

			int roomsCompleted = mem.ChallengeRoomsCompleted();
			if (Model != null && !string.IsNullOrEmpty(previousScene) && Model.CurrentState.Run.Count == 15 && (Model.CurrentState.CurrentPhase == TimerPhase.Running || roomsCompleted < 15) && mem.IsDeathrun()) {
				shouldSplit = previousScene != scene || roomsCompleted == 15;

				int split = currentSplit + 1;
				if (shouldSplit && scene != "MainMenu" && split < 15) {
					IRun run = Model.CurrentState.Run;
					string roomNumber = rooms[scene];
					for (int i = run.Count - 1; i > split; i--) {
						ISegment seg = run[i];
						if (seg.Name.IndexOf(roomNumber) >= 0) {
							while(i-- > split) {
								SwitchSegments(i);
							}
							break;
						}
					}
				}
			}

			if (scene != previousScene) {
				isLoading = true;
			}
			if (isLoading && mapName != previousMap) {
				isLoading = false;
			}

			if (Model != null) {
				Model.CurrentState.IsGameTimePaused = isLoading;
			}
			HandleSplit(shouldSplit, scene == "MainMenu" && scene != previousScene);

			previousScene = scene;
			previousMap = mapName;
		}
		private void SwitchSegments(int segIndex) {
			IRun run = Model.CurrentState.Run;

			ISegment firstSegment = run[segIndex];
			ISegment secondSegment = run[segIndex + 1];

			int maxIndex = 0;
			for (int i = run.AttemptHistory.Count - 1; i >= 0; i--) {
				Attempt hist = run.AttemptHistory[i];
				if (hist.Index > maxIndex) {
					maxIndex = hist.Index;
				}
			}

			for (int runIndex = run.GetMinSegmentHistoryIndex(); runIndex <= maxIndex; runIndex++) {
				//Remove both segment history elements if one of them has a null time and the other has has a non null time
				Time firstHistory;
				bool firstExists = firstSegment.SegmentHistory.TryGetValue(runIndex, out firstHistory);
				Time secondHistory;
				bool secondExists = secondSegment.SegmentHistory.TryGetValue(runIndex, out secondHistory);

				if (firstExists && secondExists
					&& (firstHistory[TimingMethod.RealTime].HasValue != secondHistory[TimingMethod.RealTime].HasValue
					|| firstHistory[TimingMethod.GameTime].HasValue != secondHistory[TimingMethod.GameTime].HasValue)) {
					firstSegment.SegmentHistory.Remove(runIndex);
					secondSegment.SegmentHistory.Remove(runIndex);
				}
			}

			List<string> comparisonKeys = new List<string>(firstSegment.Comparisons.Keys);
			foreach (string comparison in comparisonKeys) {
				//Fix the comparison times based on the new positions of the two segments
				Time previousTime = segIndex > 0 ? run[segIndex - 1].Comparisons[comparison] : new Time(TimeSpan.Zero, TimeSpan.Zero);
				Time firstSegmentTime = firstSegment.Comparisons[comparison] - previousTime;
				Time secondSegmentTime = secondSegment.Comparisons[comparison] - firstSegment.Comparisons[comparison];
				secondSegment.Comparisons[comparison] = new Time(previousTime + secondSegmentTime);
				firstSegment.Comparisons[comparison] = new Time(secondSegment.Comparisons[comparison] + firstSegmentTime);
			}

			run.RemoveAt(segIndex + 1);
			run.Insert(segIndex, secondSegment);
		}
		private void HandleSplit(bool shouldSplit, bool shouldReset = false) {
			if (shouldReset) {
				if (currentSplit >= 0) {
					Model.Reset();
				}
			} else if (shouldSplit) {
				if (currentSplit < 0) {
					Model.Start();
				} else {
					Model.Split();
				}
			}
		}
#endif
		private void LogValues() {
			if (lastLogCheck == 0) {
				hasLog = File.Exists(LOGFILE);
				lastLogCheck = 300;
			}
			lastLogCheck--;

			if (hasLog || !Console.IsOutputRedirected) {
				string prev = string.Empty, curr = string.Empty;
				foreach (string key in keys) {
					prev = currentValues[key];

					switch (key) {
						case "CurrentSplit": curr = currentSplit.ToString(); break;
						case "Generation": curr = mem.GetCurrentGen().ToString(); break;
						case "Scene": curr = mem.GetScene(); break;
						case "Map": curr = mem.GetMapLevelName(); break;
						case "Section": curr = mem.GetCurrentSection().ToString(); break;
						case "Time": curr = mem.GetGameTime().ToString("0.00"); break;
						case "IsDead": curr = mem.GetIsDead().ToString(); break;
						case "MemoryCount": curr = mem.GetCollectableCount("Chip").ToString(); break;
						case "Fadebox": curr = mem.GetFadeboxState().ToString(); break;
						case "Deathrun": curr = mem.IsDeathrun().ToString(); break;
						case "RoomsCompleted": curr = mem.ChallengeRoomsCompleted().ToString(); break;
						case "Loading": curr = isLoading.ToString(); break;
						default: curr = string.Empty; break;
					}

					if (prev == null) { prev = string.Empty; }
					if (curr == null) { curr = string.Empty; }
					if (!prev.Equals(curr)) {
						WriteLogWithTime(key + ": ".PadRight(16 - key.Length, ' ') + prev.PadLeft(25, ' ') + " -> " + curr);

						currentValues[key] = curr;
					}
				}
			}
		}
		private void WriteLog(string data) {
			if (hasLog || !Console.IsOutputRedirected) {
				if (!Console.IsOutputRedirected) {
					Console.WriteLine(data);
				}
				if (hasLog) {
					using (StreamWriter wr = new StreamWriter(LOGFILE, true)) {
						wr.WriteLine(data);
					}
				}
			}
		}
		private void WriteLogWithTime(string data) {
#if !Info
			WriteLog(DateTime.Now.ToString(@"HH\:mm\:ss.fff") + (Model != null && Model.CurrentState.CurrentTime.RealTime.HasValue ? " | " + Model.CurrentState.CurrentTime.RealTime.Value.ToString("G").Substring(3, 11) : "") + ": " + data);
#else
			WriteLog(DateTime.Now.ToString(@"HH\:mm\:ss.fff") + ": " + data);
#endif
		}

#if !Info
		public void Update(IInvalidator invalidator, LiveSplitState lvstate, float width, float height, LayoutMode mode) {
			//Remove duplicate autosplitter componenets
			IList<ILayoutComponent> components = lvstate.Layout.LayoutComponents;
			bool hasAutosplitter = false;
			for (int i = components.Count - 1; i >= 0; i--) {
				ILayoutComponent component = components[i];
				if (component.Component is SplitterComponent) {
					if ((invalidator == null && width == 0 && height == 0) || hasAutosplitter) {
						components.Remove(component);
					}
					hasAutosplitter = true;
				}
			}

			GetValues();
		}

		public void OnReset(object sender, TimerPhase e) {
			currentSplit = -1;
			Model.CurrentState.IsGameTimePaused = true;
			WriteLog("---------Reset----------------------------------");
		}
		public void OnResume(object sender, EventArgs e) {
			WriteLog("---------Resumed--------------------------------");
		}
		public void OnPause(object sender, EventArgs e) {
			WriteLog("---------Paused---------------------------------");
		}
		public void OnStart(object sender, EventArgs e) {
			currentSplit = 0;
			Model.CurrentState.IsGameTimePaused = true;
			Model.CurrentState.SetGameTime(TimeSpan.FromSeconds(0));
			WriteLog("---------New Game-------------------------------");
		}
		public void OnUndoSplit(object sender, EventArgs e) {
			currentSplit--;
		}
		public void OnSkipSplit(object sender, EventArgs e) {
			currentSplit++;
		}
		public void OnSplit(object sender, EventArgs e) {
			currentSplit++;
		}
		public Control GetSettingsControl(LayoutMode mode) { return null; }
		public void SetSettings(XmlNode document) { }
		public XmlNode GetSettings(XmlDocument document) { return document.CreateElement("Settings"); }
		public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion) { }
		public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion) { }
#endif
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
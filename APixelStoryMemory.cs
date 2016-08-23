using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
namespace LiveSplit.APixelStory {
	public class APixelStoryMemory {
		private ProgramPointer gameStats;
		public Process Program { get; set; }
		public bool IsHooked { get; set; } = false;
		private DateTime lastHooked;

		public APixelStoryMemory() {
			gameStats = new ProgramPointer(this, "GameStats");
			lastHooked = DateTime.MinValue;
		}

		public int GetCollectableCount(string filter, params string[] filters) {
			IntPtr head = gameStats.Read<IntPtr>(0x3c, 0x14);
			int listSize = Program.Read<int>(head, 0x0C);

			int count = 0;
			for (int i = 0; i < listSize; i++) {
				IntPtr itemHead = Program.Read<IntPtr>(head, 0x08, 0x10 + (i * 4));
				bool collected = Program.Read<bool>(itemHead, 0x0c);
				if (!collected) { continue; }
				string name = Program.GetString(Program.Read<IntPtr>(itemHead, 0x08));
				if (AddToList(name, filter, filters)) {
					count++;
				}
			}

			return count;
		}
		private bool AddToList(string name, string filter, params string[] filters) {
			if (string.IsNullOrEmpty(filter) || name.Contains(filter)) {
				bool found = filters == null || filters.Length == 0;
				if (!found) {
					for (int j = 0; j < filters.Length; j++) {
						if (name.Contains(filters[j])) {
							return true;
						}
					}
				} else {
					return true;
				}
			}
			return false;
		}
		public int GetSolveCount(string filter, params string[] filters) {
			IntPtr head = gameStats.Read<IntPtr>(0x3c, 0x18);
			int listSize = Program.Read<int>(head, 0x0C);

			int count = 0;
			for (int i = 0; i < listSize; i++) {
				IntPtr itemHead = Program.Read<IntPtr>(head, 0x08, 0x10 + (i * 4));
				bool solved = Program.Read<bool>(itemHead, 0x0c);
				if (!solved) { continue; }

				string name = Program.GetString(Program.Read<IntPtr>(itemHead, 0x08));
				if (AddToList(name, filter, filters)) {
					count++;
				}
			}

			return count;
		}
		public int GetLevelsCount(string filter, int state) {
			IntPtr head = gameStats.Read<IntPtr>(0x3c, 0x10);
			int listSize = Program.Read<int>(head, 0x0C);

			int count = 0;
			for (int i = 0; i < listSize; i++) {
				IntPtr itemHead = Program.Read<IntPtr>(head, 0x08, 0x10 + (i * 4));

				string name = Program.GetString(Program.Read<IntPtr>(itemHead, 0x08));
				int value = Program.Read<int>(itemHead, 0x14, 0x08);
				if ((string.IsNullOrEmpty(filter) || name.Contains(filter)) && value == state) {
					count++;
				}
			}

			return count;
		}
		public bool IsDeathrun() {
			return gameStats.Read<bool>(0x9c);
		}
		public float GetGameTime() {
			return gameStats.Read<float>(0x3c, 0x4c);
		}
		public GameItem GetCurrentSection() {
			IntPtr head = gameStats.Read<IntPtr>(0x3c, 0x28);
			int listSize = Program.Read<int>(head, 0x0C);
			if (listSize <= 0) { return default(GameItem); }

			int index = gameStats.Read<int>(0x14, 0xc8, 0x30, 0x208);
			IntPtr itemHead = Program.Read<IntPtr>(head, 0x08, 0x10 + (index * 4));

			GameItem section = new GameItem();
			section.Name = Program.GetString(Program.Read<IntPtr>(itemHead, 0x08));
			section.Value = Program.Read<int>(itemHead, 0x0c);
			return section;
		}
		public Generation GetCurrentGen() {
			return (Generation)gameStats.Read<int>(0x5c);
		}
		public bool GetIsDead() {
			return gameStats.Read<bool>(0x14, 0xc8, 0x85);
		}
		public string GetMapLevelName() {
			return Program.GetString(gameStats.Read<IntPtr>(0x14, 0xc8, 0x30, 0x80));
		}
		public FadeboxState GetFadeboxState() {
			return (FadeboxState)gameStats.Read<int>(0x14, 0x184);
		}
		public string GetScene() {
			return Program.GetString(gameStats.Read<IntPtr>(0x24));
		}
		public int ChallengeRoomsCompleted() {
			return gameStats.Read<int>(0xa0);
		}

		public bool HookProcess() {
			if ((Program == null || Program.HasExited) && DateTime.Now > lastHooked.AddSeconds(1)) {
				lastHooked = DateTime.Now;
				Process[] processes = Process.GetProcessesByName("A Pixel Story");
				Program = processes.Length == 0 ? null : processes[0];
				IsHooked = true;
			}

			if (Program == null || Program.HasExited) {
				IsHooked = false;
			}

			return IsHooked;
		}
		public void Dispose() {
			if (Program != null) {
				Program.Dispose();
			}
		}
	}
	public enum FadeboxState {
		fadingGameIn,
		fadingGameOut,
		fadingGameOutIn,
		idle
	}
	public enum Generation {
		Gen1,
		Gen2,
		Gen3,
		Gen4,
		Multi
	}
	public struct GameItem {
		public string Name { get; set; }
		public int Value { get; set; }

		public override string ToString() {
			return string.IsNullOrEmpty(Name) ? string.Empty : Name;
		}
	}
	public class ProgramPointer {
		private static string[] versions = new string[1] { "v1.0" };
		private static Dictionary<string, Dictionary<string, string>> funcPatterns = new Dictionary<string, Dictionary<string, string>>() {
			{"v1.0", new Dictionary<string, string>() {
					{"GameStats", "558BEC535783EC108B7D08E8????????83EC0868????????50E8????????83C41085C00F84????????8B05????????83EC0C50E8????????83C41085C0741D83EC0C57|-24"}
			}},
		};
		private IntPtr pointer;
		public APixelStoryMemory Memory { get; set; }
		public string Name { get; set; }
		public bool IsStatic { get; set; }
		private int lastID;
		private DateTime lastTry;
		public ProgramPointer(APixelStoryMemory memory, string name) {
			this.Memory = memory;
			this.Name = name;
			this.IsStatic = true;
			lastID = memory.Program == null ? -1 : memory.Program.Id;
			lastTry = DateTime.MinValue;
		}

		public IntPtr Value {
			get {
				if (!Memory.IsHooked) {
					pointer = IntPtr.Zero;
				} else {
					GetPointer(ref pointer, Name);
				}
				return pointer;
			}
		}
		public T Read<T>(params int[] offsets) {
			if (!Memory.IsHooked) { return default(T); }
			return Memory.Program.Read<T>(Value, offsets);
		}
		public string ReadString(params int[] offsets) {
			if (!Memory.IsHooked) { return string.Empty; }
			IntPtr p = Memory.Program.Read<IntPtr>(Value, offsets);
			return Memory.Program.GetString(p);
		}
		private void GetPointer(ref IntPtr ptr, string name) {
			if (Memory.IsHooked) {
				if (Memory.Program.Id != lastID) {
					ptr = IntPtr.Zero;
					lastID = Memory.Program.Id;
				}
				if (ptr == IntPtr.Zero && DateTime.Now > lastTry.AddSeconds(1)) {
					lastTry = DateTime.Now;
					ptr = GetVersionedFunctionPointer(name);
					if (ptr != IntPtr.Zero) {
						if (IsStatic) {
							ptr = Memory.Program.Read<IntPtr>(ptr, 0, 0);
						} else {
							ptr = Memory.Program.Read<IntPtr>(ptr, 0);
						}
					}
				}
			}
		}
		public IntPtr GetVersionedFunctionPointer(string name) {
			foreach (string version in versions) {
				if (funcPatterns[version].ContainsKey(name)) {
					return Memory.Program.FindSignatures(funcPatterns[version][name])[0];
				}
			}
			return IntPtr.Zero;
		}
	}
}
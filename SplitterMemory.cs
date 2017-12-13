using System;
using System.Diagnostics;
namespace LiveSplit.APixelStory {
	public class SplitterMemory {
		private static ProgramPointer GameStats = new ProgramPointer(true, new ProgramSignature(PointerVersion.V1, "558BEC535783EC108B7D08E8????????83EC0868????????50E8????????83C41085C00F84????????8B05????????83EC0C50E8????????83C41085C0741D83EC0C57|-24"));
		public Process Program { get; set; }
		public bool IsHooked { get; set; } = false;
		private DateTime lastHooked;

		public SplitterMemory() {
			lastHooked = DateTime.MinValue;
		}

		public int GetCollectableCount(string filter, params string[] filters) {
			//GameStats.stats.save.collectables
			IntPtr head = (IntPtr)GameStats.Read<uint>(Program, 0x0, 0x3c, 0x14);
			int listSize = Program.Read<int>(head, 0x0C);

			int count = 0;
			for (int i = 0; i < listSize; i++) {
				IntPtr itemHead = (IntPtr)Program.Read<uint>(head, 0x08, 0x10 + (i * 4));
				bool collected = Program.Read<bool>(itemHead, 0x0c);
				if (!collected) { continue; }
				string name = Program.Read((IntPtr)Program.Read<uint>(itemHead, 0x08));
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
			//GameStats.stats.save.solve
			IntPtr head = (IntPtr)GameStats.Read<uint>(Program, 0x0, 0x3c, 0x18);
			int listSize = Program.Read<int>(head, 0x0C);

			int count = 0;
			for (int i = 0; i < listSize; i++) {
				IntPtr itemHead = (IntPtr)Program.Read<uint>(head, 0x08, 0x10 + (i * 4));
				bool solved = Program.Read<bool>(itemHead, 0x0c);
				if (!solved) { continue; }

				string name = Program.Read((IntPtr)Program.Read<uint>(itemHead, 0x08));
				if (AddToList(name, filter, filters)) {
					count++;
				}
			}

			return count;
		}
		public int GetLevelsCount(string filter, int state) {
			//GameStats.stats.save.levelsCompleted
			IntPtr head = (IntPtr)GameStats.Read<uint>(Program, 0x0, 0x3c, 0x10);
			int listSize = Program.Read<int>(head, 0x0C);

			int count = 0;
			for (int i = 0; i < listSize; i++) {
				IntPtr itemHead = (IntPtr)Program.Read<uint>(head, 0x08, 0x10 + (i * 4));

				string name = Program.Read((IntPtr)Program.Read<uint>(itemHead, 0x08));
				int value = Program.Read<int>(itemHead, 0x14, 0x08);
				if ((string.IsNullOrEmpty(filter) || name.Contains(filter)) && value == state) {
					count++;
				}
			}

			return count;
		}
		public bool IsDeathrun() {
			//GameStats.stats.deathrunActive
			return GameStats.Read<bool>(Program, 0x0, 0xa1);
		}
		public float GetGameTime() {
			//GameStats.stats.save.totalTimeInSave
			return GameStats.Read<float>(Program, 0x0, 0x3c, 0x4c);
		}
		public GameItem GetCurrentSection() {
			//GameStats.stats.save.sections
			IntPtr head = (IntPtr)GameStats.Read<uint>(Program, 0x0, 0x3c, 0x28);
			int listSize = Program.Read<int>(head, 0x0C);
			if (listSize <= 0) { return default(GameItem); }

			//GameStats.stats.ui.playerMechanics.map.currentSectionIndex
			int index = GameStats.Read<int>(Program, 0x0, 0x14, 0xc8, 0x30, 0x20c);
			IntPtr itemHead = Program.Read<IntPtr>(head, 0x08, 0x10 + (index * 4));

			GameItem section = new GameItem();
			section.Name = Program.Read((IntPtr)Program.Read<uint>(itemHead, 0x08));
			section.Value = Program.Read<int>(itemHead, 0x0c);
			return section;
		}
		public Generation GetCurrentGen() {
			//GameStats.stats.currentGen
			return (Generation)GameStats.Read<int>(Program, 0x0, 0x5c);
		}
		public bool GetIsDead() {
			//GameStats.stats.ui.playerMechanics.isDead
			return GameStats.Read<bool>(Program, 0x0, 0x14, 0xc8, 0x85);
		}
		public string GetMapLevelName() {
			//GameStats.stats.ui.playerMechanics.map.mapLevelName
			return Program.Read((IntPtr)GameStats.Read<uint>(Program, 0x0, 0x14, 0xc8, 0x30, 0x80));
		}
		public FadeboxState GetFadeboxState() {
			//GameStats.stats.ui.fadeState
			return (FadeboxState)GameStats.Read<int>(Program, 0x0, 0x14, 0x184);
		}
		public string GetScene() {
			//GameStats.stats.nextScene
			return Program.Read((IntPtr)GameStats.Read<uint>(Program, 0x0, 0x24));
		}
		public int ChallengeRoomsCompleted() {
			//GameStats.stats.roomsCompleted
			return GameStats.Read<int>(Program, 0x0, 0xa4);
		}
		public bool HookProcess() {
			if ((Program == null || Program.HasExited) && DateTime.Now > lastHooked.AddSeconds(1)) {
				lastHooked = DateTime.Now;
				Process[] processes = Process.GetProcessesByName("A Pixel Story");
				Program = processes.Length == 0 ? null : processes[0];
			}

			IsHooked = Program != null && !Program.HasExited;

			return IsHooked;
		}
		public void Dispose() {
			if (Program != null) {
				Program.Dispose();
			}
		}
	}
	public enum PointerVersion {
		V1
	}
	public class ProgramSignature {
		public PointerVersion Version { get; set; }
		public string Signature { get; set; }
		public ProgramSignature(PointerVersion version, string signature) {
			Version = version;
			Signature = signature;
		}
		public override string ToString() {
			return Version.ToString() + " - " + Signature;
		}
	}
	public class ProgramPointer {
		private int lastID;
		private DateTime lastTry;
		private ProgramSignature[] signatures;
		private int[] offsets;
		public IntPtr Pointer { get; private set; }
		public PointerVersion Version { get; private set; }
		public bool AutoDeref { get; private set; }

		public ProgramPointer(bool autoDeref, params ProgramSignature[] signatures) {
			AutoDeref = autoDeref;
			this.signatures = signatures;
			lastID = -1;
			lastTry = DateTime.MinValue;
		}
		public ProgramPointer(bool autoDeref, params int[] offsets) {
			AutoDeref = autoDeref;
			this.offsets = offsets;
			lastID = -1;
			lastTry = DateTime.MinValue;
		}

		public T Read<T>(Process program, params int[] offsets) where T : struct {
			GetPointer(program);
			return program.Read<T>(Pointer, offsets);
		}
		public string Read(Process program, params int[] offsets) {
			GetPointer(program);
			IntPtr ptr = (IntPtr)program.Read<uint>(Pointer, offsets);
			return program.Read(ptr);
		}
		public byte[] ReadBytes(Process program, int length, params int[] offsets) {
			GetPointer(program);
			return program.Read(Pointer, length, offsets);
		}
		public void Write<T>(Process program, T value, params int[] offsets) where T : struct {
			GetPointer(program);
			program.Write<T>(Pointer, value, offsets);
		}
		public void Write(Process program, byte[] value, params int[] offsets) {
			GetPointer(program);
			program.Write(Pointer, value, offsets);
		}
		public IntPtr GetPointer(Process program) {
			if ((program?.HasExited).GetValueOrDefault(true)) {
				Pointer = IntPtr.Zero;
				lastID = -1;
				return Pointer;
			} else if (program.Id != lastID) {
				Pointer = IntPtr.Zero;
				lastID = program.Id;
			}

			if (Pointer == IntPtr.Zero && DateTime.Now > lastTry.AddSeconds(1)) {
				lastTry = DateTime.Now;

				Pointer = GetVersionedFunctionPointer(program);
				if (Pointer != IntPtr.Zero) {
					if (AutoDeref) {
						Pointer = (IntPtr)program.Read<uint>(Pointer);
					}
				}
			}
			return Pointer;
		}
		private IntPtr GetVersionedFunctionPointer(Process program) {
			if (signatures != null) {
				for (int i = 0; i < signatures.Length; i++) {
					ProgramSignature signature = signatures[i];

					IntPtr ptr = program.FindSignatures(signature.Signature)[0];
					if (ptr != IntPtr.Zero) {
						Version = signature.Version;
						return ptr;
					}
				}
			} else {
				IntPtr ptr = (IntPtr)program.Read<uint>(program.MainModule.BaseAddress, offsets);
				if (ptr != IntPtr.Zero) {
					return ptr;
				}
			}

			return IntPtr.Zero;
		}
	}
}
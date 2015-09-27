using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
namespace LiveSplit.APixelStory {
    public class APixelStoryMemory {
        //These are checked in order, so they should be in reverse release order
        private string[] versions = new string[1] { "v1.0" };
        private Dictionary<string, Dictionary<string, string>> funcPatterns = new Dictionary<string, Dictionary<string, string>>() {
            {"v1.0", new Dictionary<string, string>() {
                    {"GameStats", "558BEC535783EC108B7D08E8????????83EC0868????????50E8????????83C41085C00F84????????8B05????????83EC0C50E8????????83C41085C0741D83EC0C57|-24"},
            }},
        };

        private Dictionary<string, string> versionedFuncPatterns = new Dictionary<string, string>();
        private int gameStats = -1;
        private Process proc;
        private bool isHooked = false;
        private DateTime hookedTime;

        public List<GameItem> GetSections() {
            int head = Memory.ReadValue<int>(proc, gameStats, 0x3c, 0x28);
            int listSize = Memory.ReadValue<int>(proc, head, 0x0C);

            List<GameItem> sections = new List<GameItem>();
            for (int i = 0; i < listSize; i++) {
                int itemHead = Memory.ReadValue<int>(proc, head, 0x08, 0x10 + (i * 4));

                GameItem section = new GameItem();
                section.Name = GetString(Memory.ReadValue<int>(proc, itemHead, 0x08));
                section.Value = Memory.ReadValue<int>(proc, itemHead, 0x0c);
                sections.Add(section);
            }

            return sections;
        }

        public List<GameItem> GetCollectables() {
            int head = Memory.ReadValue<int>(proc, gameStats, 0x3c, 0x14);
            int listSize = Memory.ReadValue<int>(proc, head, 0x0C);

            List<GameItem> collectables = new List<GameItem>();
            for (int i = 0; i < listSize; i++) {
                int itemHead = Memory.ReadValue<int>(proc, head, 0x08, 0x10 + (i * 4));
                bool collected = Memory.ReadValue<bool>(proc, itemHead, 0x0c);
                if (!collected) { continue; }
                int nameHead = Memory.ReadValue<int>(proc, itemHead, 0x08);

                GameItem collectable = new GameItem();
                collectable.Name = GetString(nameHead);
                collectables.Add(collectable);
            }

            return collectables;
        }

        public int GetCollectableCount(string filter, params string[] filters) {
            int head = Memory.ReadValue<int>(proc, gameStats, 0x3c, 0x14);
            int listSize = Memory.ReadValue<int>(proc, head, 0x0C);

            int count = 0;
            for (int i = 0; i < listSize; i++) {
                int itemHead = Memory.ReadValue<int>(proc, head, 0x08, 0x10 + (i * 4));
                bool collected = Memory.ReadValue<bool>(proc, itemHead, 0x0c);
                if (!collected) { continue; }
                string name = GetString(Memory.ReadValue<int>(proc, itemHead, 0x08));
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

        public List<GameItem> GetSolves() {
            int head = Memory.ReadValue<int>(proc, gameStats, 0x3c, 0x18);
            int listSize = Memory.ReadValue<int>(proc, head, 0x0C);

            List<GameItem> solves = new List<GameItem>();
            for (int i = 0; i < listSize; i++) {
                int itemHead = Memory.ReadValue<int>(proc, head, 0x08, 0x10 + (i * 4));
                bool solved = Memory.ReadValue<bool>(proc, itemHead, 0x0c);
                if (!solved) { continue; }

                GameItem solve = new GameItem();
                solve.Name = GetString(Memory.ReadValue<int>(proc, itemHead, 0x08));
                solves.Add(solve);
            }

            return solves;
        }

        public int GetSolveCount(string filter, params string[] filters) {
            int head = Memory.ReadValue<int>(proc, gameStats, 0x3c, 0x18);
            int listSize = Memory.ReadValue<int>(proc, head, 0x0C);

            int count = 0;
            for (int i = 0; i < listSize; i++) {
                int itemHead = Memory.ReadValue<int>(proc, head, 0x08, 0x10 + (i * 4));
                bool solved = Memory.ReadValue<bool>(proc, itemHead, 0x0c);
                if (!solved) { continue; }

                string name = GetString(Memory.ReadValue<int>(proc, itemHead, 0x08));
                if (AddToList(name, filter, filters)) {
                    count++;
                }
            }

            return count;
        }

        public List<GameItem> GetLevels() {
            int head = Memory.ReadValue<int>(proc, gameStats, 0x3c, 0x10);
            int listSize = Memory.ReadValue<int>(proc, head, 0x0C);

            List<GameItem> levels = new List<GameItem>();
            for (int i = 0; i < listSize; i++) {
                int itemHead = Memory.ReadValue<int>(proc, head, 0x08, 0x10 + (i * 4));

                GameItem level = new GameItem();
                level.Name = GetString(Memory.ReadValue<int>(proc, itemHead, 0x08));
                level.Value = Memory.ReadValue<int>(proc, itemHead, 0x14, 0x08);
            }

            return levels;
        }

        public int GetLevelsCount(string filter, int state) {
            int head = Memory.ReadValue<int>(proc, gameStats, 0x3c, 0x10);
            int listSize = Memory.ReadValue<int>(proc, head, 0x0C);

            int count = 0;
            for (int i = 0; i < listSize; i++) {
                int itemHead = Memory.ReadValue<int>(proc, head, 0x08, 0x10 + (i * 4));

                string name = GetString(Memory.ReadValue<int>(proc, itemHead, 0x08));
                int value = Memory.ReadValue<int>(proc, itemHead, 0x14, 0x08);
                if ((string.IsNullOrEmpty(filter) || name.Contains(filter)) && value == state) {
                    count++;
                }
            }

            return count;
        }

        public bool IsDeathrun() {
            return Memory.ReadValue<bool>(proc, gameStats, 0x9c);
        }

        public float GetGameTime() {
            return Memory.ReadValue<float>(proc, gameStats, 0x3c, 0x4c);
        }

        public GameItem GetCurrentSection() {
            int head = Memory.ReadValue<int>(proc, gameStats, 0x3c, 0x28);
            int listSize = Memory.ReadValue<int>(proc, head, 0x0C);
            if (listSize <= 0) { return default(GameItem); }

            int index = Memory.ReadValue<int>(proc, gameStats, 0x14, 0xc8, 0x30, 0x208);
            int itemHead = Memory.ReadValue<int>(proc, head, 0x08, 0x10 + (index * 4));

            GameItem section = new GameItem();
            section.Name = GetString(Memory.ReadValue<int>(proc, itemHead, 0x08));
            section.Value = Memory.ReadValue<int>(proc, itemHead, 0x0c);
            return section;
        }

        public Generation GetCurrentGen() {
            return (Generation)Memory.ReadValue<int>(proc, gameStats, 0x5c);
        }

        public bool GetIsDead() {
            return Memory.ReadValue<bool>(proc, gameStats, 0x14, 0xc8, 0x85);
        }

        public bool GetPaused() {
            return Memory.ReadValue<bool>(proc, gameStats, 0x61);
        }

        public string GetMapLevelName() {
            int head = Memory.ReadValue<int>(proc, gameStats, 0x14, 0xc8, 0x30, 0x80);
            return GetString(head);
        }

        public FadeboxState GetFadeboxState() {
            return (FadeboxState)Memory.ReadValue<int>(proc, gameStats, 0x14, 0x184);
        }

        public string GetScene() {
            int head = Memory.ReadValue<int>(proc, gameStats, 0x24);
            return GetString(head);
        }

        public bool GetHasHat() {
            return Memory.ReadValue<bool>(proc, gameStats, 0x14, 0xc8, 0x50, 0x1d);
        }

        public int ChallengeRoomsCompleted() {
            return Memory.ReadValue<int>(proc, gameStats, 0xa0);
        }

        private string GetString(int address) {
            if (address == 0) { return string.Empty; }
            int length = Memory.ReadValue<int>(proc, address, 0x8);
            return Encoding.Unicode.GetString(Memory.GetBytes(proc, address + 0x0C, 2 * length));
        }

        public bool HookProcess() {
            if (proc == null || proc.HasExited) {
                Process[] processes = Process.GetProcessesByName("A Pixel Story");
                if (processes.Length == 0) {
                    gameStats = -1;
                    isHooked = false;
                    return isHooked;
                }

                proc = processes[0];
                if (proc.HasExited) {
                    gameStats = -1;
                    isHooked = false;
                    return isHooked;
                }

                isHooked = true;
                hookedTime = DateTime.Now;
            }

            if (gameStats <= 0) {
                gameStats = GetVersionedFunctionPointer("GameStats");
                if (gameStats > 0) {
                    gameStats = Memory.ReadValue<int>(proc, gameStats, 0, 0);
                }
            }

            return isHooked;
        }

        public void Dispose() {
            // We want to appropriately dispose of the `Process` that is attached 
            // to the process to avoid being unable to close LiveSplit.
            if (proc != null) this.proc.Dispose();
        }

        public int GetVersionedFunctionPointer(string name) {
            // If we haven't already worked out what version is needed for this function signature, 
            // then iterate the versions checking each until we get a positive result. Store the
            // version so we don't need to search again in the future, and return the address.
            if (!versionedFuncPatterns.ContainsKey(name)) {
                foreach (string version in this.versions) {
                    if (funcPatterns[version].ContainsKey(name)) {
                        int[] addrs = Memory.FindMemorySignatures(proc, funcPatterns[version][name]);
                        if (addrs[0] != 0) {
                            versionedFuncPatterns[name] = version;
                            return addrs[0];
                        }
                    }
                }
            } else {
                string version = versionedFuncPatterns[name];
                int[] addrs = Memory.FindMemorySignatures(proc, funcPatterns[version][name]);
                return addrs[0];
            }

            return 0;
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
}
using LiveSplit.Model;
using LiveSplit.UI.Components;
using System;
using System.Reflection;
namespace LiveSplit.APixelStory {
    public class APixelStoryFactory : IComponentFactory {
        public string ComponentName { get { return "A Pixel Story Autosplitter v" + this.Version.ToString(); } }
        public string Description { get { return "Autosplitter for A Pixel Story"; } }
        public ComponentCategory Category { get { return ComponentCategory.Control; } }
        public IComponent Create(LiveSplitState state) { return new APixelStoryComponent(); }
        public string UpdateName { get { return this.ComponentName; } }
        public string UpdateURL { get { return ""; } }
        public string XMLURL { get { return ""; } }
        public Version Version { get { return Assembly.GetExecutingAssembly().GetName().Version; } }
    }
}
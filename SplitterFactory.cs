#if !Info
using LiveSplit.Model;
using LiveSplit.UI.Components;
using System;
using System.Reflection;
namespace LiveSplit.APixelStory {
	public class SplitterFactory : IComponentFactory {
		public string ComponentName { get { return "A Pixel Story Autosplitter v" + this.Version.ToString(); } }
		public string Description { get { return "Autosplitter for A Pixel Story"; } }
		public ComponentCategory Category { get { return ComponentCategory.Control; } }
		public IComponent Create(LiveSplitState state) { return new SplitterComponent(state); }
		public string UpdateName { get { return this.ComponentName; } }
		public string UpdateURL { get { return "https://raw.githubusercontent.com/ShootMe/LiveSplit.APixelStory/master/"; } }
		public string XMLURL { get { return this.UpdateURL + "Components/Updates.xml"; } }
		public Version Version { get { return Assembly.GetExecutingAssembly().GetName().Version; } }
	}
}
#endif
using System;
using System.Threading;
using System.Windows.Forms;
namespace LiveSplit.APixelStory {
	public partial class SplitterInfo {
		public static void Main(string[] args) {
			try {
				Thread t = new Thread(UpdateLoop);
				t.IsBackground = true;
				t.Start();
				Application.Run();
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}
		}
		private static void UpdateLoop() {
			SplitterComponent component = new SplitterComponent(null);
			while (true) {
				try {
					component.GetValues();
					Thread.Sleep(12);
				} catch { }
			}
		}
	}
}
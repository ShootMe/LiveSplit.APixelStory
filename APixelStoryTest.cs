using System;
using System.Threading;
namespace LiveSplit.APixelStory {
	public class APixelStoryTest {
		private static APixelStoryComponent comp;
		public static void Main(string[] args) {
			try {
				comp = new APixelStoryComponent();
				Thread t = new Thread(GetVals);
				t.IsBackground = true;
				t.Start();
				System.Windows.Forms.Application.Run();
			} catch (Exception e) {
				Console.WriteLine(e.ToString());
			}
		}
		private static void GetVals() {
			while (true) {
				try {
					comp.GetValues();

					Thread.Sleep(5);
				} catch (Exception e) {
					Console.WriteLine(e.ToString());
				}
			}
		}
	}
}
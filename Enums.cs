namespace LiveSplit.APixelStory {
	public struct GameItem {
		public string Name { get; set; }
		public int Value { get; set; }

		public override string ToString() {
			return string.IsNullOrEmpty(Name) ? string.Empty : Name;
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
}
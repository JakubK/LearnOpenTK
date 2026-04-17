namespace LearnOpenTK;

	[CLSCompliant(false)]
	public enum FtLoad : int
	{
		Default = 0x000000,

		NoScale = 0x000001,

		NoHinting = 0x000002,

		Render = 0x000004,

		NoBitmap = 0x000008,

		VerticalLayout = 0x000010,

		ForceAutohint = 0x000020,

		CropBitmap = 0x000040,

		Pedantic = 0x000080,

		[Obsolete("Ignored. Deprecated.")]
		IgnoreGlobalAdvanceWidth = 0x000200,

		NoRecurse = 0x000400,

		IgnoreTransform = 0x000800,

		Monochrome = 0x001000,

		LinearDesign = 0x002000,

		NoAutohint = 0x008000,

		Color = 0x100000,

		ComputeMetrics = 0x200000,

		AdvanceFlagFastOnly = 0x20000000
	}
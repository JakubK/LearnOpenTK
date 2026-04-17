namespace LearnOpenTK;

[Flags]
public enum OutlineFlags
{
	None = 0x0000,

	Owner = 0x0001,

	EvenOddFill = 0x0002,

	ReverseFill = 0x0004,

	IgnoreDropouts = 0x0008,

	SmartDropouts = 0x0010,

	IncludeStubs =		0x0020,

	HighPrecision =		0x0100,

	SinglePass =		0x0200
}
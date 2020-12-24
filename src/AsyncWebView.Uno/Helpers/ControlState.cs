using System;

namespace Chinook.AsyncWebView
{
	[Flags]
	internal enum ControlState
	{
		None = 0,

		Loaded = 1,
		Templated = 2,
		Sized = 4,

		All = Loaded | Templated | Sized
	}
}

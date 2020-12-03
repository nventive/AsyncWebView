#if __WASM__
using System.Threading;
using System.Threading.Tasks;

namespace chinook.AsyncWebView
{
	/// <summary>
	/// Implementation of <see cref="AsyncWebView" /> for WASM.
	/// </summary>
	public partial class AsyncWebView
	{
		private Task ClearCacheAndCookies(CancellationToken ct)
		{
			return Task.CompletedTask;
		}

		private Task ClearCookies(CancellationToken ct)
		{
			return Task.CompletedTask;
		}
	}
}
#endif

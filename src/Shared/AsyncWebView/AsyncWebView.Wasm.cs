#if __WASM__
using System.Threading;
using System.Threading.Tasks;

namespace AsyncWebView
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

		// Add any platform-specific initialization for your webview here
		private void InitializeWebView() { }
	}
}
#endif

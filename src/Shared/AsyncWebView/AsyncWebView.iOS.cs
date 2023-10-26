#if __IOS__
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Extensions.Logging;
using WebKit;

namespace AsyncWebView
{
	/// <summary>
	/// Implementation of <see cref="AsyncWebView"/> for iOS
	/// </summary>
	public partial class AsyncWebView
	{
		private async Task ClearCacheAndCookies(CancellationToken ct)
		{
			if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebug("Clearing cache and cookies.");
			}

			NSUrlCache.SharedCache.RemoveAllCachedResponses();

			await ClearCookies(ct);

			if (_logger.IsEnabled(LogLevel.Information))
			{
				_logger.LogInformation("Cleared cache and cookies.");
			}
		}

		private async Task ClearCookies(CancellationToken ct)
		{
			await WKWebsiteDataStore
				.DefaultDataStore
				.RemoveDataOfTypesAsync(
					WKWebsiteDataStore.AllWebsiteDataTypes,
					date: NSDate.FromTimeIntervalSince1970(0)
				);
		}

		// Add any platform-specific initialization for your webview here
		private void InitializeWebView() { }
	}
}
#endif

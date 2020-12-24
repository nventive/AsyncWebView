#if __ANDROID__
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Webkit;
using Microsoft.Extensions.Logging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Chinook.AsyncWebView
{
	/// <summary>
	/// Implementation of <see cref="AsyncWebView"/> for Android
	/// </summary>
	public partial class AsyncWebView
	{
		private async Task ClearCacheAndCookies(CancellationToken ct)
		{
			if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebug("Clearing cache and the cookies.");
			}

			var webView = GetChildren(_webView)
				.OfType<WebView>()
				.FirstOrDefault();

			webView?.ClearCache(includeDiskFiles: true);

			ClearCookies();

			if (_logger.IsEnabled(LogLevel.Information))
			{
				_logger.LogInformation("Cleared cache and cookies.");
			}
		}

		private void ClearCookies()
		{
			CookieManager.Instance.RemoveAllCookies(null);
			CookieManager.Instance.Flush();
		}

		private static IEnumerable<DependencyObject> GetChildren(DependencyObject obj)
		{
			var count = VisualTreeHelper.GetChildrenCount(obj);

			for (var i = 0; i < count; i++)
			{
				yield return VisualTreeHelper.GetChild(obj, i);
			}
		}
	}
}
#endif

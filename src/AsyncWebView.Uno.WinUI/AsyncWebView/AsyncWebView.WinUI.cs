#if WINDOWS10_0_18362_0_OR_GREATER
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Windows.Web.Http.Filters;

namespace AsyncWebView
{
	/// <summary>
	/// Implementation of <see cref="AsyncWebView"/> for Windows
	/// </summary>
	public partial class AsyncWebView
	{
		private async Task ClearCacheAndCookies(CancellationToken ct)
		{
			if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebug("Clearing cache and cookies.");
			}

			await WebView2
				.ClearTemporaryWebDataAsync()
				.AsTask(ct);

			ClearCookies();

			if (_logger.IsEnabled(LogLevel.Information))
			{
				_logger.LogInformation("Cleared cache and cookies.");
			}
		}

		private void ClearCookies()
		{
			if (SourceUri != null)
			{
				ClearCookies(SourceUri);
			}

			if (SourceMessage != null)
			{
				ClearCookies(SourceMessage.RequestUri);
			}
		}

		private void ClearCookies(Uri uri)
		{
			var filter = new HttpBaseProtocolFilter();
			var cookieManager = filter.CookieManager;
			var cookies = cookieManager.GetCookies(uri);

			foreach (var cookie in cookies)
			{
				cookieManager.DeleteCookie(cookie);
			}
		}

		private bool ProcessScriptNotification(NotifyEventArgs args)
		{
			if (ScriptNotificationCommand != null)
			{
				var parameter = new[]
				{
					args.CallingUri?.AbsoluteUri,
					args.Value
				};

				if (ScriptNotificationCommand.CanExecute(parameter))
				{
					ScriptNotificationCommand.Execute(parameter);

					return true;
				}
			}

			return false;
		}
	}
}
#endif

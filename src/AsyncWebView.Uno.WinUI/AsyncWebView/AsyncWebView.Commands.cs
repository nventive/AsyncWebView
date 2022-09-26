#if WINDOWS || __ANDROID__ || __IOS__
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AsyncWebView
{
	/// <summary>
	/// Implementation of <see href="AsyncWebView" /> active commands
	/// </summary>
	public partial class AsyncWebView
	{
		private void CreateCommands()
		{
			GoBackCommand = new WebViewCommand(GoBack, () => _webView.CanGoBack);
			GoForwardCommand = new WebViewCommand(GoForward, () => _webView.CanGoForward);
			RefreshCommand = new WebViewCommand(Refresh, () => true);
		}

		private void GoBack()
		{
			if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebug("Going back.");
			}

			_webView.GoBack();

			if (_logger.IsEnabled(LogLevel.Information))
			{
				_logger.LogInformation("Went back.");
			}
		}

		private void GoForward()
		{
			if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebug("Going forward.");
			}

			_webView.GoForward();

			if (_logger.IsEnabled(LogLevel.Information))
			{
				_logger.LogInformation("Went forward.");
			}
		}

		private void Refresh()
		{
			if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebug("Refreshing web view.");
			}

			if (_isLastErrorOnSource)
			{
				// When an error occurs while navigating to a newly pushed source, that
				// source doesn't get added to the stack. What we really want here is to
				// try that source again.
				Update();
			}
			else
			{
#if WINDOWS
				_webView.Reload();
#else
				_webView.Refresh();
#endif
			}

			if (_logger.IsEnabled(LogLevel.Information))
			{
				_logger.LogInformation("Refreshed web view.");
			}
		}
	}
}
#endif

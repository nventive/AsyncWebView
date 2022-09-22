#if WINUI || __ANDROID__ || __IOS__ || __WASM__
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Logging;
using Windows.System;
using System.Net.Http;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq;
#if __ANDROID__ || __IOS__ || __WASM__
using _WebView = Microsoft.UI.Xaml.Controls.WebView;
#else
using _WebView = Microsoft.UI.Xaml.Controls.WebView2;
#endif

namespace AsyncWebView
{
	/// <summary>
	/// Encapsulates a WebView control and adds multiple navigation handling scenarios.
	/// </summary>
	[TemplatePart(Name = WebViewPartName, Type = typeof(_WebView))]
	[TemplateVisualState(Name = VisualStates.Loading)]
	[TemplateVisualState(Name = VisualStates.Refreshing)]
	[TemplateVisualState(Name = VisualStates.Navigating)]
	[TemplateVisualState(Name = VisualStates.Ready)]
	[TemplateVisualState(Name = VisualStates.Error)]
	[TemplateVisualState(Name = VisualStates.ConnectivityError)]
	public partial class AsyncWebView : Control
	{
		private const string WebViewPartName = "PART_WebView";

		private static readonly Uri _blankPageUri = new Uri("about:blank");

		private readonly ControlStateManager _state = new ControlStateManager();
		private readonly CoreDispatcher _dispatcher;
		private readonly ILogger _logger;

		private _WebView _webView;
		private bool _isUpdating;
		private bool _isLastErrorOnSource;
		private bool _isNavigating;

		/// <summary>
		/// Gets or sets a source that reports if the device has
		/// network access. This observable is used to select which error visual state to go to when an
		/// error occurs navigating to an external source.
		/// </summary>
		public static Func<bool> IsNetworkAvailableSourceProvider { get; set; }

		public static Func<Uri, Task> ApplicationNavigation { get; set; }

		public static ILogger<AsyncWebView> Logger { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="AsyncWebView"/> class.
		/// </summary>
		public AsyncWebView()
		{
			DefaultStyleKey = typeof(AsyncWebView);
			
			_logger = Logger ?? NullLogger<AsyncWebView>.Instance;
			_dispatcher = Dispatcher;

			Loaded += OnLoaded;
			Unloaded += OnUnloaded;

			CreateCommands();

			_state.AddSubscription(ControlState.Loaded | ControlState.Templated, ControlState.Loaded, SubscribeToEvents);
			_state.AddSetTrigger(ControlState.Loaded | ControlState.Templated, Update);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_webView = GetTemplateChild(WebViewPartName) as _WebView ?? throw new ArgumentNullException(WebViewPartName);

			_state.SetState(ControlState.Templated);
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			try
			{
				if (OpenLinksUsingExternalBrowser)
				{
					NavigationMode = NavigationMode.External;
				}

				_state.SetState(ControlState.Loaded);
			}
			catch (Exception error)
			{
				if (_logger.IsEnabled(LogLevel.Error))
				{
					_logger.LogError(error, "Failed to load the web view.");
				}
			}
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			_state.ClearState(ControlState.Loaded);

			if (IsClearingOnUnload)
			{
				_ = _dispatcher.RunTaskAsync(CoreDispatcherPriority.Normal, async () =>
				{
					try
					{
						await ClearCacheAndCookies(CancellationToken.None);
					}
					catch (Exception error)
					{
						if (_logger.IsEnabled(LogLevel.Error))
						{
							_logger.LogError(error, "Failed to load the web view.");
						}
					}
				});
			}
		}

		private void Update()
		{
			if (!_state.HasState(ControlState.Loaded | ControlState.Templated))
			{
				return;
			}

			// The source might have changed before we were ready.
			if (Source != null)
			{
				OnSourceChanged(Source);
			}
		}

		private void UpdateVisualState(string name)
		{
			VisualStateManager.GoToState(this, name, true);
		}

		protected virtual bool OnNavigationStarting(WebViewNavigationStartingEventArgs args)
		{
			if (_logger.IsEnabled(LogLevel.Trace))
			{
				_logger.Trace($"Navigation to uri '{args?.Uri?.AbsoluteUri}' is starting.");
			}

			return true;
		}

		protected virtual void OnNavigationSucceeded(WebViewNavigationCompletedEventArgs args)
		{
			UpdateVisualState(VisualStates.Ready);

			if (_logger.IsEnabled(LogLevel.Information))
			{
				_logger.LogInformation($"The navigation to uri '{args?.Uri?.AbsoluteUri}' has succeeded.");
			}
		}

		protected virtual void OnNavigationFailed(WebViewNavigationCompletedEventArgs args)
		{
			var isNetworkAvailable = IsNetworkAvailableSourceProvider?.Invoke() ?? true;

			UpdateVisualState(isNetworkAvailable ? VisualStates.Error : VisualStates.ConnectivityError);

			if (_logger.IsEnabled(LogLevel.Error))
			{
				_logger.LogError($"The navigation to uri '{args?.Uri?.AbsoluteUri}' failed due to '{args?.WebErrorStatus}'.");
			}
		}

		protected virtual void OnNavigationCompleted(WebViewNavigationCompletedEventArgs args)
		{
			if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebug($"Handling the completed navigation to uri '{args?.Uri?.AbsoluteUri}'.");
			}

			if (CompletionCommand != null)
			{
				// We cannot use WebViewNavigationCompletedEventArgs directly, as it must be called from the UI Thread on Windows
				// The WebErrorStatus field is not provided, as it uses a different namespace in Uno and Windows.
				var commandArgs = new CompletionCommandArgs(this, args.IsSuccess, args.Uri);

				if (CompletionCommand.CanExecute(commandArgs))
				{
					CompletionCommand.Execute(commandArgs);
				}

				if (_logger.IsEnabled(LogLevel.Information))
				{
					_logger.LogInformation($"Handled the completed navigation to uri '{args?.Uri?.AbsoluteUri}'.");
				}
			}
		}

		private void OnSourceChanged(object source)
		{
			if (!_state.HasState(ControlState.Loaded | ControlState.Templated) || _isNavigating)
			{
				return;
			}

			if (source == null)
			{
				_isUpdating = true;

				UpdateVisualState(VisualStates.Loading);
				NavigateToBlank();

				return;
			}

			var sourceText = source as string;
			if (sourceText != null)
			{
				_isUpdating = true;

				UpdateVisualState(VisualStates.Loading);
				NavigateToString(sourceText);

				return;
			}

			var sourceUri = source as Uri;
			if (sourceUri != null)
			{
				_isUpdating = true;

				UpdateVisualState(VisualStates.Loading);
				NavigateToUri(sourceUri);

				return;
			}

			var sourceHttpRequestMessage = source as HttpRequestMessage;
			if (sourceHttpRequestMessage != null)
			{
				_isUpdating = true;

				UpdateVisualState(VisualStates.Loading);
				NavigateToMessage(sourceHttpRequestMessage);

				return;
			}

			if (_logger.IsEnabled(LogLevel.Error))
			{
				_logger.LogError($"Unsupported source type '{source?.GetType()?.Name}' (source: '{source}').");
			}
		}

		private void OnSourceHtmlChanged(string html)
		{
			Source = html;
		}

		private void OnSourceMessageChanged(HttpRequestMessage message)
		{
			Source = message;
		}

		private void OnSourceUriChanged(Uri uri)
		{
			Source = uri;
		}

		private void NavigateToBlank()
		{
			NavigateToUri(_blankPageUri);
		}

		private void NavigateToMessage(HttpRequestMessage message)
		{
			_ = _dispatcher.RunTaskAsync(CoreDispatcherPriority.Normal, async () =>
			{
				try
				{
					_webView?.NavigateWithHttpRequestMessage(message);
				}
				catch (Exception e)
				{
					if (_logger.IsEnabled(LogLevel.Error))
					{
						_logger.LogError(e, $"Failed to navigate to message.");
					}
				}
			});
		}

		private void NavigateToString(string content)
		{
			_ = _dispatcher.RunTaskAsync(CoreDispatcherPriority.Normal, async () =>
			{
				try
				{
					_webView?.NavigateToString(content);
				}
				catch (Exception e)
				{
					if (_logger.IsEnabled(LogLevel.Error))
					{
						_logger.LogError(e, $"Failed to navigate to content.");
					}
				}
			});
		}

		private void NavigateToUri(Uri uri)
		{
			_ = _dispatcher.RunTaskAsync(CoreDispatcherPriority.Normal, async () =>
			{
				try
				{
					_webView?.Navigate(uri);
				}
				catch (Exception e)
				{
					if (_logger.IsEnabled(LogLevel.Error))
					{
						_logger.LogError(e, $"Failed to navigate to uri.");
					}
				}
			});
		}

		private bool HandleLinkWithScheme(Uri uri)
		{
			var absolutePath = uri?.AbsoluteUri;

			if (absolutePath == null || !absolutePath.IsUrlAction())
			{
				return false;
			}

			_ = _dispatcher.RunTaskAsync(
					CoreDispatcherPriority.Normal,
					async () => await Launcher.LaunchUriAsync(uri)
				);

			return true;
		}

		private IDisposable SubscribeToEvents()
		{
			_webView.NavigationStarting += OnNavigationgStartingEvent;
			_webView.NavigationCompleted += OnNavigationCompletedEvent;
			_webView.NavigationFailed += OnNavigationFailedEvent;

#if WINDOWS_UWP
			_webView.ScriptNotify += OnScriptNotifyEvent;
#endif

			return Disposable.Create(() =>
			{
				_webView.NavigationStarting -= OnNavigationgStartingEvent;
				_webView.NavigationCompleted -= OnNavigationCompletedEvent;
				_webView.NavigationFailed -= OnNavigationFailedEvent;

#if WINDOWS_UWP
				_webView.ScriptNotify -= OnScriptNotifyEvent;
#endif
			});
		}

		private void OnNavigationCompletedEvent(_WebView sender, WebViewNavigationCompletedEventArgs args)
		{
			(GoBackCommand as WebViewCommand)?.RaiseCanExecuteChanged();
			(GoForwardCommand as WebViewCommand)?.RaiseCanExecuteChanged();

			ProcessNavigationCompleted(args);
		}

		private void OnNavigationgStartingEvent(_WebView sender, WebViewNavigationStartingEventArgs args)
		{
			ProcessNavigationStarting(args);
		}

		private void OnNavigationFailedEvent(object sender, WebViewNavigationFailedEventArgs e)
		{
			(GoBackCommand as WebViewCommand)?.RaiseCanExecuteChanged();
			(GoForwardCommand as WebViewCommand)?.RaiseCanExecuteChanged();
		}

#if WINDOWS_UWP
		private void OnScriptNotifyEvent(object sender, NotifyEventArgs args)
		{
			ProcessScriptNotification(args);
		}
#endif

		// This method has to be synchronous. Otherwise, changing the args.Cancel has no effect since it's evaluated synchronously.
		private void ProcessNavigationStarting(WebViewNavigationStartingEventArgs args)
		{
			var shouldStopNavigation = !OnNavigationStarting(args);
			if (shouldStopNavigation)
			{
				args.Cancel = true;
				return;
			}

			// If not navigating to an URI we are probably giving it raw HTML.
			if (args.Uri == null)
			{
				return;
			}

			var absoluteUri = args.Uri.AbsoluteUri;

			// We always ignore starting a navigation to about:blank.
			if (absoluteUri.Equals(_blankPageUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			// If the Uri is an action type ("tel:", "mailto:", etc), we ignore the navigation and open the appropriate app if any.
			if (absoluteUri.IsUrlAction())
			{
				var isSchemeSupported = HandleLinkWithScheme(args.Uri);

				// If action type was handled, cancel navigation
				if (isSchemeSupported)
				{
					args.Cancel = true;
					return;
				}
			}

			if (!_isUpdating)
			{
				if (NavigationMode != NavigationMode.Internal)
				{
					args.Cancel = true;

					if (NavigationMode == NavigationMode.Application)
					{
						_ = _dispatcher.RunTaskAsync(CoreDispatcherPriority.Normal, async () => await NavigateUsingApplication(args.Uri));
					}
					else if (NavigationMode == NavigationMode.External)
					{
						_ = _dispatcher.RunTaskAsync(CoreDispatcherPriority.Normal, async () => await NavigateUsingExternalBrowser(args.Uri));
					}

					return;
				}

				// This navigation does not come from a source change or explicit Navigate call.
				if (NavigationCommand != null)
				{
					if (NavigationCommand.CanExecute(args.Uri))
					{
						if (_logger.IsEnabled(LogLevel.Debug))
						{
							_logger.LogDebug($"Executing navigation to '{args?.Uri?.AbsoluteUri}' command.");
						}

						NavigationCommand.Execute(args.Uri);

						args.Cancel = true;
						return;
					}
				}

				// When we navigate to another page by clicking a link
				if (!(Source as Uri)?.Equals(args.Uri) ?? true)
				{
					_isNavigating = true;
					SourceUri = args.Uri;
					_isNavigating = false;
				}

				UpdateVisualState(VisualStates.Navigating);
			}
		}

		private async Task NavigateUsingExternalBrowser(Uri uri)
		{
			if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebug($"Navigating to uri '{uri?.AbsoluteUri}' using external browser.");
			}

			await _dispatcher.RunTaskAsync(CoreDispatcherPriority.Normal, () => Launcher.LaunchUriAsync(uri).AsTask());

			if (_logger.IsEnabled(LogLevel.Information))
			{
				_logger.LogInformation($"Navigated to uri '{uri?.AbsoluteUri}' using external browser.");
			}
		}

		private async Task NavigateUsingApplication(Uri uri)
		{
			if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebug($"Navigating to uri '{uri?.AbsoluteUri}' using application configuration.");
			}

			if (ApplicationNavigation == null)
			{
				throw new InvalidOperationException($"You must set {nameof(ApplicationNavigation)} in order to use the application navigation mode.");
			}

			await ApplicationNavigation.Invoke(uri);

			if (_logger.IsEnabled(LogLevel.Information))
			{
				_logger.LogInformation($"Navigated to uri '{uri?.AbsoluteUri}' using application configuration.");
			}
		}

		private bool ProcessNavigationCompleted(WebViewNavigationCompletedEventArgs args)
		{
			// iOS can perform navigations to about:blank right before an actual navigation. We must ignore them.
			var hasNonBlankSource = !((Source as Uri)?.Equals(_blankPageUri) ?? true);
			if (hasNonBlankSource && (args.Uri?.AbsoluteUri?.Equals(_blankPageUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase) ?? false))
			{
				return false;
			}

			_isLastErrorOnSource = !args.IsSuccess && _isUpdating;
			_isUpdating = false;

			if (args.IsSuccess)
			{
				OnNavigationSucceeded(args);
			}
			else
			{
				// While in error state page, the previous page is still loaded in background.
				// So it can output some sound (i.e : youtube.com) while being in error state.
				// Also if the page is heavy we will have a glimpse of it when navigating to another page.
				// For example : fallingfalling.com -> Error state page -> microsoft.com
				// While on Error state page we can hear the sound coming from the fallingfalling.com
				// When navigating to microsoft.com we have a glimpse at fallingfalling.com
				NavigateToBlank();

				OnNavigationFailed(args);
			}

			OnNavigationCompleted(args);

			return true;
		}

		/// <summary>
		/// Calls a javascript function in the context of the currently loaded web page. See remarks for best practice.
		/// </summary>
		/// <param name="ct">A cancellation token.</param>
		/// <param name="script">The javascript to invoke.</param>
		/// <param name="arguments">Optional arguments for your javascript function.</param>
		/// <returns>The result of the invoked function, if defined, otherwise null.</returns>
		/// <remarks><para>If your view-model needs to invoke javascript, the best place is from
		/// the <seealso cref="CompletionCommand"/> upon success, using <seealso cref="CompletionCommandArgs.InvokeScript(CancellationToken, string, string[])"/>.
		/// You're certain the WebView is ready to handle your call.</para>
		/// <para>In iOS and Android, the script must always be "eval".</para></remarks>
		public async Task<string> InvokeScriptAsync(CancellationToken ct, string script, params string[] arguments)
		{
			if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebug($"Invoking script with '{arguments?.Length}' arguments.");
			}

			if (_webView == null)
			{
				if (_logger.IsEnabled(LogLevel.Warning))
				{
					_logger.Warn("Unable to invoke the script as the web view is null.");
				}

				return null;
			}

			if (Source == null)
			{
				if (_logger.IsEnabled(LogLevel.Warning))
				{
					_logger.Warn("Unable to invoke the script as the source is null.");
				}

				return null;
			}

			// In Windows, you must specify the function to call, which can be "eval" to evaluate
			// javascript passed as the argument, or a function defined in the document. In Android
			// and iOS, it is assumed that "eval" is called.
#if __ANDROID__ || __IOS__
			if (!(script?.Equals("eval") ?? false))
			{
				if (_logger.IsEnabled(LogLevel.Error))
				{
					_logger.LogError("The script to call must be \"eval\".");
				}

				throw new NotSupportedException("The script to call must be \"eval\".");
			}

			script = arguments.FirstOrDefault();
			arguments = arguments.Skip(1).ToArray();
#endif

#if __ANDROID__ || __IOS__ || WINDOWS_UWP
			var result = await _webView.InvokeScriptAsync(script, arguments);
#else
			var result = await _webView.InvokeScriptAsync(script, arguments).AsTask();
#endif
			if (_logger.IsEnabled(LogLevel.Information))
			{
				_logger.LogInformation($"Invoked script with '{arguments?.Length}' arguments.");
			}

			return result;
		}
	}
}
#endif

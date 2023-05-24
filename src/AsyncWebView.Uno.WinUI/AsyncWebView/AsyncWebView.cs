#if WINDOWS || __ANDROID__ || __IOS__ || __WASM__
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Disposables;
using Uno.Logging;
using Windows.System;
using _WebView = Microsoft.UI.Xaml.Controls.WebView2;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using DispatcherQueuePriority = Microsoft.UI.Dispatching.DispatcherQueuePriority;
using HttpRequestMessage = Windows.Web.Http.HttpRequestMessage;
using NavigationCompletedEventArgs = Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs;
using NavigationFailedEventArgs = Microsoft.Web.WebView2.Core.CoreWebView2ProcessFailedEventArgs;
using NavigationStartingEventArgs = Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs;
using ScriptDialogOpeningEventArgs = Microsoft.Web.WebView2.Core.CoreWebView2ScriptDialogOpeningEventArgs;

namespace AsyncWebView;

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
	private readonly DispatcherQueue _dispatcher;
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
		_dispatcher = DispatcherQueue;

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
			_ = _dispatcher.RunAsync(DispatcherQueuePriority.Normal, async () =>
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

	protected virtual bool OnNavigationStarting(NavigationStartingEventArgs args)
	{
		if (_logger.IsEnabled(LogLevel.Trace))
		{
			_logger.Trace($"Navigation to uri '{args?.Uri}' is starting.");
		}

		return true;
	}

	protected virtual void OnNavigationSucceeded(NavigationCompletedEventArgs args)
	{
		UpdateVisualState(VisualStates.Ready);

		if (_logger.IsEnabled(LogLevel.Information))
		{
			_logger.LogInformation($"The navigation to uri '{SourceUri.AbsoluteUri}' has succeeded.");
		}
	}

	protected virtual void OnNavigationFailed(NavigationCompletedEventArgs args)
	{
		var isNetworkAvailable = IsNetworkAvailableSourceProvider?.Invoke() ?? true;

		UpdateVisualState(isNetworkAvailable ? VisualStates.Error : VisualStates.ConnectivityError);

		if (_logger.IsEnabled(LogLevel.Error))
		{
			_logger.LogError($"The navigation to uri '{SourceUri.AbsoluteUri}' failed due to '{args?.WebErrorStatus}'.");
		}
	}

	protected virtual void OnNavigationCompleted(NavigationCompletedEventArgs args)
	{
		if (_logger.IsEnabled(LogLevel.Debug))
		{
			_logger.LogDebug($"Handling the completed navigation to uri '{SourceUri.AbsoluteUri}'.");
		}

		if (CompletionCommand != null)
		{
			// We cannot use WebViewNavigationCompletedEventArgs directly, as it must be called from the UI Thread on Windows
			// The WebErrorStatus field is not provided, as it uses a different namespace in Uno and Windows.
			var commandArgs = new CompletionCommandArgs(this, args.IsSuccess, SourceUri);

			if (CompletionCommand.CanExecute(commandArgs))
			{
				CompletionCommand.Execute(commandArgs);
			}

			if (_logger.IsEnabled(LogLevel.Information))
			{
				_logger.LogInformation($"Handled the completed navigation to uri '{SourceUri.AbsoluteUri}'.");
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
		_ = _dispatcher.RunAsync(DispatcherQueuePriority.Normal, async () =>
		{
			try
			{
				await _webView.EnsureCoreWebView2Async();
				_webView?.CoreWebView2.NavigateToString(message.RequestUri.OriginalString);
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
		_ = _dispatcher.RunAsync(DispatcherQueuePriority.Normal, async () =>
		{
			try
			{
				await _webView.EnsureCoreWebView2Async();
				_webView?.CoreWebView2.NavigateToString(content);
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
		_ = _dispatcher.RunAsync(DispatcherQueuePriority.Normal, async () =>
		{
			try
			{
				await _webView.EnsureCoreWebView2Async();
				_webView?.CoreWebView2.Navigate(uri.OriginalString);

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
				DispatcherQueuePriority.Normal,
				async () => await Launcher.LaunchUriAsync(uri)
			);

		return true;
	}

	private IDisposable SubscribeToEvents()
	{
		_webView.NavigationStarting += OnNavigationgStartingEvent;
		_webView.NavigationCompleted += OnNavigationCompletedEvent;
		_webView.CoreProcessFailed += OnNavigationFailedEvent;
#if WINDOWS
		_ = _dispatcher.RunAsync(DispatcherQueuePriority.Normal, async () => {


			await _webView.EnsureCoreWebView2Async();
			_webView.CoreWebView2.ScriptDialogOpening += OnScriptNotifyEvent;
		});
#endif


		return Disposable.Create(() =>
		{
			_webView.NavigationStarting -= OnNavigationgStartingEvent;
			_webView.NavigationCompleted -= OnNavigationCompletedEvent;
			_webView.CoreProcessFailed -= OnNavigationFailedEvent;

#if WINDOWS
			_webView.CoreWebView2.ScriptDialogOpening -= OnScriptNotifyEvent;
#endif
		});
	}

	private void OnNavigationCompletedEvent(_WebView sender, NavigationCompletedEventArgs args)
	{
		(GoBackCommand as WebViewCommand)?.RaiseCanExecuteChanged();
		(GoForwardCommand as WebViewCommand)?.RaiseCanExecuteChanged();

		ProcessNavigationCompleted(args);
	}

	private void OnNavigationgStartingEvent(_WebView sender, NavigationStartingEventArgs args)
	{
		ProcessNavigationStarting(args);
	}

	private void OnNavigationFailedEvent(_WebView sender, NavigationFailedEventArgs e)
	{
		(GoBackCommand as WebViewCommand)?.RaiseCanExecuteChanged();
		(GoForwardCommand as WebViewCommand)?.RaiseCanExecuteChanged();
	}

#if WINDOWS
	private void OnScriptNotifyEvent(object sender, ScriptDialogOpeningEventArgs args)
	{
		ProcessScriptNotification(args);
	}
#endif

	// This method has to be synchronous. Otherwise, changing the args.Cancel has no effect since it's evaluated synchronously.
	private void ProcessNavigationStarting(NavigationStartingEventArgs args)
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

		Uri uri = new Uri(args.Uri);
		var absoluteUri = (Source as Uri).AbsoluteUri;

		// We always ignore starting a navigation to about:blank.
		if (absoluteUri.Equals(_blankPageUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		// If the Uri is an action type ("tel:", "mailto:", etc), we ignore the navigation and open the appropriate app if any.
		if (absoluteUri.IsUrlAction())
		{

			var isSchemeSupported = HandleLinkWithScheme(uri);

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
					_ = _dispatcher.RunAsync(DispatcherQueuePriority.Normal, async () => await NavigateUsingApplication(uri));
				}
				else if (NavigationMode == NavigationMode.External)
				{
					_ = _dispatcher.RunAsync(DispatcherQueuePriority.Normal, async () => await NavigateUsingExternalBrowser(uri));
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
						_logger.LogDebug($"Executing navigation to '{args.Uri}' command.");
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
				SourceUri = _webView.Source;
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

		await _dispatcher.RunTaskAsync(DispatcherQueuePriority.Normal, () => Launcher.LaunchUriAsync(uri).AsTask());

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

	private bool ProcessNavigationCompleted(NavigationCompletedEventArgs args)
	{
		var sourceUri = (Source as Uri);
		// iOS can perform navigations to about:blank right before an actual navigation. We must ignore them.
		var hasNonBlankSource = !(sourceUri?.Equals(_blankPageUri) ?? true);
		if (hasNonBlankSource &&
			(sourceUri?.AbsoluteUri?.Equals(_blankPageUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase) ?? false))
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

#if __ANDROID__ || __IOS__
		var result = await _webView.ExecuteScriptAsync(script);
#else
		var result = await _webView.ExecuteScriptAsync(script);
#endif
		if (_logger.IsEnabled(LogLevel.Information))
		{
			_logger.LogInformation($"Invoked script with '{arguments?.Length}' arguments.");
		}

		return result;
	}
}
#endif

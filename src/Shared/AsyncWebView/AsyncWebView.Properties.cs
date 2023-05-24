#if WINDOWS_UWP || __ANDROID__ || __IOS__ || __WASM__ || WINUI
using System;
#if WINUI
using Microsoft.UI.Xaml;
#else
using Windows.UI.Xaml;
#endif
using System.Windows.Input;
#if WINDOWS_UWP
using Windows.Web.Http;
#else
using System.Net.Http;
#endif

namespace AsyncWebView
{
	/// <summary>
	/// Implementation of <see cref="AsyncWebView"/> properties
	/// </summary>
	public partial class AsyncWebView
	{
		#region CanOpenExternalLinks

		/// <summary>
		/// Can open external links property
		/// </summary>
		public static readonly DependencyProperty CanOpenExternalLinksProperty =
			DependencyProperty.Register("CanOpenExternalLinks", typeof(bool), typeof(AsyncWebView), new PropertyMetadata(false));

		/// <summary>
		/// Gets or sets a value indicating whether links with either an external reference (opening a new window)
		/// or using a scheme that generally opens an external app (like mailto:) can be tapped by the user.
		/// </summary>
		public bool CanOpenExternalLinks
		{
			get { return (bool)GetValue(CanOpenExternalLinksProperty); }
			set { SetValue(CanOpenExternalLinksProperty, value); }
		}

		#endregion

		#region OpenLinksUsingExternalBrowser

		/// <summary>
		/// Can open links using external browser property
		/// </summary>
		// Using a DependencyProperty as the backing store for NavigateUsingExternalBrowser.  This enables animation, styling, binding, etc...
		// [Obsolete("Set NavigationMode to External instead.")]
		public static readonly DependencyProperty OpenLinksUsingExternalBrowserProperty =
			DependencyProperty.Register("OpenLinksUsingExternalBrowser", typeof(bool), typeof(AsyncWebView), new PropertyMetadata(false));

		/// <summary>
		/// Gets or sets a value indicating whether can open links using external browser
		/// </summary>
		// [Obsolete("Set NavigationMode to External instead.")]
		public bool OpenLinksUsingExternalBrowser
		{
			get { return (bool)GetValue(OpenLinksUsingExternalBrowserProperty); }
			set { SetValue(OpenLinksUsingExternalBrowserProperty, value); }
		}

		#endregion

		#region NavigationMode

		/// <summary>
		/// Navigation mode property
		/// </summary>
		public static readonly DependencyProperty NavigationModeProperty =
			DependencyProperty.Register("NavigationMode", typeof(NavigationMode), typeof(AsyncWebView), new PropertyMetadata(NavigationMode.Internal));

		/// <summary>
		/// Gets or sets the navigation mode (how links should be opened).
		/// </summary>
		public NavigationMode NavigationMode
		{
			get { return (NavigationMode)GetValue(NavigationModeProperty); }
			set { SetValue(NavigationModeProperty, value); }
		}

		#endregion

		#region IsClearingOnUnload

		/// <summary>
		/// Is clearing on unload property
		/// </summary>
		public static readonly DependencyProperty IsClearingOnUnloadProperty =
			DependencyProperty.Register("IsClearingOnUnload", typeof(bool), typeof(AsyncWebView), new PropertyMetadata(false));

		/// <summary>
		/// Gets or sets a value indicating whether the cache and cookies should be cleared when the control unloads.
		/// </summary>
		public bool IsClearingOnUnload
		{
			get { return (bool)GetValue(IsClearingOnUnloadProperty); }
			set { SetValue(IsClearingOnUnloadProperty, value); }
		}

		#endregion

		#region Source

		/// <summary>
		/// Source property
		/// </summary>
		public static readonly DependencyProperty SourceProperty =
			DependencyProperty.Register("Source", typeof(object), typeof(AsyncWebView), new PropertyMetadata(null, (s, e) => (s as AsyncWebView)?.OnSourceChanged(e.NewValue)));

		/// <summary>
		/// Gets or sets the source to view. This object can either be a Uri, to perform a normal navigation,
		/// a string to display HTML directly, or an HttpRequestMessage to perform a custom HTTP call.
		/// </summary>
		/// <remarks>If the initial navigation type never changes, you should instead use <seealso cref="NavigateToUri(Uri)"/>
		/// <seealso cref="NavigateToString(string)"/> or <seealso cref="NavigateToMessage(HttpRequestMessage)"/>.</remarks>
		public object Source
		{
			get { return (object)GetValue(SourceProperty); }
			set { SetValue(SourceProperty, value); }
		}

		#endregion

		#region SourceHtml

		/// <summary>
		/// Source Html property
		/// </summary>
		public static readonly DependencyProperty SourceHtmlProperty =
			DependencyProperty.Register("SourceHtml", typeof(string), typeof(AsyncWebView), new PropertyMetadata(null, (s, e) => (s as AsyncWebView)?.OnSourceHtmlChanged(e.NewValue as string)));

		/// <summary>
		/// Gets or sets the source HTML to view.
		/// </summary>
		public string SourceHtml
		{
			get { return (string)GetValue(SourceHtmlProperty); }
			set { SetValue(SourceHtmlProperty, value); }
		}

		#endregion

		#region SourceMessage

		/// <summary>
		/// Source message property
		/// </summary>
		public static readonly DependencyProperty SourceMessageProperty =
			DependencyProperty.Register("SourceMessage", typeof(Windows.Web.Http.HttpRequestMessage), typeof(AsyncWebView), new PropertyMetadata(null, (s, e) => (s as AsyncWebView)?.OnSourceMessageChanged(e.NewValue as Windows.Web.Http.HttpRequestMessage)));

		/// <summary>
		/// Gets or sets the source <seealso cref="HttpRequestMessage"/> to use to perform a
		/// navigation to an HTTP resource.
		/// </summary>
		public HttpRequestMessage SourceMessage
		{
			get { return (HttpRequestMessage)GetValue(SourceMessageProperty); }
			set { SetValue(SourceMessageProperty, value); }
		}

		#endregion

		#region SourceUri

		/// <summary>
		/// Gets or sets the source <seealso cref="Uri"/> to navigate to. This property
		/// does not always reflect the currently viewed url. It serves as a way for the
		/// view-model to force a navigation.
		/// </summary>
		public Uri SourceUri
		{
			get { return (Uri)GetValue(SourceUriProperty); }
			set { SetValue(SourceUriProperty, value); }
		}

		/// <summary>
		/// Source uri property
		/// </summary>
		public static readonly DependencyProperty SourceUriProperty =
			DependencyProperty.Register("SourceUri", typeof(Uri), typeof(AsyncWebView), new PropertyMetadata(null, (s, e) => (s as AsyncWebView)?.OnSourceUriChanged((Uri)e.NewValue)));

		#endregion

		#region GoBackCommand

		/// <summary>
		/// Gets the command that is exposed by the <seealso cref="AsyncWebView"/> to allow binding to
		/// other controls to perform a back navigation, when possible.
		/// </summary>
		public ICommand GoBackCommand
		{
			get { return (ICommand)GetValue(GoBackCommandProperty); }
			private set { SetValue(GoBackCommandProperty, value); }
		}

		/// <summary>
		/// Goback command property
		/// </summary>
		public static readonly DependencyProperty GoBackCommandProperty =
			DependencyProperty.Register("GoBackCommand", typeof(ICommand), typeof(AsyncWebView), new PropertyMetadata(null));

		#endregion

		#region GoForwardCommand

		/// <summary>
		/// Gets the command that is exposed by the <seealso cref="AsyncWebView"/> to allow binding to
		/// other controls to perform a forward navigation, when possible.
		/// </summary>
		public ICommand GoForwardCommand
		{
			get { return (ICommand)GetValue(GoForwardCommandProperty); }
			private set { SetValue(GoForwardCommandProperty, value); }
		}

		/// <summary>
		/// Goforward command property
		/// </summary>
		public static readonly DependencyProperty GoForwardCommandProperty =
			DependencyProperty.Register("GoForwardCommand", typeof(ICommand), typeof(AsyncWebView), new PropertyMetadata(null));

		#endregion

		#region RefreshCommand

		/// <summary>
		/// Gets the command that is exposed by the <seealso cref="AsyncWebView"/> to allow binding to
		/// other controls to perform a refresh of the currently visible page.
		/// </summary>
		public ICommand RefreshCommand
		{
			get { return (ICommand)GetValue(RefreshCommandProperty); }
			private set { SetValue(RefreshCommandProperty, value); }
		}

		/// <summary>
		/// Refresh command property
		/// </summary>
		public static readonly DependencyProperty RefreshCommandProperty =
			DependencyProperty.Register("RefreshCommand", typeof(ICommand), typeof(AsyncWebView), new PropertyMetadata(null));

		#endregion

		#region CompletionCommand

		/// <summary>
		/// Gets or sets the command to execute when a navigation to a Uri completes.
		/// The command parameter will be a <seealso cref="CompletionCommandArgs"/>.
		/// This property is useful for detecting when a flow completes or fails, and navigate
		/// away from the WebView page.
		/// </summary>
		public ICommand CompletionCommand
		{
			get { return (ICommand)GetValue(CompletionCommandProperty); }
			set { SetValue(CompletionCommandProperty, value); }
		}

		/// <summary>
		/// Completion command property
		/// </summary>
		public static readonly DependencyProperty CompletionCommandProperty =
			DependencyProperty.Register("CompletionCommand", typeof(ICommand), typeof(AsyncWebView), new PropertyMetadata(null));

		#endregion

		#region NavigationCommand

		/// <summary>
		/// Gets or sets the command to execute when the user taps a link within the current web page.
		/// The command parameter will contain the target <seealso cref="Uri"/>. If such a command is
		/// provided, the <seealso cref="AsyncWebView"/> cancels its own navigation.
		/// </summary>
		public ICommand NavigationCommand
		{
			get { return (ICommand)GetValue(NavigationCommandProperty); }
			set { SetValue(NavigationCommandProperty, value); }
		}

		/// <summary>
		/// Navigation command property
		/// </summary>
		public static readonly DependencyProperty NavigationCommandProperty =
			DependencyProperty.Register("NavigationCommand", typeof(ICommand), typeof(AsyncWebView), new PropertyMetadata(null));

		#endregion

		#region ScriptNotificationCommand

		/// <summary>
		/// Gets or sets the command to execute when the web page sends a notification.
		/// The parameter will contain an array of two strings. The first is the source url,
		/// and the second is the actual message.
		/// </summary>
		/// <remarks>This is currently only supported on Windows, when the web page calls window.external.notify().</remarks>
		public ICommand ScriptNotificationCommand
		{
			get { return (ICommand)GetValue(ScriptNotificationCommandProperty); }
			set { SetValue(ScriptNotificationCommandProperty, value); }
		}

		/// <summary>
		/// Script notification command property
		/// </summary>
		public static readonly DependencyProperty ScriptNotificationCommandProperty =
			DependencyProperty.Register("ScriptNotificationCommand", typeof(ICommand), typeof(AsyncWebView), new PropertyMetadata(null));

		#endregion
	}
}
#endif

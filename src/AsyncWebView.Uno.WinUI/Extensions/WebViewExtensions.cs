#if WINUI || __ANDROID__ || __IOS__ || __WASM__
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using _WebView2 = Microsoft.UI.Xaml.Controls.WebView2;

namespace AsyncWebView
{
	/// <summary>
	/// Encapsulates extensions for webview
	/// </summary>
	public static class WebViewExtensions
	{
		/// <summary>
		/// Register attached source string
		/// </summary>
		public static readonly DependencyProperty SourceStringProperty =
			DependencyProperty.RegisterAttached("SourceString", typeof(string), typeof(WebViewExtensions), new PropertyMetadata(string.Empty, OnSourceStringChanged));

		/// <summary>
		/// Gets source string
		/// </summary>
		/// <param name="obj">Webview</param>
		/// <returns>Source string</returns>
		public static string GetSourceString(_WebView2 obj)
		{
			return (string)obj.GetValue(SourceStringProperty);
		}

		/// <summary>
		/// Sets source string
		/// </summary>
		/// <param name="obj">Webview</param>
		/// <param name="value">Source string</param>
		public static void SetSourceString(_WebView2 obj, string value)
		{
			obj.SetValue(SourceStringProperty, value);
		}

		private static void OnSourceStringChanged(object d, DependencyPropertyChangedEventArgs e)
		{
			(d as _WebView2).NavigateToString(e.NewValue.ToString());
		}

#if WINUI
		/// <summary>
		/// Invokes scripts
		/// </summary>
		/// <param name="webView2">Web view</param>
		/// <param name="ct">Cancellation token</param>
		/// <param name="script">Script</param>
		/// <param name="arguments">Script agruments</param>
		/// <returns>void</returns>
		public static async Task<string> InvokeScriptAsync(this _WebView2 webView2, CancellationToken ct, string script)
		{
			return await webView2.ExecuteScriptAsync(script).AsTask(ct);
		}
#endif
	}
}
#endif

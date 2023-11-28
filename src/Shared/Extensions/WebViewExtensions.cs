#if __ANDROID__ || __IOS__ || __WASM__ || WINUI
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
#if WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using _WebView = Microsoft.UI.Xaml.Controls.WebView2;
#endif

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
		public static string GetSourceString(_WebView obj)
		{
			return (string)obj.GetValue(SourceStringProperty);
		}

		/// <summary>
		/// Sets source string
		/// </summary>
		/// <param name="obj">Webview</param>
		/// <param name="value">Source string</param>
		public static void SetSourceString(_WebView obj, string value)
		{
			obj.SetValue(SourceStringProperty, value);
		}

		private static void OnSourceStringChanged(object d, DependencyPropertyChangedEventArgs e)
		{
			(d as _WebView).NavigateToString(e.NewValue.ToString());
		}
	}
}
#endif

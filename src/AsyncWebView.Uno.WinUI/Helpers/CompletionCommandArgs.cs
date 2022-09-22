#if WINUI || __ANDROID__ || __IOS__ || __WASM__
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncWebView
{
	/// <summary>
	/// Encapsulates completion command arguments
	/// </summary>
	public class CompletionCommandArgs
	{
		private readonly AsyncWebView _asyncWebView;

		/// <summary>
		/// Initializes a new instance of the <see cref="CompletionCommandArgs"/> class.
		/// </summary>
		/// <param name="asyncWebView">Async Webview</param>
		/// <param name="isSuccess">If the navigation completed with success</param>
		/// <param name="uri">The destination Uri of the navigation, if any</param>
		public CompletionCommandArgs(AsyncWebView asyncWebView, bool isSuccess, Uri uri)
		{
			_asyncWebView = asyncWebView;

			IsSuccess = isSuccess;
			Uri = uri;
		}

		/// <summary>
		/// Gets a value indicating whether the navigation completed with success.
		/// </summary>
		public bool IsSuccess { get; }

		/// <summary>
		/// Gets the destination Uri of the navigation, if any.
		/// </summary>
		public Uri Uri { get; }

		/// <summary>
		/// Invokes the secified javascript in the context of the currently loaded web page.
		/// </summary>
		/// <param name="ct">A cancellation token.</param>
		/// <param name="script">The javascript to invoke.</param>
		/// <param name="arguments">Optional arguments for your javascript function.</param>
		/// <returns>The result of the invoked function, if defined, otherwise null.</returns>
		/// <remarks>In iOS and Android, the script must always be "eval".</remarks>
		public async Task<string> InvokeScript(CancellationToken ct, string script, params string[] arguments)
		{
			return await _asyncWebView.InvokeScriptAsync(ct, script, arguments);
		}
	}
}
#endif

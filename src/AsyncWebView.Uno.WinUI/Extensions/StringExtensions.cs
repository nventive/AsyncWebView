using System;

namespace AsyncWebView
{
	public static class StringExtensions
	{
		/// <summary>
		/// Gets if url is an action.
		/// </summary>
		/// <param name="absoluteUrl">Url to evaluate.</param>
		/// <returns>True if url is a supported action, false otherwise.</returns>
		public static bool IsUrlAction(this string absoluteUrl)
		{
#if __WASM__
			return absoluteUrl.Contains(AsyncWebViewConstants.LinkSchemes.PhoneDialer)
				|| absoluteUrl.Contains(AsyncWebViewConstants.LinkSchemes.Messages)
				|| absoluteUrl.Contains(AsyncWebViewConstants.LinkSchemes.Email);
#else
			return absoluteUrl.Contains(AsyncWebViewConstants.LinkSchemes.PhoneDialer, StringComparison.InvariantCulture)
				|| absoluteUrl.Contains(AsyncWebViewConstants.LinkSchemes.Messages, StringComparison.InvariantCulture)
				|| absoluteUrl.Contains(AsyncWebViewConstants.LinkSchemes.Email, StringComparison.InvariantCulture);
#endif
		}
	}
}

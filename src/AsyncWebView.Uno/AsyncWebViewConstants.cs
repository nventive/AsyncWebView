namespace Chinook.AsyncWebView
{
	public static class AsyncWebViewConstants
	{
		/// <summary>
		/// Schemes used in a HTML <a/> tag to perform an action.
		/// </summary>
		public static class LinkSchemes
		{
			/// <summary>
			/// Phone dialer scheme.
			/// </summary>
			public static readonly string PhoneDialer = "tel:";

			/// <summary>
			/// Messages scheme.
			/// </summary>
			public static readonly string Messages = "sms:";

			/// <summary>
			/// Send a email scheme.
			/// </summary>
			public static readonly string Email = "mailto:";
		}
	}
}

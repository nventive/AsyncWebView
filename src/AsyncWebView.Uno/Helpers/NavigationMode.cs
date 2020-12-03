namespace chinook.AsyncWebView
{
	/// <summary>
	/// Specifies how links inside the AsyncWebView should be opened.
	/// </summary>
	public enum NavigationMode
	{
		/// <summary>
		/// Open links in the WebView itself.
		/// </summary>
		Internal,

		/// <summary>
		/// Open links with the device's browser app.
		/// </summary>
		External,

		/// <summary>
		/// Open links with an application command.
		/// </summary>
		Application
	}
}

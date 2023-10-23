namespace AsyncWebView.Sample;

public sealed partial class MainPage : Page
{
	public MainPage()
	{
		this.InitializeComponent();
		Webview.Source = "<html><body><strong>Hello,</strong> World!</body></html>";
	}
}

namespace Groot.App;

public partial class App : Application
{
	public App(Storage.GrootStorage storage)
	{
		InitializeComponent();

		// Opening the file and applying the schema takes long enough to be felt if it happens on
		// the way into a screen, so it starts here, beside the splash screen, and is finished by
		// the time a run is saved. Nothing waits on it: Ready() is idempotent and whoever needs
		// the store awaits it themselves.
		_ = storage.Ready();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new MainPage()) { Title = "Groot" };
	}
}

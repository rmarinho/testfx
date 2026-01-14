namespace BlankIOS;

/// <summary>
/// Main app delegate that runs tests when the app is launched via 'dotnet run --device'.
/// Tests are executed using Microsoft.Testing.Platform and results are output to console.
/// </summary>
[Register ("AppDelegate")]
public class AppDelegate : UIApplicationDelegate {
	private const string TAG = "DeviceTests";

	public override bool FinishedLaunching (UIApplication application, NSDictionary? launchOptions)
	{
		Console.WriteLine($"[{TAG}] AppDelegate.FinishedLaunching - starting test execution");
		
		// Run tests on a background thread to avoid blocking the UI thread
		_ = Task.Run(RunTestsAsync);
		
		return true;
	}

	private async Task RunTestsAsync()
	{
		int exitCode = 1;
		
		try
		{
			// Get writable directory for test results
			var documentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			var testResultsDir = Path.Combine(documentsDir, "TestResults");
			
			Directory.CreateDirectory(testResultsDir);
			
			Console.WriteLine($"[{TAG}] Test results directory: {testResultsDir}");

			// Configure test arguments
			// --no-ansi and --no-progress are required on iOS as Console.BufferWidth throws PlatformNotSupportedException
			var args = new[]
			{
				"--results-directory", testResultsDir,
				"--report-trx",
				"--no-ansi",
				"--no-progress"
			};

			Console.WriteLine($"[{TAG}] Starting test execution...");
			
			// Run the tests via the generated entry point
			exitCode = await MicrosoftTestingPlatformEntryPoint.Main(args);
			
			Console.WriteLine($"[{TAG}] Tests completed with exit code: {exitCode}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[{TAG}] Test execution error: {ex}");
			exitCode = 1;
		}
		finally
		{
			Console.WriteLine($"[{TAG}] Terminating app with exit code: {exitCode}");
			
			// Terminate the application
			// Note: On iOS, we can't call exit() directly in a production app,
			// but for test apps this is acceptable to signal completion
			await Task.Delay(500); // Give time for output to flush
			Environment.Exit(exitCode);
		}
	}

	public override UISceneConfiguration GetConfiguration (UIApplication application, UISceneSession connectingSceneSession, UISceneConnectionOptions options)
	{
		// Called when a new scene session is being created.
		// Use this method to select a configuration to create the new scene with.
		// "Default Configuration" is defined in the Info.plist's 'UISceneConfigurationName' key.
		return new UISceneConfiguration ("Default Configuration", connectingSceneSession.Role);
	}

	public override void DidDiscardSceneSessions (UIApplication application, NSSet<UISceneSession> sceneSessions)
	{
		// Called when the user discards a scene session.
		// If any sessions were discarded while the application was not running, this will be called shortly after 'FinishedLaunching'.
		// Use this method to release any resources that were specific to the discarded scenes, as they will not return.
	}
}

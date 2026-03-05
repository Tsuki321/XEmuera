using System;
using System.IO;
using System.Threading.Tasks;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Content;
using Android.Views;

namespace XEmuera.Droid
{
	[Activity(Label = "XEmuera", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, Exported = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize, ScreenOrientation = ScreenOrientation.Sensor)]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
	{
		public static Activity Instance;

		private static bool Init;

		private static int UIOptions;
		private static int EmueraUIOptions;

		private static bool CrashHandlersRegistered;
		private static readonly object CrashLogLock = new object();

		private static void WriteCrashLog(Exception ex)
		{
			try
			{
				string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{ex}\n\n";
				lock (CrashLogLock)
				{
					// Try user-visible location first (external storage root /XEmuera/)
					try
					{
#pragma warning disable CS0618
						string extRoot = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
#pragma warning restore CS0618
						string publicDir = Path.Combine(extRoot, "XEmuera");
						Directory.CreateDirectory(publicDir);
						File.AppendAllText(Path.Combine(publicDir, "crash_log.txt"), entry);
						return;
					}
					catch (Exception ioEx)
					{
						Android.Util.Log.Warn("XEmuera", $"Failed to write crash log to public storage: {ioEx.Message}");
					}

					// Fallback to app-specific external files directory
					string logDir = Application.Context.GetExternalFilesDir(null)?.AbsolutePath
						?? System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
					File.AppendAllText(Path.Combine(logDir, "crash_log.txt"), entry);
				}
			}
			catch (Exception logEx)
			{
				Android.Util.Log.Error("XEmuera", $"Failed to write crash log: {logEx.Message}");
			}
		}

		private static void RegisterCrashHandlers()
		{
			if (CrashHandlersRegistered)
				return;
			CrashHandlersRegistered = true;

			AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
			{
				WriteCrashLog(e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString()));
			};

			AndroidEnvironment.UnhandledExceptionRaiser += (sender, e) =>
			{
				WriteCrashLog(e.Exception);
				e.Handled = true;
			};

			TaskScheduler.UnobservedTaskException += (sender, e) =>
			{
				WriteCrashLog(e.Exception);
				e.SetObserved();
			};
		}

		protected override void OnCreate(Bundle savedInstanceState)
		{
			RegisterCrashHandlers();

			base.OnCreate(savedInstanceState);

			Instance = this;

			Xamarin.Essentials.Platform.Init(this, savedInstanceState);
			global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

			if (!Init)
			{
				Init = true;

#pragma warning disable CS0618
				UIOptions = (int)Window.DecorView.SystemUiVisibility;
#pragma warning restore CS0618
				EmueraUIOptions = UIOptions
					| (int)SystemUiFlags.HideNavigation
					| (int)SystemUiFlags.LayoutHideNavigation
					| (int)SystemUiFlags.Fullscreen
					| (int)SystemUiFlags.LayoutFullscreen
					| (int)SystemUiFlags.LayoutStable
					| (int)SystemUiFlags.ImmersiveSticky;
			}

			//最后运行app
			LoadApplication(new App());
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
		{
			Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

			base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);

			switch (requestCode)
			{
				case GameUtils.ManageFilesPermissionsRequestCode:
					GameUtils.StorageAccess = resultCode == Result.Ok
						? Xamarin.Essentials.PermissionStatus.Granted : Xamarin.Essentials.PermissionStatus.Denied;
					break;

				case GameUtils.FileSelectorRequestCode:
					if (resultCode == Result.Ok && data?.Data != null)
					{
						string path = DroidDependencyService.GetPathFromDocumentTreeUri(data.Data);
						DroidDependencyService.FolderPickerCallback?.Invoke(path);
					}
					else
					{
						DroidDependencyService.FolderPickerCallback?.Invoke(null);
					}
					DroidDependencyService.FolderPickerCallback = null;
					break;

				default:
					break;
			}
		}

		public override void OnWindowFocusChanged(bool hasFocus)
		{
			base.OnWindowFocusChanged(hasFocus);

			if (!hasFocus)
				return;

			if (GameUtils.IsEmueraPage)
#pragma warning disable CS0618
				Window.DecorView.SystemUiVisibility = (StatusBarVisibility)EmueraUIOptions;
			else
				Window.DecorView.SystemUiVisibility = (StatusBarVisibility)UIOptions;
#pragma warning restore CS0618
		}
	}
}
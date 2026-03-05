using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Essentials;
using Xamarin.Forms;
using XEmuera.Droid;

[assembly: Dependency(typeof(DroidDependencyService))]
namespace XEmuera.Droid
{
	internal class DroidDependencyService : IPlatformService
	{
		internal static Action<string> FolderPickerCallback;

		public void CloseApplication()
		{
			MainActivity.Instance.FinishAffinity();
		}

		public void EmueraPageAppearing()
		{
			MainActivity.Instance.OnWindowFocusChanged(true);
		}

		public void EmueraPageDisappearing()
		{
			MainActivity.Instance.OnWindowFocusChanged(true);
		}

		public string GetStoragePath()
		{
#pragma warning disable CS0618
			return Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
#pragma warning restore CS0618
		}

		public void LockScreenOrientation()
		{
			ScreenOrientation orientation;

			switch (DeviceDisplay.MainDisplayInfo.Rotation)
			{
				case DisplayRotation.Rotation0:
					orientation = ScreenOrientation.Portrait;
					break;
				case DisplayRotation.Rotation90:
					orientation = ScreenOrientation.Landscape;
					break;
				case DisplayRotation.Rotation180:
					orientation = ScreenOrientation.ReversePortrait;
					break;
				case DisplayRotation.Rotation270:
					orientation = ScreenOrientation.ReverseLandscape;
					break;
				default:
					orientation = ScreenOrientation.Unspecified;
					break;
			}

			MainActivity.Instance.RequestedOrientation = orientation;
		}

		public bool NeedManageFilesPermissions()
		{
			return (int)Build.VERSION.SdkInt >= 30 && !Android.OS.Environment.IsExternalStorageManager;
		}

		public bool NeedRebootIfLanguageChanged()
		{
			return (int)Build.VERSION.SdkInt <= 32;
		}

		public bool NeedStoragePermissions()
		{
			return (int)Build.VERSION.SdkInt >= 24;
		}

		public void PickFolder(Action<string> callback)
		{
			FolderPickerCallback = callback;
			Intent intent = new Intent(Intent.ActionOpenDocumentTree);
			intent.AddFlags(ActivityFlags.GrantReadUriPermission);
			MainActivity.Instance.StartActivityForResult(intent, GameUtils.FileSelectorRequestCode);
		}

		public void RequestManageFilesPermissions()
		{
			Intent intent = new Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
			intent.SetData(Android.Net.Uri.Parse("package:" + MainActivity.Instance.PackageName));

			MainActivity.Instance.StartActivityForResult(intent, GameUtils.ManageFilesPermissionsRequestCode);
		}

		public void UnlockScreenOrientation()
		{
			MainActivity.Instance.RequestedOrientation = ScreenOrientation.Sensor;
		}

		// Audio playback using Android MediaPlayer
		private const int MaxConcurrentSounds = 10;
		private static readonly Android.Media.MediaPlayer[] soundPlayers = new Android.Media.MediaPlayer[MaxConcurrentSounds];
		private static Android.Media.MediaPlayer bgmPlayer;

		public void PlaySound(string filepath, int repeat = 1)
		{
			if (!System.IO.File.Exists(filepath)) return;
			try
			{
				int i;
				for (i = 0; i < soundPlayers.Length; i++)
				{
					if (soundPlayers[i] == null || !soundPlayers[i].IsPlaying)
						break;
				}
				if (i >= soundPlayers.Length) i = 0;
				soundPlayers[i]?.Release();
				soundPlayers[i] = new Android.Media.MediaPlayer();
				soundPlayers[i].SetDataSource(filepath);
				// Android MediaPlayer supports looping (infinite repeat) but not play-N-times natively
				soundPlayers[i].Looping = (repeat < 0);
				soundPlayers[i].Prepare();
				soundPlayers[i].Start();
			}
			catch { }
		}

		public void StopSound()
		{
			for (int i = 0; i < soundPlayers.Length; i++)
			{
				try
				{
					if (soundPlayers[i] != null && soundPlayers[i].IsPlaying)
					{
						soundPlayers[i].Stop();
						soundPlayers[i].Release();
						soundPlayers[i] = null;
					}
				}
				catch { }
			}
		}

		public void PlayBgm(string filepath)
		{
			if (!System.IO.File.Exists(filepath)) return;
			try
			{
				bgmPlayer?.Stop();
				bgmPlayer?.Release();
				bgmPlayer = new Android.Media.MediaPlayer();
				bgmPlayer.SetDataSource(filepath);
				bgmPlayer.Looping = true;
				bgmPlayer.Prepare();
				bgmPlayer.Start();
			}
			catch { }
		}

		public void StopBgm()
		{
			try
			{
				bgmPlayer?.Stop();
				bgmPlayer?.Release();
				bgmPlayer = null;
			}
			catch { }
		}

		public void SetBgmVolume(int volume)
		{
			try
			{
				float vol = System.Math.Max(0f, System.Math.Min(100f, volume)) / 100f;
				bgmPlayer?.SetVolume(vol, vol);
			}
			catch { }
		}

		public void SetSoundVolume(int volume)
		{
			float vol = System.Math.Max(0f, System.Math.Min(100f, volume)) / 100f;
			for (int i = 0; i < soundPlayers.Length; i++)
			{
				try { soundPlayers[i]?.SetVolume(vol, vol); }
				catch { }
			}
		}

		internal static string GetPathFromDocumentTreeUri(Android.Net.Uri uri)
		{
			if (uri == null) return null;

			if ("file".Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase))
				return uri.Path;

			string docId = Android.Provider.DocumentsContract.GetTreeDocumentId(uri);
			if (docId == null) return null;

			int colonIndex = docId.IndexOf(':');
			if (colonIndex < 0) return null;

			string volumeId = docId.Substring(0, colonIndex);
			string relativePath = docId.Substring(colonIndex + 1);

			if (volumeId.Equals("primary", StringComparison.OrdinalIgnoreCase))
			{
#pragma warning disable CS0618
				string basePath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
#pragma warning restore CS0618
				return string.IsNullOrEmpty(relativePath)
					? basePath
					: basePath + "/" + relativePath;
			}

			// External SD card / USB OTG
			return "/storage/" + volumeId + (string.IsNullOrEmpty(relativePath) ? "" : "/" + relativePath);
		}
	}
}
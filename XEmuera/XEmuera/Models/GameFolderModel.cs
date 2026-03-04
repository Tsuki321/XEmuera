using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace XEmuera.Models
{
	public class GameFolderModel
	{
		public const string PrefKeyGameFolders = nameof(PrefKeyGameFolders);

		private const string Separator = "_|_";

		/// <summary>
		/// The default built-in folder (kept for backward compatibility).
		/// </summary>
		public static GameFolderModel Instance { get; private set; }

		/// <summary>
		/// All folders to scan for games (default + user-added).
		/// </summary>
		public static readonly List<GameFolderModel> AllFolders = new List<GameFolderModel>();

		public string Name { get; set; }
		public string Path { get; set; }

		public static void Load()
		{
			string defaultPath = GameUtils.PlatformService.GetStoragePath() + System.IO.Path.DirectorySeparatorChar + "emuera";

			Instance = new GameFolderModel()
			{
				Name = System.IO.Path.GetFileName(defaultPath),
				Path = defaultPath
			};

			AllFolders.Clear();
			AllFolders.Add(Instance);

			string savedPaths = GameUtils.GetPreferences(PrefKeyGameFolders, null);
			if (!string.IsNullOrEmpty(savedPaths))
			{
				foreach (var path in savedPaths.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries))
				{
					if (!AllFolders.Any(f => f.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
					{
						AllFolders.Add(new GameFolderModel
						{
							Name = System.IO.Path.GetFileName(path),
							Path = path
						});
					}
				}
			}
		}

		public static void AddFolder(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return;

			if (AllFolders.Any(f => f.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
				return;

			AllFolders.Add(new GameFolderModel
			{
				Name = System.IO.Path.GetFileName(path),
				Path = path
			});

			SaveFolders();
		}

		private static void SaveFolders()
		{
			// Save all folders except the default one
			var customPaths = AllFolders.Skip(1).Select(f => f.Path);
			GameUtils.SetPreferences(PrefKeyGameFolders, string.Join(Separator, customPaths));
		}
	}
}

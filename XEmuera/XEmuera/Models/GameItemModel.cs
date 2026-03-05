using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using XEmuera.Resources;

namespace XEmuera.Models
{
	/// <summary>
	/// 游戏目录下的游戏项目
	/// </summary>
	public class GameItemModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public const string PrefKeyFavoriteItem = nameof(PrefKeyFavoriteItem);
		public const string PrefKeyRecentGames = nameof(PrefKeyRecentGames);

		private const string separator = "_|_";
		private const int MaxRecentGames = 10;

		public static readonly ObservableCollection<GameItemModel> AllModels = new ObservableCollection<GameItemModel>();

		public string Name { get; private set; }
		public string Path { get; private set; }

		public bool Favorite
		{
			get { return _favorite; }
			set
			{
				if (_favorite == value)
					return;
				_favorite = value;
				Sort();
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Favorite)));
			}
		}
		private bool _favorite;

		public bool IsRecent { get; private set; }

		public bool HasError { get; private set; }

		public string Error { get; private set; }

		private void Sort()
		{
			var list = AllModels.ToList();
			list.Sort(GameItemSorter);

			AllModels.Clear();

			foreach (var item in list)
			{
				AllModels.Add(item);
			}

			SaveFavorite();
		}

		public static void Load()
		{
			AllModels.Clear();

			var recentPaths = LoadRecentPaths();
			var favoritePaths = LoadFavoritePaths();

			GameItemModel gameItem;
			char directorySeparatorChar = System.IO.Path.DirectorySeparatorChar;

			foreach (var folder in GameFolderModel.AllFolders)
			{
				string mainPath = folder.Path;

				if (!Directory.Exists(mainPath))
					continue;

				// Check if the folder itself is a game (contains ERB/ directly)
				if (Directory.Exists(mainPath + directorySeparatorChar + "ERB")
					&& !AllModels.Any(m => m.Path.Equals(mainPath, StringComparison.OrdinalIgnoreCase)))
				{
					gameItem = new GameItemModel
					{
						Name = System.IO.Path.GetFileName(mainPath),
						Path = mainPath,
					};

					if (!Directory.Exists(mainPath + directorySeparatorChar + "CSV"))
					{
						gameItem.HasError = true;
						gameItem.Error = "(" + StringsText.CSVFolderNotExists + ")";
					}

					AllModels.Add(gameItem);
				}

				var gameItemPaths = Directory.GetDirectories(mainPath);

				foreach (var itemPath in gameItemPaths)
				{
					if (!Directory.Exists(itemPath + directorySeparatorChar + "ERB"))
						continue;

					// Skip duplicates (in case the same path appears in multiple folders)
					if (AllModels.Any(m => m.Path.Equals(itemPath, StringComparison.OrdinalIgnoreCase)))
						continue;

					gameItem = new GameItemModel
					{
						Name = System.IO.Path.GetFileName(itemPath),
						Path = itemPath,
					};

					if (!Directory.Exists(itemPath + directorySeparatorChar + "CSV"))
					{
						gameItem.HasError = true;
						gameItem.Error = "(" + StringsText.CSVFolderNotExists + ")";
					}

					AllModels.Add(gameItem);
				}
			}

			// Mark favorites
			foreach (var itemPath in favoritePaths)
			{
				var item = AllModels.FirstOrDefault(m => m.Path.Equals(itemPath, StringComparison.OrdinalIgnoreCase));
				if (item != null)
					item._favorite = true;
			}

			// Mark recent games (also include recently-played entries that may not be in current folders)
			foreach (var itemPath in recentPaths)
			{
				var item = AllModels.FirstOrDefault(m => m.Path.Equals(itemPath, StringComparison.OrdinalIgnoreCase));
				if (item != null)
				{
					item.IsRecent = true;
				}
				else if (Directory.Exists(itemPath + directorySeparatorChar + "ERB"))
				{
					// Game exists but not under a scanned folder – add it as a recent entry
					gameItem = new GameItemModel
					{
						Name = System.IO.Path.GetFileName(itemPath),
						Path = itemPath,
						IsRecent = true,
					};

					if (!Directory.Exists(itemPath + directorySeparatorChar + "CSV"))
					{
						gameItem.HasError = true;
						gameItem.Error = "(" + StringsText.CSVFolderNotExists + ")";
					}

					AllModels.Add(gameItem);
				}
			}

			// Sort: recent first, then favorites, then alphabetical
			var sorted = AllModels.ToList();
			sorted.Sort(GameItemSorter);
			AllModels.Clear();
			foreach (var item in sorted)
				AllModels.Add(item);
		}

		private static List<string> LoadFavoritePaths()
		{
			var result = new List<string>();
			string favoritePaths = GameUtils.GetPreferences(PrefKeyFavoriteItem, null);
			if (!string.IsNullOrEmpty(favoritePaths))
				result.AddRange(favoritePaths.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries));
			return result;
		}

		private static List<string> LoadRecentPaths()
		{
			var result = new List<string>();
			string recentPaths = GameUtils.GetPreferences(PrefKeyRecentGames, null);
			if (!string.IsNullOrEmpty(recentPaths))
				result.AddRange(recentPaths.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries));
			return result;
		}

		public static void SaveRecentGame(string gamePath)
		{
			if (string.IsNullOrWhiteSpace(gamePath))
				return;

			var recent = LoadRecentPaths();

			// Remove if already present, then insert at front
			recent.RemoveAll(p => p.Equals(gamePath, StringComparison.OrdinalIgnoreCase));
			recent.Insert(0, gamePath);

			if (recent.Count > MaxRecentGames)
				recent = recent.Take(MaxRecentGames).ToList();

			GameUtils.SetPreferences(PrefKeyRecentGames, string.Join(separator, recent));
		}

		private static int GameItemSorter(GameItemModel a, GameItemModel b)
		{
			// Recent first
			int recentComp = b.IsRecent.CompareTo(a.IsRecent);
			if (recentComp != 0) return recentComp;

			// Then favorites
			int favComp = b.Favorite.CompareTo(a.Favorite);
			if (favComp != 0) return favComp;

			// Then alphabetical
			return string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);
		}

		public static void SaveFavorite()
		{
			if (AllModels.Count == 0)
				return;

			var list = AllModels.Where(item => item.Favorite).Select(item => item.Path);
			GameUtils.SetPreferences(PrefKeyFavoriteItem, string.Join(separator, list));
		}
	}
}

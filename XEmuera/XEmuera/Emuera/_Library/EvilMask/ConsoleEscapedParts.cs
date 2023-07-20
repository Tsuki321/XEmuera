using MinorShift.Emuera;
using MinorShift.Emuera.GameView;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvilMask.Emuera
{
	internal sealed class ConsoleEscapedParts
	{
		static readonly DataTable dt = new DataTable();
		static readonly Dictionary<Int64, AConsoleDisplayPart> parts = new Dictionary<long, AConsoleDisplayPart>();
		static bool getOnce = false;
		static int lastTop, lastBottom, lastGeneration;
		public static bool Changed { get; private set; }
		public static bool TestedInRange(int top, int bottom, int gen)
		{
			return getOnce && top == lastTop && bottom == lastBottom && gen == lastGeneration;
		}
		static ConsoleEscapedParts()
		{
			dt.Columns.Add("line", typeof(int));
			dt.Columns.Add("depth", typeof(int));
			dt.Columns.Add("top", typeof(int));
			dt.Columns.Add("bottom", typeof(int));
			dt.Columns.Add("id", typeof(Int64));
			dt.Columns.Add("div", typeof(sbyte));
		}
		public static void Clear()
		{
			getOnce = false;
			dt.Clear();
			parts.Clear();
			Changed = false;
		}
		public static void Add(AConsoleDisplayPart part, int line, int depth, int top, int bottom)
		{
			var id = Utils.TimePoint();
			var row = dt.NewRow();
			row[0] = line;
			row[1] = depth;
			row[2] = top;
			row[3] = bottom;
			row[4] = id;
			if (part is ConsoleDivPart div)
				row[5] = (!div.IsRelative ? 2 : 0) | 1;
			else
				row[5] = 0;
			dt.Rows.Add(row);
			parts.Add(id, part);
			Changed = true;
		}
		public static void Remove(int line)
		{
			foreach (var row in DataTableExtensions.AsEnumerable(dt).Where(r => (int)r[0] >= line).ToArray())
			{
				parts.Remove((Int64)row[4]);
				dt.Rows.Remove(row);
				Changed = true;
			}
		}
		public static void RemoveAt(int line)
		{
			foreach (var row in DataTableExtensions.AsEnumerable(dt).Where(r => (int)r[0] == line).ToArray())
			{
				parts.Remove((Int64)row[4]);
				dt.Rows.Remove(row);
				Changed = true;
			}
		}
		public static void GetPartsInRange(int top, int bottom, int gen, Dictionary<int, List<AConsoleDisplayPart>> rmap)
		{
			if (rmap == null) return;
			rmap.Clear();
			foreach (var row in DataTableExtensions.AsEnumerable(dt)
				.Where(r => ((sbyte)r[5] & 2) != 0 || ((int)r[2] <= bottom + 1 && (int)r[3] >= top && r[0] is int line
				&& ((sbyte)r[5] != 0 || top > line || line > bottom + 1))))
			{
				List<AConsoleDisplayPart> list = null;
				rmap.TryGetValue((int)row[1], out list);
				if (list == null)
				{
					list = new List<AConsoleDisplayPart>();
					rmap.Add((int)row[1], list);
				}
				list.Add(parts[(Int64)row[4]]);
			}
			getOnce = true;
			lastTop = top; lastBottom = bottom; lastGeneration = gen;
			Changed = false;
		}
	}
}

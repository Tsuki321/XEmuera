using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MinorShift.Emuera.Sub
{
	internal sealed class EraStreamReader : IDisposable
	{
		public EraStreamReader(bool useRename)
		{
			this.useRename = useRename;
		}

		string filepath;
		string filename;
        readonly bool useRename = false;
		int curNo = 0;
		int nextNo = 0;
		StreamReader reader;
		FileStream stream;

		public bool Open(string path)
		{
			return Open(path, Path.GetFileName(path));
		}

		public bool Open(string path, string name)
		{
			//そんなお行儀の悪いことはしていない
			//if (disposed)
			//    throw new ExeEE("破棄したオブジェクトを再利用しようとした");
			//if ((reader != null) || (stream != null) || (filepath != null))
			//    throw new ExeEE("使用中のオブジェクトを別用途に再利用しようとした");
			filepath = path;
			filename = name;
			nextNo = 0;
			curNo = 0;
			try
			{
				stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				Encoding encoding = DetectEncoding(stream);
				stream.Position = 0;
				reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true);
			}
			catch
			{
				this.Dispose();
				return false;
			}
			return true;
		}

		/// <summary>
		/// Auto-detect file encoding by examining byte content.
		/// Checks for BOM first, then validates whether the content is valid UTF-8
		/// with multi-byte sequences. Falls back to Config.Encode (Shift-JIS) otherwise.
		/// </summary>
		private static Encoding DetectEncoding(FileStream stream)
		{
			byte[] bom = new byte[4];
			int bomRead = stream.Read(bom, 0, 4);

			// Check for BOM markers
			if (bomRead >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
				return Encoding.UTF8;
			if (bomRead >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)
				return Encoding.Unicode;
			if (bomRead >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)
				return Encoding.BigEndianUnicode;

			// No BOM found — check if content is valid UTF-8 with multi-byte sequences
			stream.Position = 0;
			int bufSize = (int)Math.Min(8192, stream.Length);
			byte[] buffer = new byte[bufSize];
			int bytesRead = stream.Read(buffer, 0, bufSize);

			if (LooksLikeUtf8(buffer, bytesRead))
				return Encoding.UTF8;

			return Config.Encode;
		}

		/// <summary>
		/// Returns true if the byte sequence is valid UTF-8 and contains at least
		/// one multi-byte character (indicating it is not plain ASCII).
		/// </summary>
		private static bool LooksLikeUtf8(byte[] data, int length)
		{
			bool hasMultibyte = false;
			int i = 0;
			while (i < length)
			{
				byte b = data[i];
				int seqLen;

				if (b <= 0x7F)
				{
					seqLen = 1;
				}
				else if ((b & 0xE0) == 0xC0)
				{
					seqLen = 2;
					hasMultibyte = true;
				}
				else if ((b & 0xF0) == 0xE0)
				{
					seqLen = 3;
					hasMultibyte = true;
				}
				else if ((b & 0xF8) == 0xF0)
				{
					seqLen = 4;
					hasMultibyte = true;
				}
				else
				{
					return false; // Invalid UTF-8 leading byte
				}

				if (i + seqLen > length)
					break; // Truncated at buffer boundary — not an error

				for (int j = 1; j < seqLen; j++)
				{
					if ((data[i + j] & 0xC0) != 0x80)
						return false; // Invalid continuation byte
				}

				i += seqLen;
			}
			return hasMultibyte;
		}

		public string ReadLine()
		{
			nextNo++;
			curNo = nextNo;
			return reader.ReadLine();
		}

		/// <summary>
		/// 次の有効な行を読む。LexicalAnalyzer経由でConfigを参照するのでConfig完成までつかわないこと。
		/// </summary>
		public StringStream ReadEnabledLine(bool disabled = false)
		{
			string line;
			StringStream st;
			curNo = nextNo;
			while (true)
			{
				line = reader.ReadLine();
				curNo++;
				nextNo++;
				if (line == null)
					return null;
				if (line.Length == 0)
					continue;

				if (useRename && (line.IndexOf("[[") >= 0) && (line.IndexOf("]]") >= 0))
				{
					foreach (KeyValuePair<string, string> pair in ParserMediator.RenameDic)
						line = line.Replace(pair.Key, pair.Value);
				}
				st = new StringStream(line);
				LexicalAnalyzer.SkipWhiteSpace(st);
				if (st.EOS)
					continue;
				//[SKIPSTART]～[SKIPEND]中にここが誤爆するので無効化
				if (!disabled)
				{
					if (st.Current == '}')
						throw new CodeEE("予期しない行連結終端記号'}'が見つかりました", new ScriptPosition(filename, curNo));
					if (st.Current == '{')
					{
						if (line.Trim() != "{")
							throw new CodeEE("行連結始端記号'{'の行に'{'以外の文字を含めることはできません", new ScriptPosition(filename, curNo));
						break;
					}
				}
				return st;
			}
			//curNoはこの後加算しない(始端記号の行を行番号とする)
			StringBuilder b = new StringBuilder();
			while (true)
			{
				line = reader.ReadLine();
				nextNo++;
				if (line == null)
				{
					throw new CodeEE("行連結始端記号'{'が使われましたが終端記号'}'が見つかりません", new ScriptPosition(filename, curNo));
				}

				if (useRename && (line.IndexOf("[[") >= 0) && (line.IndexOf("]]") >= 0))
				{
					foreach (KeyValuePair<string, string> pair in ParserMediator.RenameDic)
						line = line.Replace(pair.Key, pair.Value);
				}
				string test = line.TrimStart();
				if (test.Length > 0)
				{
					if (test[0] == '}')
					{
						if (test.Trim() != "}")
							throw new CodeEE("行連結終端記号'}'の行に'}'以外の文字を含めることはできません", new ScriptPosition(filename, nextNo));
						break;
					}
                    //行連結文字なら1字でないとおかしい、というか、こうしないとFORMの数値変数処理が誤爆する。
                    //{
                    //A}
                    //みたいなどうしようもないコードは知ったこっちゃない
					if (test[0] == '{' && test.Length == 1)
						throw new CodeEE("予期しない行連結始端記号'{'が見つかりました", new ScriptPosition(filename, nextNo));
				}
				b.Append(line);
				b.Append(" ");
			}
			st = new StringStream(b.ToString());
			LexicalAnalyzer.SkipWhiteSpace(st);
			return st;
		}

		/// <summary>
		/// 直前に読んだ行の行番号
		/// </summary>
		public int LineNo
		{ get { return curNo; } }
		public string Filename
		{
			get
			{
				return filename;
			}
		}
		//public string Filepath
		//{
		//    get
		//    {
		//        return filepath;
		//    }
		//}

		public void Close() { this.Dispose(); }
		bool disposed = false;
		#region IDisposable メンバ

		public void Dispose()
		{
			if (disposed)
				return;
			if (reader != null)
				reader.Close();
			else if (stream != null)
				stream.Close();
			filepath = null;
			filename = null;
			reader = null;
			stream = null;
			disposed = true;
		}

		#endregion
	}
}

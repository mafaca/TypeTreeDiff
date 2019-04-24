using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TypeTreeDiff
{
	public sealed class Dump
	{
		private Dump()
		{
		}

		public static Dump Read(string filePath)
		{
			if (!File.Exists(filePath))
			{
				throw new Exception($"File '{filePath}' doesn't exist");
			}

			byte[] data = File.ReadAllBytes(filePath);
			using (MemoryStream stream = new MemoryStream(data))
			{
				return Read(stream);
			}
		}

		public static Dump Read(Stream stream)
		{
			Dump dump = new Dump();
			using (DumpReader reader = new DumpReader(stream))
			{
				System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
				watch.Start();
				dump.ReadInner(reader);
				watch.Stop();

			}
			return dump;
		}

		private void ReadInner(DumpReader reader)
		{
			Version = ReadVersion(reader);
			Type = ReadType(reader);

			List<TypeTreeDump> trees = new List<TypeTreeDump>();
			while (!ReadValidation(reader, trees))
			{
				TypeTreeDump tree = TypeTreeDump.Read(reader);
				trees.Add(tree);
			}
			TypeTrees = trees.ToArray();
		}

		private Version ReadVersion(DumpReader reader)
		{
			reader.FindValidateWord("version");
			reader.ValidateWord(":");

			string major = reader.FindReadLineWord();
			reader.ValidateWord(".");
			string minor = reader.ReadWord();
			reader.ValidateWord(".");
			string buildType = reader.ReadWord();
			string versionString = $"{major}.{minor}.{buildType}";

			Version version = new Version();
			version.Parse(versionString);
			return version;
		}

		private string ReadType(DumpReader reader)
		{
			reader.FindValidateLineWord("(");
			string type = reader.ReadWord();
			reader.FindValidateLineWord(")");
			reader.FindValidateEOL();
			return type;
		}

		private bool ReadValidation(DumpReader reader, IReadOnlyList<TypeTreeDump> trees)
		{
			reader.FindContent();

			bool validation = false;
			reader.StartPeeking();
			if (reader.ReadWord() == "//")
			{
				if (reader.FindReadLineWord().StartsWith("==", StringComparison.InvariantCulture))
				{
					validation = true;
				}
			}
			reader.FinishPeeking();

			if (validation)
			{
				reader.ValidateWord("//");
				reader.FindReadLineWord();
				reader.FindValidateEOL();
				reader.FindNextLine();

				reader.ValidateWord("//");
				reader.FindValidateLineWord("Successfully");
				reader.FindValidateLineWord("finished");
				reader.FindValidateLineWord(".");
				reader.FindValidateLineWord("Written");
				int written = reader.FindReadLineInt();
				reader.FindValidateLineWord("of");
				int count = reader.FindReadLineInt();
				reader.FindValidateEOL();

				if (trees.Count != count)
				{
					throw new Exception($"Class count mismatch. Read {trees.Count} expected {count}");
				}
				int validCount = trees.Count(t => t.IsValid);
				if (validCount != written)
				{
					throw new Exception($"Valid class count mismatch. Read {validCount} expected {written}");
				}

				return true;
			}
			else
			{
				return false;
			}
		}

		public Version Version { get; private set; }
		public string Type { get; private set; }
		public IReadOnlyList<TypeTreeDump> TypeTrees { get; private set; }
	}
}

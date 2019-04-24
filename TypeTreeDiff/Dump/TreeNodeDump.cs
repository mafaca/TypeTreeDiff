using System;
using System.Collections.Generic;
using System.Globalization;

namespace TypeTreeDiff
{
	public class TreeNodeDump
	{
		protected TreeNodeDump()
		{
		}

		public int GetNodeCount()
		{
			int count = 1;
			foreach (TreeNodeDump child in Children)
			{
				count += child.GetNodeCount();
			}
			return count;
		}

		public override string ToString()
		{
			if (Type == null)
			{
				return base.ToString();
			}
			else
			{
				return $"{Type} {Name}";
			}
		}

		protected string ReadParameter(DumpReader reader, string name)
		{
			reader.FindValidateLineWord(name);
			reader.ValidateWord("{");
			string value = reader.ReadWord();
			reader.ValidateWord("}");
			return value;
		}

		protected int ReadIntParameter(DumpReader reader, string name)
		{
			string value = ReadParameter(reader, name);
			if (int.TryParse(value, out int intValue))
			{
				return intValue;
			}
			else
			{
				throw new Exception($"Can't parse int value '{value}'");
			}
		}

		protected int ReadHexIntParameter(DumpReader reader, string name)
		{
			string value = ReadParameter(reader, name);
			if (int.TryParse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int intValue))
			{
				return intValue;
			}
			else
			{
				throw new Exception($"Can't parse int value '{value}'");
			}
		}

		protected bool ReadBoolParameter(DumpReader reader, string name)
		{
			return ReadIntParameter(reader, name) != 0;
		}

		protected void ReadTypeTreeNode(DumpReader reader, int indent)
		{
			Type = ReadType(reader);
			Name = ReadName(reader);

			reader.FindValidateWord("//");

			ByteSize = ReadHexIntParameter(reader, "ByteSize");
			reader.FindValidateLineWord(",");
			Index = ReadHexIntParameter(reader, "Index");
			reader.FindValidateLineWord(",");
			IsArray = ReadBoolParameter(reader, "IsArray");
			reader.FindValidateLineWord(",");
			MetaFlag = unchecked((uint)ReadHexIntParameter(reader, "MetaFlag"));
			reader.FindValidateEOL();
			reader.FindNextLine();

			int childIndent = indent + 1;
			List<TreeNodeDump> children = new List<TreeNodeDump>();
			while (reader.PeekIndend() == childIndent)
			{
				TreeNodeDump child = new TreeNodeDump();
				child.ReadTypeTreeNode(reader, childIndent);
				children.Add(child);
			}
			Children = children.ToArray();
		}

		private string ReadType(DumpReader reader)
		{
			string type = reader.FindReadLineWord();
			if (type == "unsigned")
			{
				string subType = reader.FindReadLineWord();
				return $"{type} {subType}";
			}
			else
			{
				return type;
			}
		}

		private string ReadName(DumpReader reader)
		{
			string name = reader.FindReadLineWord();
			while (reader.FindPeekLineWord() != "//")
			{
				name += " " + reader.FindReadLineWord();
			}
			return name;
		}

		public string Type { get; private set; }
		public string Name { get; private set; }
		public int ByteSize { get; private set; }
		public int Index { get; private set; }
		public bool IsArray { get; private set; }
		public uint MetaFlag { get; private set; }
		public bool IsAlign => (MetaFlag & 0x4000) != 0;
		public IReadOnlyList<TreeNodeDump> Children { get; private set; }
	}
}

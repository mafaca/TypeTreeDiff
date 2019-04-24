using System.Collections.Generic;

namespace TypeTreeDiff
{
	public sealed class TreeDump : TreeNodeDump
	{
		private TreeDump()
		{
		}

		internal static TreeDump Read(DumpReader reader)
		{
			TreeDump typeTree = new TreeDump();
			typeTree.ReadTypeTree(reader);
			return typeTree;
		}

		public override TreeNodeDump Optimize()
		{
			TreeDump tree = new TreeDump();
			Optimize(tree);
			tree.ClassID = ClassID;
			tree.ClassName = ClassName;
			tree.Inheritance = Inheritance;
			tree.IsValid = IsValid;
			tree.IsAbstract = IsAbstract;
			return tree;
		}

		public override string ToString()
		{
			if (ClassName == null)
			{
				return base.ToString();
			}
			else
			{
				string result = string.Empty;
				if (IsAbstract)
				{
					result += "abstract ";
				}
				result += $"{ClassName}({ClassID})";
				if (Inheritance.Count > 0)
				{
					result += $" : {Inheritance[0]}";
				}
				return result;
			}
		}

		private void ReadTypeTree(DumpReader reader)
		{
			ReadHeader(reader);
			ReadValid(reader);
			if (IsValid)
			{
				if (!ReadAbstract(reader))
				{
					ReadTypeTreeNode(reader, 0);
				}
			}
		}

		private void ReadHeader(DumpReader reader)
		{
			reader.ValidateWord("//");
			ClassID = ReadIntParameter(reader, "classID");
			reader.ValidateWord(":");
			ClassName = reader.FindReadLineWord();

			List<string> inheritance = new List<string>();
			while (reader.FindLineContent())
			{
				reader.ValidateWord("<-");
				string baseName = reader.FindReadLineWord();
				inheritance.Add(baseName);
			}
			Inheritance = inheritance.ToArray();
			reader.FindNextLine();
		}

		private void ReadValid(DumpReader reader)
		{
			IsValid = true;
			reader.StartPeeking();
			if (reader.ReadWord() == "//")
			{
				if (reader.FindReadLineWord() == "Can't")
				{
					IsValid = false;
				}
			}
			reader.FinishPeeking();

			if (!IsValid)
			{
				reader.ValidateWord("//");
				reader.FindValidateLineWord("Can't");
				reader.FindValidateLineWord("produce");
				reader.FindValidateLineWord(ClassName);
				reader.FindValidateEOL();
				reader.FindNextLine();
			}
		}

		private bool ReadAbstract(DumpReader reader)
		{
			int level = 0;
			while (reader.PeekWord() == "//")
			{
				string name = level == 0 ? ClassName : Inheritance[level - 1];
				reader.ValidateWord("//");
				reader.FindValidateWord(name);
				reader.FindValidateWord("is");
				reader.FindValidateWord("abstract");
				reader.FindValidateEOL();
				reader.FindNextLine();
				level++;
			}
			IsAbstract = level > 0;
			return level == Inheritance.Count + 1;
		}


		public int ClassID { get; private set; }
		public string ClassName { get; private set; }
		public IReadOnlyList<string> Inheritance { get; private set; }
		public bool IsValid { get; private set; }
		public bool IsAbstract { get; private set; }
	}
}

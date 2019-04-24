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

		public virtual TreeNodeDump Optimize()
		{
			TreeNodeDump node = new TreeNodeDump();
			Optimize(node);
			return node;
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

		protected void Optimize(TreeNodeDump dest)
		{
			string type = Type;
			dest.Type = ToOptimizedType(this);
			dest.Name = Name;
			dest.ByteSize = ByteSize;
			dest.Index = Index;
			dest.IsArray = IsArray;
			dest.MetaFlag = MetaFlag;

			IReadOnlyList<TreeNodeDump> children = GetOptimizedChildren();
			int childCount = children == null ? 0 : children.Count;
			TreeNodeDump[] newChildren = new TreeNodeDump[childCount];
			for (int i = 0; i < childCount; i++)
			{
				newChildren[i] = children[i].Optimize();
			}
			dest.Children = newChildren;
		}

		private static string ToOptimizedType(TreeNodeDump node)
		{
			string type = node.Type;
			IReadOnlyList<TreeNodeDump> children = node.Children;
			switch (type)
			{
				case "SInt8":
					return "sbyte";
				case "UInt8":
					return "byte";
				case "SInt16":
					return "short";
				case "UInt16":
					return "ushort";
				case "unsigned int":
					return "uint";
				case "SInt64":
					return "long";
				case "UInt64":
					return "ulong";

				case "vector":
					{
						if (children.Count != 1)
						{
							throw new Exception($"Vector has {children.Count} children but expected 1");
						}

						TreeNodeDump array = children[0];
						if (array.Children.Count != 2)
						{
							throw new Exception($"Vector's array node has {array.Children.Count} children but expected 2");
						}
						if (array.Type != "Array")
						{
							throw new Exception($"Vector's array node type {array.Name} doesn't match expected value 'Array'");
						}
						if (array.Name != "Array")
						{
							throw new Exception($"Vector's array node name {array.Name} doesn't match expected value 'Array'");
						}

						TreeNodeDump vectorSize = array.Children[0];
						if (vectorSize.Type != "int")
						{
							throw new Exception($"Vector's size node type {vectorSize.Type} doesn't match expected value 'Array'");
						}
						if (vectorSize.Name != "size")
						{
							throw new Exception($"Vector's size node name {vectorSize.Name} doesn't match expected value 'size'");
						}

						TreeNodeDump vectorValue = array.Children[1];
						if (vectorValue.Name != "data")
						{
							throw new Exception($"Vector's value node name {vectorValue.Name} doesn't match expected value 'data'");
						}

						return $"{ToOptimizedType(vectorValue)}[]";
					}

				case "map":
					{
						if (children.Count != 1)
						{
							throw new Exception($"Map has {children.Count} children but expected 1");
						}

						TreeNodeDump array = children[0];
						if (array.Children.Count != 2)
						{
							throw new Exception($"Map's array node has {array.Children.Count} children but expected 2");
						}
						if (array.Type != "Array")
						{
							throw new Exception($"Map's array node type {array.Name} doesn't match expected value 'Array'");
						}
						if (array.Name != "Array")
						{
							throw new Exception($"Map's array node name {array.Name} doesn't match expected value 'Array'");
						}

						TreeNodeDump mapSize = array.Children[0];
						if (mapSize.Type != "int")
						{
							throw new Exception($"Map's size node type {mapSize.Type} doesn't match expected value 'Array'");
						}
						if (mapSize.Name != "size")
						{
							throw new Exception($"Map's size node name {mapSize.Name} doesn't match expected value 'size'");
						}

						TreeNodeDump pair = array.Children[1];
						if (pair.Children.Count != 2)
						{
							throw new Exception($"Map's value node has {array.Children.Count} children but expected 2");
						}
						if (pair.Type != "pair")
						{
							throw new Exception($"Map's value node type {pair.Type} doesn't match expected value 'pair'");
						}
						if (pair.Name != "data")
						{
							throw new Exception($"Map's value node name {pair.Name} doesn't match expected value 'data'");
						}

						TreeNodeDump key = pair.Children[0];
						if (key.Name != "first")
						{
							throw new Exception($"Map's kvp-key node name {key.Name} doesn't match expected value 'first'");
						}
						TreeNodeDump value = pair.Children[1];
						if (value.Name != "second")
						{
							throw new Exception($"Map's kvp-value node name {value.Name} doesn't match expected value 'second'");
						}

						return $"Dictionary<{ToOptimizedType(key)}, {ToOptimizedType(value)}>";
					}

				case "set":
					{
						if (children.Count != 1)
						{
							throw new Exception($"Set has {children.Count} children but expected 1");
						}

						TreeNodeDump array = children[0];
						if (array.Children.Count != 2)
						{
							throw new Exception($"Set's array node has {array.Children.Count} children but expected 2");
						}
						if (array.Type != "Array")
						{
							throw new Exception($"Set's array node type {array.Name} doesn't match expected value 'Array'");
						}
						if (array.Name != "Array")
						{
							throw new Exception($"Set's array node name {array.Name} doesn't match expected value 'Array'");
						}

						TreeNodeDump setSize = array.Children[0];
						if (setSize.Type != "int")
						{
							throw new Exception($"Set's size node type {setSize.Type} doesn't match expected value 'int'");
						}
						if (setSize.Name != "size")
						{
							throw new Exception($"Set's size node name {setSize.Name} doesn't match expected value 'size'");
						}

						TreeNodeDump setValue = array.Children[1];
						if (setValue.Name != "data")
						{
							throw new Exception($"Set's value node name {setValue.Name} doesn't match expected value 'data'");
						}

						return $"HashSet<{ToOptimizedType(setValue)}>";
					}

				case "string":
					{
						if (children.Count != 1)
						{
							throw new Exception($"String has {children.Count} children but expected 1");
						}

						TreeNodeDump array = children[0];
						if (array.Children.Count != 2)
						{
							throw new Exception($"String's array node has {array.Children.Count} children but expected 2");
						}
						if (array.Type != "Array")
						{
							throw new Exception($"String's array node type {array.Name} doesn't match expected value 'Array'");
						}
						if (array.Name != "Array")
						{
							throw new Exception($"String's array node name {array.Name} doesn't match expected value 'Array'");
						}

						TreeNodeDump stringSize = array.Children[0];
						if (stringSize.Type != "int")
						{
							throw new Exception($"String's size node type {stringSize.Type} doesn't match expected value 'int'");
						}
						if (stringSize.Name != "size")
						{
							throw new Exception($"String's size node name {stringSize.Name} doesn't match expected value 'size'");
						}

						TreeNodeDump stringValue = array.Children[1];
						if (stringValue.Type != "char")
						{
							throw new Exception($"String's value node type {stringValue.Type} doesn't match expected value 'char'");
						}
						if (stringValue.Name != "data")
						{
							throw new Exception($"String's value node name {stringValue.Name} doesn't match expected value 'data'");
						}

						return "string";
					}

				case "pair":
					{
						if (children.Count != 2)
						{
							throw new Exception($"Pair has {children.Count} children but expected 2");
						}

						TreeNodeDump first = children[0];
						if (first.Name != "first")
						{
							throw new Exception($"Pair's first child name {children[0].Name} doesn't match expected value 'first'");
						}
						TreeNodeDump second = children[1];
						if (second.Name != "second")
						{
							throw new Exception($"Pair's first child name {children[0].Name} doesn't match expected value 'second'");
						}
						return $"KeyValuePair<{ToOptimizedType(first)}, {ToOptimizedType(second)}>";
					}

				case "TypelessData":
					{
						if (children.Count != 2)
						{
							throw new Exception($"Typeless data has {children.Count} children but expected 2");
						}

						TreeNodeDump dataSize = children[0];
						if (dataSize.Type != "int")
						{
							throw new Exception($"Typeless data's size node type {dataSize.Type} doesn't match expected value 'int'");
						}
						if (dataSize.Name != "size")
						{
							throw new Exception($"Typeless data's size node name {dataSize.Name} doesn't match expected value 'size'");
						}

						TreeNodeDump dataValue = children[1];
						if (dataValue.Type != "UInt8")
						{
							throw new Exception($"String's value node type {dataValue.Type} doesn't match expected value 'UInt8'");
						}
						if (dataValue.Name != "data")
						{
							throw new Exception($"String's value node name {dataValue.Name} doesn't match expected value 'data'");
						}
						return $"byte[]";
					}

				default:
					return type;
			}
		}

		private static bool IsPrimitiveType(string type)
		{
			switch (type)
			{
				case "bool":
				case "char":
				case "SInt8":
				case "UInt8":
				case "SInt16":
				case "UInt16":
				case "int":
				case "unsigned int":
				case "SInt64":
				case "UInt64":
				case "float":
				case "double":
				case "string":
					return true;

				case "Vector2f":
				case "Vector3f":
				case "Vector4f":
				case "Rectf":
				case "Matrix4x4f":
				case "GUID":
				case "Hash128":
				case "BitField":
					return true;

				default:
					return false;
			}
		}

		private IReadOnlyList<TreeNodeDump> GetOptimizedChildren()
		{
			switch (Type)
			{
				case "vector":
					return IsPrimitiveType(Children[0].Children[1].Type) ? new TreeNodeDump[0] : new TreeNodeDump[] { Children[0].Children[1] };
				case "map":
					return new TreeNodeDump[] { Children[0].Children[1] };
				case "set":
					return IsPrimitiveType(Children[0].Children[1].Type) ? new TreeNodeDump[0] : new TreeNodeDump[] { Children[0].Children[1] };
				case "string":
					return new TreeNodeDump[0];
				case "TypelessData":
					return new TreeNodeDump[0];

				case "Vector2f":
				case "Vector3f":
				case "Vector4f":
				case "Rectf":
				case "Quaternionf":
				case "Matrix4x4f":
				case "GUID":
				case "Hash128":
				case "BitField":
					return new TreeNodeDump[0];

				case null:
					return null;
			}

			if (Type.StartsWith("PPtr<"))
			{
				return new TreeNodeDump[0];
			}
			return Children;
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

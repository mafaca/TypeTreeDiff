using System;

namespace TypeTreeDiff
{
	public sealed class TreeDiff
	{
		public TreeDiff(TypeTreeDump tree, DiffStatus status)
		{
			if (tree == null)
			{
				throw new ArgumentNullException(nameof(tree));
			}

			ClassID = tree.ClassID;
			LeftClassName = RightClassName = tree.ClassName ?? throw new ArgumentNullException(nameof(tree.ClassName));
			LeftBaseName = RightBaseName = tree.Inheritance.Count == 0 ? string.Empty : tree.Inheritance[0] ?? throw new ArgumentNullException("BaseName");
			Node = tree.IsAbstract || !tree.IsValid ? null : new TreeNodeDiff(tree, status);
			Status = status;
		}

		public TreeDiff(TypeTreeDump left, TypeTreeDump right)
		{
			if (left == null)
			{
				throw new ArgumentNullException(nameof(left));
			}
			if (right == null)
			{
				throw new ArgumentNullException(nameof(right));
			}
			if (left.ClassID != right.ClassID)
			{
				throw new ArgumentException($"Left class ID {left.ClassID} doesn't match to right {right.ClassID}");
			}

			ClassID = left.ClassID;
			LeftClassName = left.ClassName ?? throw new ArgumentNullException(nameof(left.ClassName));
			RightClassName = right.ClassName ?? throw new ArgumentNullException(nameof(right.ClassName));
			LeftBaseName = left.Inheritance.Count == 0 ? string.Empty : left.Inheritance[0] ?? throw new ArgumentNullException("BaseName");
			RightBaseName = right.Inheritance.Count == 0 ? string.Empty : right.Inheritance[0] ?? throw new ArgumentNullException("BaseName");
			if (left.IsValid && right.IsValid)
			{
				if (left.IsAbstract || right.IsAbstract)
				{
					Status = (LeftClassName == RightClassName && LeftBaseName == RightBaseName) ? DiffStatus.Unchanged : DiffStatus.Changed;
				}
				else
				{
					Node = new TreeNodeDiff(left, right);
					Status = (LeftClassName == RightClassName && LeftBaseName == RightBaseName) ? Status = Node.Status : DiffStatus.Changed;
				}
			}
			else
			{
				Status = DiffStatus.Invalid;
			}
		}

		public int ClassID { get; }
		public string LeftClassName { get; }
		public string RightClassName { get; }
		public string LeftBaseName { get; }
		public string RightBaseName { get; }
		public TreeNodeDiff Node { get; }
		public DiffStatus Status { get; }
	}
}

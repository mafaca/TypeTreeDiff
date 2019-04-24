using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeTreeDiff
{
	public sealed class TreeNodeDiff
	{
		public TreeNodeDiff(TreeNodeDump node, DiffStatus status)
		{
			if (node == null)
			{
				throw new ArgumentNullException(nameof(node));
			}
			if (status != DiffStatus.Added && status != DiffStatus.Deleted)
			{
				throw new ArgumentException(nameof(status));
			}

			Name = node.Name ?? throw new ArgumentNullException(nameof(node.Name));

			string type = node.Type ?? throw new ArgumentNullException(nameof(node.Type));
			LeftType = status == DiffStatus.Added ? string.Empty : type;
			RightType = status == DiffStatus.Added ? type : string.Empty;

			LeftAlign = status == DiffStatus.Added ? false : node.IsAlign;
			RightAlign = status == DiffStatus.Added ? node.IsAlign : false;

			int childNodeCount = node.GetNodeCount() - 1;
			TreeNodeDiff emptyElement = childNodeCount == 0 ? null : new TreeNodeDiff(status);
			TreeNodeDiff[] children = node.Children.Select(t => new TreeNodeDiff(t, status)).ToArray();
			TreeNodeDiff[] emptyChildren = Enumerable.Repeat(emptyElement, childNodeCount).ToArray();
			LeftChildren = status == DiffStatus.Added ? emptyChildren : children;
			RightChildren = status == DiffStatus.Added ? children : emptyChildren;

			Status = status;
		}

		public TreeNodeDiff(TreeDump left, TreeDump right) :
			this(left, right, true)
		{
		}

		public TreeNodeDiff(TreeNodeDump left, TreeNodeDump right):
			this(left, right, false)
		{
		}

		private TreeNodeDiff(DiffStatus status)
		{
			Name = string.Empty;
			LeftType = RightType = string.Empty;
			LeftChildren = RightChildren = new TreeNodeDiff[0];
			Status = status;
		}

		private TreeNodeDiff(TreeNodeDump left, TreeNodeDump right, bool forceMerge)
		{
			if (left == null)
			{
				throw new ArgumentNullException(nameof(left));
			}
			if (right == null)
			{
				throw new ArgumentNullException(nameof(right));
			}
			if (left.Name != right.Name)
			{
				throw new ArgumentException("Left and right names aren't equal");
			}

			Name = left.Name ?? throw new ArgumentNullException(nameof(left.Name));
			LeftType = left.Type ?? throw new ArgumentNullException(nameof(left.Type));
			RightType = right.Type ?? throw new ArgumentNullException(nameof(left.Type));
			LeftAlign = left.IsAlign;
			RightAlign = right.IsAlign;
			Status = LeftAlign == RightAlign ? DiffStatus.Unchanged : DiffStatus.Changed;

			if (LeftType == RightType || forceMerge)
			{
				TreeNodeDiff[] children = CreateChildren(left, right);
				LeftChildren = RightChildren = children;
				Status = children.All(t => t.Status == DiffStatus.Unchanged) ? Status : DiffStatus.Changed;
			}
			else
			{
				int leftNodeCount = left.GetNodeCount();
				int rightNodeCount = right.GetNodeCount();
				int extraLeft = Math.Max(rightNodeCount - leftNodeCount, 0);
				int extraRight = Math.Max(leftNodeCount - rightNodeCount, 0);
				TreeNodeDiff extraElement = leftNodeCount == rightNodeCount ? null : new TreeNodeDiff(DiffStatus.Changed);
				IEnumerable<TreeNodeDiff> extraLeftChildren = Enumerable.Repeat(extraElement, extraLeft);
				IEnumerable<TreeNodeDiff> extraRightChildren = Enumerable.Repeat(extraElement, extraRight);

				LeftChildren = left.Children.Select(t => new TreeNodeDiff(t, true)).Concat(extraLeftChildren).ToArray();
				RightChildren = right.Children.Select(t => new TreeNodeDiff(t, false)).Concat(extraRightChildren).ToArray();
				Status = DiffStatus.Changed;
			}
		}

		private TreeNodeDiff(TreeNodeDump node, bool left)
		{
			if (node == null)
			{
				throw new ArgumentNullException(nameof(node));
			}

			Name = node.Name ?? throw new ArgumentNullException(nameof(node.Name));

			string type = node.Type ?? throw new ArgumentNullException(nameof(node.Type));
			LeftType = left ? string.Empty : type;
			RightType = left ? type : string.Empty;

			LeftAlign = left ? false : node.IsAlign;
			RightAlign = left ? node.IsAlign : false;

			TreeNodeDiff[] children = node.Children.Select(t => new TreeNodeDiff(t, left)).ToArray();
			LeftChildren = left ? children : new TreeNodeDiff[0];
			RightChildren = left ? new TreeNodeDiff[0] : children;

			Status = DiffStatus.Changed;
		}

		private static TreeNodeDiff[] CreateChildren(TreeNodeDump left, TreeNodeDump right)
		{
			List<TreeNodeDiff> children = new List<TreeNodeDiff>();
			for (int li = 0, ri = 0; li < left.Children.Count || ri < right.Children.Count;)
			{
				if (li == left.Children.Count)
				{
					TreeNodeDiff node = new TreeNodeDiff(right.Children[ri], DiffStatus.Added);
					children.Add(node);
					ri++;
					continue;
				}
				if (ri == right.Children.Count)
				{
					TreeNodeDiff node = new TreeNodeDiff(left.Children[li], DiffStatus.Deleted);
					children.Add(node);
					li++;
					continue;
				}

				TreeNodeDump leftChild = left.Children[li];
				TreeNodeDump rightChild = right.Children[ri];
				if (IsNodeSame(leftChild, rightChild))
				{
					TreeNodeDiff node = new TreeNodeDiff(leftChild, rightChild);
					children.Add(node);
					li++;
					ri++;
					continue;
				}

				if (IsNodePresent(leftChild, right, ri))
				{
					do
					{
						TreeNodeDiff node = new TreeNodeDiff(rightChild, DiffStatus.Added);
						children.Add(node);
						ri++;

						rightChild = right.Children[ri];
					} while (!IsNodeSame(leftChild, rightChild));
				}
				else
				{
					TreeNodeDiff node = new TreeNodeDiff(leftChild, DiffStatus.Deleted);
					children.Add(node);
					li++;
					continue;
				}
			}
			return children.ToArray();
		}

		private static bool IsNodeSame(TreeNodeDump left, TreeNodeDump right)
		{
			return left.Name == right.Name;
		}

		private static bool IsNodePresent(TreeNodeDump node, TreeNodeDump right, int startIndex)
		{
			for (int ri = startIndex + 1; ri < right.Children.Count; ri++)
			{
				TreeNodeDump rightChild = right.Children[ri];
				if (IsNodeSame(node, rightChild))
				{
					return true;
				}
			}
			return false;
		}

		public int GetNodeCount(bool left)
		{
			int count = 1;
			IReadOnlyList<TreeNodeDiff> children = left ? LeftChildren : RightChildren;
			foreach (TreeNodeDiff child in children)
			{
				count += child.GetNodeCount(left);
			}
			return count;
		}

		public override string ToString()
		{
			return LeftType == RightType ? $"{RightType} {Name}" : $"{RightType}({LeftType}) {Name}";
		}

		public string Name { get; }
		public string LeftType { get; }
		public string RightType { get; }
		public bool LeftAlign { get; }
		public bool RightAlign { get; }
		public IReadOnlyList<TreeNodeDiff> LeftChildren { get; }
		public IReadOnlyList<TreeNodeDiff> RightChildren { get; }
		public DiffStatus Status { get; }
	}
}

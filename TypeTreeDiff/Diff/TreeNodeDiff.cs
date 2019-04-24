using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeTreeDiff
{
	public sealed class TreeNodeDiff
	{
		public TreeNodeDiff(TypeTreeNodeDump node, DiffStatus status)
		{
			if (node == null)
			{
				throw new ArgumentNullException(nameof(node));
			}

			Type = node.Type ?? throw new ArgumentNullException(nameof(node.Type));
			Name = node.Name ?? throw new ArgumentNullException(nameof(node.Name));
			Children = node.Children.Select(t => new TreeNodeDiff(t, status)).ToArray();
			Status = status;
		}

		public TreeNodeDiff(TypeTreeDump left, TypeTreeDump right)
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

			Type = left.Type ?? throw new ArgumentNullException(nameof(left.Type));
			Name = left.Name ?? throw new ArgumentNullException(nameof(left.Name));
			Children = CreateChildren(left, right);
			Status = Children.All(t => t.Status == DiffStatus.Unchanged) ? DiffStatus.Unchanged : DiffStatus.Changed;
		}

		public TreeNodeDiff(TypeTreeNodeDump left, TypeTreeNodeDump right)
		{
			if (left == null)
			{
				throw new ArgumentNullException(nameof(left));
			}
			if (right == null)
			{
				throw new ArgumentNullException(nameof(right));
			}
			if (left.Type != right.Type)
			{
				throw new ArgumentException("Left and right types aren't equal");
			}
			if (left.Name != right.Name)
			{
				throw new ArgumentException("Left and right names aren't equal");
			}

			Type = left.Type ?? throw new ArgumentNullException(nameof(left.Type));
			Name = left.Name ?? throw new ArgumentNullException(nameof(left.Name));
			Children = CreateChildren(left, right);
			Status = Children.All(t => t.Status == DiffStatus.Unchanged) ? DiffStatus.Unchanged : DiffStatus.Changed;
		}

		private static TreeNodeDiff[] CreateChildren(TypeTreeNodeDump left, TypeTreeNodeDump right)
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

				TypeTreeNodeDump leftChild = left.Children[li];
				TypeTreeNodeDump rightChild = right.Children[ri];
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

		private static bool IsNodeSame(TypeTreeNodeDump left, TypeTreeNodeDump right)
		{
			if (left.Name == right.Name)
			{
				if (left.Type == right.Type)
				{
					return true;
				}
			}
			return false;
		}

		private static bool IsNodePresent(TypeTreeNodeDump node, TypeTreeNodeDump right, int startIndex)
		{
			for (int ri = startIndex + 1; ri < right.Children.Count; ri++)
			{
				TypeTreeNodeDump rightChild = right.Children[ri];
				if (IsNodeSame(node, rightChild))
				{
					return true;
				}
			}
			return false;
		}

		public string Type { get; }
		public string Name { get; }
		public IReadOnlyList<TreeNodeDiff> Children { get; }
		public DiffStatus Status { get; }
	}
}

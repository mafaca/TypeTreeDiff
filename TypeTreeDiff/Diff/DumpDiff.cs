using System;
using System.Collections.Generic;

namespace TypeTreeDiff
{
	public sealed class DumpDiff
	{
		public DumpDiff(Dump left, Dump right)
		{
			if (left == null)
			{
				throw new ArgumentNullException(nameof(left));
			}
			if (right == null)
			{
				throw new ArgumentNullException(nameof(right));
			}

			LeftVersion = left.Version;
			RightVersion = right.Version;
			if (LeftVersion >= RightVersion)
			{
				throw new ArgumentException($"Left version {LeftVersion} should be less than right {RightVersion}");
			}

			Dictionary<int, TypeTreeDump> rightTrees = new Dictionary<int, TypeTreeDump>(right.TypeTrees.Count);
			foreach (TypeTreeDump rightTree in right.TypeTrees)
			{
				rightTrees.Add(rightTree.ClassID, rightTree);
			}

			List<TreeDiff> treeDiffs = new List<TreeDiff>();
			foreach (TypeTreeDump leftTree in left.TypeTrees)
			{
				if (rightTrees.TryGetValue(leftTree.ClassID, out TypeTreeDump rightTree))
				{
					TreeDiff treeDiff = new TreeDiff(leftTree, rightTree);
					treeDiffs.Add(treeDiff);
					rightTrees.Remove(leftTree.ClassID);
				}
				else
				{
					TreeDiff tree = new TreeDiff(leftTree, DiffStatus.Deleted);
					treeDiffs.Add(tree);
				}
			}
			foreach (TypeTreeDump rightTree in rightTrees.Values)
			{
				TreeDiff tree = new TreeDiff(rightTree, DiffStatus.Added);
				treeDiffs.Add(tree);
			}
			TreeDiffs = treeDiffs.ToArray();
		}

		public Version LeftVersion { get; }
		public Version RightVersion { get; }
		public IReadOnlyList<TreeDiff> TreeDiffs { get; }
	}
}

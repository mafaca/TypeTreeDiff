using System;
using System.Collections.Generic;

namespace TypeTreeDiff
{
	public sealed class DBDiff
	{
		public DBDiff(DBDump left, DBDump right)
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

			Dictionary<int, TreeDump> leftTrees = new Dictionary<int, TreeDump>(left.TypeTrees.Count);
			foreach (TreeDump leftTree in left.TypeTrees)
			{
				leftTrees.Add(leftTree.ClassID, leftTree);
			}
			Dictionary<int, TreeDump> rightTrees = new Dictionary<int, TreeDump>(right.TypeTrees.Count);
			foreach (TreeDump rightTree in right.TypeTrees)
			{
				rightTrees.Add(rightTree.ClassID, rightTree);
			}

			List<TreeDiff> treeDiffs = new List<TreeDiff>();
			for (int li = 0, ri = 0; li < left.TypeTrees.Count; li++)
			{
				TreeDump leftTree = left.TypeTrees[li];
				if (rightTrees.TryGetValue(leftTree.ClassID, out TreeDump rightTree))
				{
					TreeDiff treeDiff = new TreeDiff(leftTree, rightTree);
					treeDiffs.Add(treeDiff);
					rightTrees.Remove(leftTree.ClassID);
				}
				else
				{
					TreeDiff treeDiff = new TreeDiff(leftTree, DiffStatus.Deleted);
					treeDiffs.Add(treeDiff);
				}

				while (ri < right.TypeTrees.Count)
				{
					TreeDump tree = right.TypeTrees[ri++];
					if (leftTrees.ContainsKey(tree.ClassID))
					{
						break;
					}
					else
					{
						TreeDiff treeDiff = new TreeDiff(tree, DiffStatus.Added);
						treeDiffs.Add(treeDiff);
						rightTrees.Remove(tree.ClassID);
					}
				}
			}
			foreach (TreeDump rightTree in rightTrees.Values)
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

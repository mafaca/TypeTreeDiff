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

			Dictionary<int, TreeDump> rightTrees = new Dictionary<int, TreeDump>(right.TypeTrees.Count);
			foreach (TreeDump rightTree in right.TypeTrees)
			{
				rightTrees.Add(rightTree.ClassID, rightTree);
			}

			List<TreeDiff> treeDiffs = new List<TreeDiff>();
			foreach (TreeDump leftTree in left.TypeTrees)
			{
				if (rightTrees.TryGetValue(leftTree.ClassID, out TreeDump rightTree))
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
#warning TODO: combine
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

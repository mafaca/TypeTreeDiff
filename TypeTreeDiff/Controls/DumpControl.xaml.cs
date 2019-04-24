using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace TypeTreeDiff
{
	/// <summary>
	/// Interaction logic for DumpControl.xaml
	/// </summary>
	public partial class DumpControl : UserControl
	{
		public enum Position
		{
			Left,
			Right,
		}

		private class TreeInfo
		{
			public int ID { get; set; }
			public string Name { get; set; }
			public DiffStatus Status { get; set; }
		}

		private class TreeNodeInfo
		{
			public override string ToString()
			{
				return new string(' ', Indent * 6) + Type + " " + Name + (Align ? " (align)" : string.Empty);
			}

			public static TreeNodeInfo Empty { get; } = new TreeNodeInfo() { Type = string.Empty, Name = string.Empty };

			public int Indent { get; set; }
			public string Type { get; set; }
			public string Name { get; set; }
			public bool Align { get; set; }
			public DiffStatus Status { get; set; }
		}

		public event Action EventDumpDropped;
		public event Action EventDumpCreated;

		public event Action<string> EventDumpSortOrderChanged;
		public event Action<int> EventDumpSelectionChanged;
		public event Action<int> EventDumpTypeTreesSelected;
		public event Action<double> EventDumpHeaderSizeChanged;
		public event Action<double> EventDumpScrollChanged;

		public event Action EventTypeTreeBackClicked;
		public event Action<int> EventTypeTreeSelectionChanged;
		public event Action<double> EventTypeTreeScrollChanged;

		public Position DiffPosition { get; set; }
		public DBDump Dump { get; private set; }
		public DBDump DumpOptimized { get; private set; }
		public DBDiff Diff { get; private set; }

		private bool IsTypeTreeView => TypeTreeArea.Visibility == Visibility.Visible;

		public DumpControl()
		{
			InitializeComponent();

			DropArea.EventFileDropped += OnFileDropped;
			DropArea.Visibility = Visibility.Visible;
			DumpListView.ItemContainerGenerator.StatusChanged += OnDumpListViewStatusChanged;
			TypeTreeListBox.ItemContainerGenerator.StatusChanged += OnTypeTreeListBoxStatusChanged;
		}

		public void ShowDragAndDrop()
		{
			DropArea.Visibility = Visibility.Visible;
		}

		public void HideDragAndDrop()
		{
			if (Dump != null)
			{
				DropArea.Visibility = Visibility.Hidden;
			}
		}

		public void ShowDumpView()
		{
			DumpListView.Visibility = Visibility.Visible;
			TypeTreeArea.Visibility = Visibility.Hidden;
			CopyContentButton.Content = "Copy enum";
		}

		public void ShowTypeTreeView(int classID)
		{
			TreeDump treeDump = Dump.TypeTrees.FirstOrDefault(t => t.ClassID == classID);
			TreeDiff treeDiff = Diff.TreeDiffs.First(t => t.ClassID == classID);
			List<TreeNodeInfo> items = new List<TreeNodeInfo>();

			if (treeDump == null)
			{
				items.Add(TreeNodeInfo.Empty);
			}
			else
			{
				string baseHierarchy = string.Join(" <= ", treeDump.Inheritance);
				string hierarchy = baseHierarchy == string.Empty ? treeDump.ClassName : treeDump.ClassName + " <= " + baseHierarchy;

				DiffStatus headerStatus = DiffStatus.Changed;
				if (treeDiff.Status == DiffStatus.Added || treeDiff.Status == DiffStatus.Deleted || treeDiff.Status == DiffStatus.Invalid)
				{
					headerStatus = treeDiff.Status;
				}
				else
				{
					if (treeDiff.LeftClassName == treeDiff.RightClassName)
					{
						if (treeDiff.LeftBaseName == treeDiff.RightBaseName)
						{
							headerStatus = DiffStatus.Unchanged;
						}
					}
				}
				TreeNodeInfo info = new TreeNodeInfo() { Type = hierarchy, Status = headerStatus };
				items.Add(info);
			}

			if (treeDiff.Node != null)
			{
				items.Add(TreeNodeInfo.Empty);

				FillTypeTreeItems(items, treeDiff.Node, 0);
			}

			TypeTreeListBox.ItemsSource = items;
			DumpListView.Visibility = Visibility.Hidden;
			TypeTreeArea.Visibility = Visibility.Visible;
			CopyContentButton.Content = "Copy struct";
			SetTypeTreeScrollPosition(0.0);
		}

		public void SetDumpScrollPosition(double offset)
		{
			Decorator border = (Decorator)VisualTreeHelper.GetChild(DumpListView, 0);
			ScrollViewer scrollViewer = (ScrollViewer)border.Child;
			scrollViewer.ScrollToVerticalOffset(offset);
		}

		public void SetTypeTreeScrollPosition(double offset)
		{
			Decorator border = (Decorator)VisualTreeHelper.GetChild(TypeTreeListBox, 0);
			ScrollViewer scrollViewer = (ScrollViewer)border.Child;
			scrollViewer.ScrollToVerticalOffset(offset);
		}

		public void SortDumpItems(string property, ListSortDirection direction)
		{
			CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(DumpListView.ItemsSource);
			view.SortDescriptions.Clear();
			view.SortDescriptions.Add(new SortDescription(property, direction));
		}

		public void ProcessDumpFile(string filePath)
		{
			DropArea.IsEnabled = false;
			EventDumpDropped?.Invoke();
			ThreadPool.QueueUserWorkItem(ReadDump, filePath);
		}

		public void FillLeftDump(DBDiff diff)
		{
			Diff = diff;
			List<TreeInfo> list = new List<TreeInfo>();
			foreach (TreeDiff tree in Diff.TreeDiffs)
			{
				TreeInfo item = new TreeInfo { ID = tree.ClassID, Name = tree.LeftClassName, Status = tree.Status };
				list.Add(item);
			}
			DumpListView.ItemsSource = list;
			ChangedStack.Visibility = Visibility.Visible;
			ChangedLabel.Content = Diff.TreeDiffs.Count(t => t.Status == DiffStatus.Changed);
			AddedStack.Visibility = Visibility.Collapsed;
			RemovedStack.Visibility = Visibility.Visible;
			RemovedLabel.Content = Diff.TreeDiffs.Count(t => t.Status == DiffStatus.Deleted);
		}

		public void FillRightDump(DBDiff diff)
		{
			Diff = diff;
			List<TreeInfo> list = new List<TreeInfo>();
			foreach (TreeDiff tree in diff.TreeDiffs)
			{
				TreeInfo item = new TreeInfo { ID = tree.ClassID, Name = tree.RightClassName, Status = tree.Status };
				list.Add(item);
			}
			DumpListView.ItemsSource = list;
			ChangedStack.Visibility = Visibility.Visible;
			ChangedLabel.Content = Diff.TreeDiffs.Count(t => t.Status == DiffStatus.Changed);
			RemovedStack.Visibility = Visibility.Collapsed;
			AddedStack.Visibility = Visibility.Visible;
			AddedLabel.Content = Diff.TreeDiffs.Count(t => t.Status == DiffStatus.Added);
		}

		private void FillTypeTreeItems(List<TreeNodeInfo> items, TreeNodeDiff node, int indent)
		{
			TreeNodeInfo info = new TreeNodeInfo();
			info.Indent = indent;
			info.Type = DiffPosition == Position.Left ? node.LeftType : node.RightType;
			info.Name = node.Name;
			info.Align = DiffPosition == Position.Left ? node.LeftAlign : node.RightAlign;
			info.Status = node.Status;
			items.Add(info);

			IReadOnlyList<TreeNodeDiff> children = DiffPosition == Position.Left ? node.LeftChildren : node.RightChildren;
			foreach (TreeNodeDiff child in children)
			{
				FillTypeTreeItems(items, child, indent + 1);
			}
		}

		private void ReadDump(object state)
		{
			string filePath = (string)state;
			Dump = DBDump.Read(filePath);
			DumpOptimized = Dump.Optimize();
			Dispatcher.Invoke(() =>
			{
				VersionLabel.Content = Dump.Version.ToString();
				TypeLabel.Content = Dump.Type;
				CountLabel.Content = Dump.TypeTrees.Count.ToString();
				ChangedStack.Visibility = Visibility.Collapsed;
				AddedStack.Visibility = Visibility.Collapsed;
				RemovedStack.Visibility = Visibility.Collapsed;

				DropArea.IsEnabled = true;
				DropArea.Visibility = Visibility.Hidden;
			});
			EventDumpCreated?.Invoke();
		}

		private Color GetForegroundStatusColor(DiffStatus status)
		{
			switch (status)
			{
				case DiffStatus.Unchanged:
					return Colors.Black;
				case DiffStatus.Changed:
					return Colors.Black;
				case DiffStatus.Added:
					return DiffPosition == Position.Left ? Colors.Transparent : Colors.Black;
				case DiffStatus.Deleted:
					return DiffPosition == Position.Left ? Colors.White : Colors.Transparent;
				case DiffStatus.Invalid:
					return Colors.White;

				default:
					throw new Exception(status.ToString());
			}
		}

		private Color GetBackgroundStatusColor(DiffStatus status)
		{
			switch (status)
			{
				case DiffStatus.Unchanged:
					return Colors.LightYellow;
				case DiffStatus.Changed:
					return Colors.DarkGray;
				case DiffStatus.Added:
					return DiffPosition == Position.Left ? Colors.Transparent : Colors.LightGreen;
				case DiffStatus.Deleted:
					return DiffPosition == Position.Left ? Colors.DarkRed : Colors.Transparent;
				case DiffStatus.Invalid:
					return Colors.Black;

				default:
					throw new Exception(status.ToString());
			}
		}

		private string DumpToEnum()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("enum ClassIDType");
			sb.Append('{').AppendLine();
			int maxLen = (Dump.TypeTrees.Max(t => t.ClassName.Length) + 3) & ~3;
			foreach (TreeDump typeTree in Dump.TypeTrees.OrderBy(t => t.ClassID))
			{
				int padding = maxLen - typeTree.ClassName.Length;
				int tabs = (padding + 3) / 4;
				sb.Append('\t').Append(typeTree.ClassName);
				sb.Append('\t', tabs);
				sb.Append("= ").Append(typeTree.ClassID).Append(',').AppendLine();
			}
			sb.Append('}').AppendLine();
			return sb.ToString();
		}

		private string CopyTypeTree()
		{
			TreeInfo info = (TreeInfo)DumpListView.Items[DumpListView.SelectedIndex];
			TreeDump typeTree = Dump.TypeTrees.FirstOrDefault(t => t.ClassID == info.ID);
			if (typeTree == null)
			{
				return string.Empty;
			}

			// header
			StringBuilder sb = new StringBuilder();
			sb.Append("// classID{").Append(typeTree.ClassID).Append("}: ").Append(typeTree.ClassName);
			for (int i = 0; i < typeTree.Inheritance.Count; i++)
			{
				sb.Append(" <- ");
				sb.Append(typeTree.Inheritance[i]);
			}
			sb.AppendLine();

			// nodes
			CopyTypeTreeNode(typeTree, 0, sb);
			return sb.ToString();
		}

		private void CopyTypeTreeNode(TreeNodeDump node, int indent, StringBuilder sb)
		{
			sb.Append('\t', indent).Append(node.Type).Append(' ').Append(node.Name);
			// Nice bug, C#. Look at this beautiful piece of... code
			sb.AppendFormat(" // ByteSize{0}{1:x}{2}, Index{3}{4:x}{5}, IsArray{{{6}}}, MetaFlag{7}{8:x}{9}",
					"{", unchecked((uint)node.ByteSize), "}",
					"{", node.Index, "}",
					node.IsArray ? 1 : 0,
					"{", node.MetaFlag, "}").AppendLine();
			foreach (TreeNodeDump child in node.Children)
			{
				CopyTypeTreeNode(child, indent + 1, sb);
			}
		}

		// =================================
		// Custom events
		// =================================

		private void OnFileDropped(string filePath)
		{
			ProcessDumpFile(filePath);
		}

		private void OnDumpListViewStatusChanged(object sender, EventArgs e)
		{
			ItemContainerGenerator generator = (ItemContainerGenerator)sender;
			if (generator.Status == GeneratorStatus.ContainersGenerated)
			{
				for (int i = 0; i < Diff.TreeDiffs.Count; i++)
				{
					ListViewItem listItem = (ListViewItem)generator.ContainerFromIndex(i);
					if (listItem != null)
					{
						TreeInfo treeInfo = (TreeInfo)generator.Items[i];

						Color expectedForegroundColor = GetForegroundStatusColor(treeInfo.Status);
						if (listItem.Foreground is SolidColorBrush solidForeBrush)
						{
							if (solidForeBrush.Color != expectedForegroundColor)
							{
								listItem.Foreground = new SolidColorBrush(expectedForegroundColor);
							}
						}
						else
						{
							listItem.Foreground = new SolidColorBrush(expectedForegroundColor);
						}

						Color expectedBackgroundColor = GetBackgroundStatusColor(treeInfo.Status);
						if (listItem.Background is SolidColorBrush solidBackBrush)
						{
							if (solidBackBrush.Color != expectedBackgroundColor)
							{
								listItem.Background = new SolidColorBrush(expectedBackgroundColor);
							}
						}
						else
						{
							listItem.Background = new SolidColorBrush(expectedBackgroundColor);
						}
					}
				}
			}
		}

		private void OnTypeTreeListBoxStatusChanged(object sender, EventArgs e)
		{
			ItemContainerGenerator generator = (ItemContainerGenerator)sender;
			if (generator.Status == GeneratorStatus.ContainersGenerated)
			{
				for (int i = 0; i < generator.Items.Count; i++)
				{
					ListBoxItem listItem = (ListBoxItem)generator.ContainerFromIndex(i);
					if (listItem != null)
					{
						TreeNodeInfo info = (TreeNodeInfo)generator.Items[i];
						Color expectedForegroundColor = GetForegroundStatusColor(info.Status);
						if (listItem.Foreground is SolidColorBrush solidForeBrush)
						{
							if (solidForeBrush.Color != expectedForegroundColor)
							{
								listItem.Foreground = new SolidColorBrush(expectedForegroundColor);
							}
						}
						else
						{
							listItem.Foreground = new SolidColorBrush(expectedForegroundColor);
						}

						Color expectedBackgroundColor = GetBackgroundStatusColor(info.Status);
						if (listItem.Background is SolidColorBrush solidBackBrush)
						{
							if (solidBackBrush.Color != expectedBackgroundColor)
							{
								listItem.Background = new SolidColorBrush(expectedBackgroundColor);
							}
						}
						else
						{
							listItem.Background = new SolidColorBrush(expectedBackgroundColor);
						}
					}
				}
			}
		}

		// =================================
		// Form events
		// =================================

		private void OnDumpHeaderClicked(object sender, RoutedEventArgs e)
		{
			GridViewColumnHeader header = e.OriginalSource as GridViewColumnHeader;
			if (header == null)
			{
				return;
			}
			if (header.Role == GridViewColumnHeaderRole.Padding)
			{
				return;
			}

			EventDumpSortOrderChanged?.Invoke((string)header.Content);
		}

		private void OnDumpSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count > 0)
			{
				EventDumpSelectionChanged?.Invoke(DumpListView.SelectedIndex);
			}
		}

		private void OnDumpItemDoubleClicked(object sender, MouseButtonEventArgs e)
		{
			TreeInfo info = (TreeInfo)DumpListView.Items[DumpListView.SelectedIndex];
			EventDumpTypeTreesSelected?.Invoke(info.ID);
		}

		private void OnDumpScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			if (e.ExtentWidthChange != 0.0)
			{
				EventDumpHeaderSizeChanged?.Invoke(e.ExtentWidth);
			}
			if (e.VerticalChange != 0.0)
			{
				EventDumpScrollChanged?.Invoke(e.VerticalOffset);
			}
		}

		private void OnTypeTreeBackClicked(object sender, RoutedEventArgs e)
		{
			EventTypeTreeBackClicked?.Invoke();
		}

		private void OnTypeTreeSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count > 0)
			{
				EventTypeTreeSelectionChanged?.Invoke(TypeTreeListBox.SelectedIndex);
			}
		}

		private void OnTypeTreeNodeDoubleClicked(object sender, MouseButtonEventArgs e)
		{
			TreeNodeInfo item = (TreeNodeInfo)TypeTreeListBox.SelectedItem;
			Clipboard.SetText($"{item.Type} {item.Name}");
		}

		private void OnTypeTreeScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			if (e.VerticalChange != 0.0)
			{
				EventTypeTreeScrollChanged?.Invoke(e.VerticalOffset);
			}
		}

		private void OnCopyContentButtonClicked(object sender, RoutedEventArgs e)
		{
			if (IsTypeTreeView)
			{
				Clipboard.SetText(CopyTypeTree());
			}
			else
			{
				Clipboard.SetText(DumpToEnum());
			}
		}
	}
}

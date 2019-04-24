using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
		public Dump Dump { get; private set; }
		private DumpDiff Diff { get; set;}

		private readonly Dictionary<int, DiffStatus> m_dumpStatus = new Dictionary<int, DiffStatus>();
		private readonly List<DiffStatus> m_typeTreeStatus = new List<DiffStatus>();

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

		public void FillLeftDump(DumpDiff diff)
		{
			Diff = diff;
			List<TreeInfo> list = new List<TreeInfo>();
			foreach (TreeDiff tree in Diff.TreeDiffs)
			{
				TreeInfo item = new TreeInfo { ID = tree.ClassID, Name = tree.LeftClassName };
				list.Add(item);
			}
			DumpListView.ItemsSource = list;
			ChangedStack.Visibility = Visibility.Visible;
			ChangedLabel.Content = Diff.TreeDiffs.Count(t => t.Status == DiffStatus.Changed);
			AddedStack.Visibility = Visibility.Collapsed;
			RemovedStack.Visibility = Visibility.Visible;
			RemovedLabel.Content = Diff.TreeDiffs.Count(t => t.Status == DiffStatus.Deleted);
		}

		public void FillRightDump(DumpDiff diff)
		{
			Diff = diff;
			List<TreeInfo> list = new List<TreeInfo>();
			foreach (TreeDiff tree in diff.TreeDiffs)
			{
				TreeInfo item = new TreeInfo { ID = tree.ClassID, Name = tree.RightClassName };
				list.Add(item);
			}
			DumpListView.ItemsSource = list;
			ChangedStack.Visibility = Visibility.Visible;
			ChangedLabel.Content = Diff.TreeDiffs.Count(t => t.Status == DiffStatus.Changed);
			RemovedStack.Visibility = Visibility.Collapsed;
			AddedStack.Visibility = Visibility.Visible;
			AddedLabel.Content = Diff.TreeDiffs.Count(t => t.Status == DiffStatus.Added);
		}

		public void FillTypeTree(int classID)
		{
			TreeDiff tree = Diff.TreeDiffs.First(t => t.ClassID == classID);
			TypeTreeDump treeDump = Dump.TypeTrees.First(t => t.ClassID == classID);
			List<string> items = new List<string>();

			string baseHierarchy = string.Join(" <= ", treeDump.Inheritance);
			string hierarchy = baseHierarchy == string.Empty ? treeDump.ClassName : treeDump.ClassName + " <= " + baseHierarchy;
			items.Add(hierarchy);

			m_typeTreeStatus.Clear();
			m_typeTreeStatus.Add(tree.Status);

			if (tree.Node != null)
			{
				items.Add(string.Empty);
				m_typeTreeStatus.Add(DiffStatus.Unchanged);

				FillTypeTreeItems(items, tree.Node, 0);
			}
			TypeTreeListBox.ItemsSource = items;
			DumpListView.Visibility = Visibility.Hidden;
			TypeTreeArea.Visibility = Visibility.Visible;
		}

		private void FillTypeTreeItems(List<string> items, TreeNodeDiff node, int indent)
		{
			string name = new string(' ', indent * 2) + node.Type + " " + node.Name;
			items.Add(name);
			m_typeTreeStatus.Add(node.Status);

			foreach (TreeNodeDiff child in node.Children)
			{
				FillTypeTreeItems(items, child, indent + 1);
			}
		}

		public void SortDumpItems(string property, ListSortDirection direction)
		{
			CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(DumpListView.ItemsSource);
			view.SortDescriptions.Clear();
			view.SortDescriptions.Add(new SortDescription(property, direction));
		}

		private void ReadDump(object state)
		{
			string filePath = (string)state;
			Dump = Dump.Read(filePath);
			Dispatcher.Invoke(() =>
			{
				VersionLabel.Content = Dump.Version.ToString();
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
					{
						if (DiffPosition == Position.Left)
						{
							Color color = new Color();
							color.R = color.G = color.B = color.A = 0;
							return color;
						}
						else
						{
							return Colors.Black;
						}
					}
				case DiffStatus.Deleted:
					{
						if (DiffPosition == Position.Left)
						{
							return Colors.White;
						}
						else
						{
							Color color = new Color();
							color.R = color.G = color.B = color.A = 0;
							return color;
						}
					}
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
					return Colors.CornflowerBlue;
				case DiffStatus.Added:
					return DiffPosition == Position.Left ? Colors.LightYellow : Colors.LightGreen;
				case DiffStatus.Deleted:
					return DiffPosition == Position.Right ? Colors.LightYellow : Colors.DarkRed;
				case DiffStatus.Invalid:
					return Colors.Black;

				default:
					throw new Exception(status.ToString());
			}
		}

		// =================================
		// Custom events
		// =================================

		private void OnFileDropped(string filePath)
		{
			DropArea.IsEnabled = false;
			EventDumpDropped?.Invoke();
			ThreadPool.QueueUserWorkItem(ReadDump, filePath);
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
						TreeDiff tree = Diff.TreeDiffs.First(t => t.ClassID == treeInfo.ID);

						Color expectedForegroundColor = GetForegroundStatusColor(tree.Status);
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

						Color expectedBackgroundColor = GetBackgroundStatusColor(tree.Status);
						if (listItem.Background is SolidColorBrush solidBackBrush)
						{
							if (solidBackBrush.Color != expectedForegroundColor)
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
				for (int i = 0; i < m_typeTreeStatus.Count; i++)
				{
					ListBoxItem listItem = (ListBoxItem)generator.ContainerFromIndex(i);
					if (listItem != null)
					{
						Color expectedForegroundColor = GetForegroundStatusColor(m_typeTreeStatus[i]);
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

						Color expectedBackgroundColor = GetBackgroundStatusColor(m_typeTreeStatus[i]);
						if (listItem.Background is SolidColorBrush solidBackBrush)
						{
							if (solidBackBrush.Color != expectedForegroundColor)
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
				System.Diagnostics.Debug.WriteLine("Selected " + TypeTreeListBox.SelectedIndex);
				EventTypeTreeSelectionChanged?.Invoke(TypeTreeListBox.SelectedIndex);
			}
		}

		private void OnTypeTreeScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			if (e.VerticalChange != 0.0)
			{
				EventTypeTreeScrollChanged?.Invoke(e.VerticalOffset);
			}
		}
	}
}

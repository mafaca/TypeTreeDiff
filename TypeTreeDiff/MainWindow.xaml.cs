using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TypeTreeDiff
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private string m_treeSortPropery;
		private ListSortDirection m_treeSortDirection;

		public MainWindow()
		{
			InitializeComponent();

			LeftDump.DiffPosition = DumpControl.Position.Left;
			RightDump.DiffPosition = DumpControl.Position.Right;

			LeftDump.EventDumpDropped += OnDumpDropped;
			RightDump.EventDumpDropped += OnDumpDropped;
			LeftDump.EventDumpCreated += OnDumpCreated;
			RightDump.EventDumpCreated += OnDumpCreated;
			LeftDump.EventDumpSortOrderChanged += OnDumpSortOrderChanged;
			RightDump.EventDumpSortOrderChanged += OnDumpSortOrderChanged;
			LeftDump.EventDumpSelectionChanged += (index) => OnDumpSelectionChanged(RightDump, index);
			RightDump.EventDumpSelectionChanged += (index) => OnDumpSelectionChanged(LeftDump, index);
			LeftDump.EventDumpTypeTreesSelected += (classID) => OnDumpTypeTreeSelected(classID);
			RightDump.EventDumpTypeTreesSelected += (classID) => OnDumpTypeTreeSelected(classID);
			LeftDump.EventDumpHeaderSizeChanged += (offset) => OnDumpHeaderSizeChanged(RightDump, LeftDump);
			RightDump.EventDumpHeaderSizeChanged += (offset) => OnDumpHeaderSizeChanged(LeftDump, RightDump);
			LeftDump.EventDumpScrollChanged += (offset) => OnDumpScrollChanged(RightDump, offset);
			RightDump.EventDumpScrollChanged += (offset) => OnDumpScrollChanged(LeftDump, offset);

			LeftDump.EventTypeTreeBackClicked += OnTypeTreeBackClicked;
			RightDump.EventTypeTreeBackClicked += OnTypeTreeBackClicked;
			LeftDump.EventTypeTreeSelectionChanged += (index) => OnTypeTreeSelectionChanged(RightDump, index);
			RightDump.EventTypeTreeSelectionChanged += (index) => OnTypeTreeSelectionChanged(LeftDump, index);
			LeftDump.EventTypeTreeScrollChanged += (offset) => OnTypeTreeScrollChanged(RightDump, offset);
			RightDump.EventTypeTreeScrollChanged += (offset) => OnTypeTreeScrollChanged(LeftDump, offset);

			string[] args = Environment.GetCommandLineArgs();
			ProcessArguments(args);
		}

		private void ProcessArguments(string[] args)
		{
			if (args.Length < 2)
			{
				return;
			}

			string leftFile = args[1];
			if (!File.Exists(leftFile))
			{
				MessageBox.Show($"File '{leftFile}' doesn't exists");
				return;
			}

			LeftDump.ProcessDumpFile(leftFile);
			if (args.Length == 2)
			{
				return;
			}

			string rightFile = args[2];
			if (!File.Exists(rightFile))
			{
				MessageBox.Show($"File '{rightFile}' doesn't exists");
				return;
			}

			RightDump.ProcessDumpFile(rightFile);
		}

		// =================================
		// Custom events
		// =================================

		private void OnDumpDropped()
		{
			m_treeSortPropery = null;

			LeftDump.HideDragAndDrop();
			RightDump.HideDragAndDrop();
			LeftDump.ShowDumpView();
			RightDump.ShowDumpView();
		}

		private void OnDumpCreated()
		{
			if (LeftDump.Dump == null)
			{
				return;
			}
			if (RightDump.Dump == null)
			{
				return;
			}

			DBDiff diff = new DBDiff(LeftDump.DumpOptimized, RightDump.DumpOptimized);
			Dispatcher.InvokeAsync(() =>
			{
				LeftDump.FillLeftDump(diff);
				RightDump.FillRightDump(diff);

				Color foreColor = diff.LeftVersion <= diff.RightVersion ? Colors.Black : Colors.White;
				Color backColor = diff.LeftVersion <= diff.RightVersion ? Colors.Transparent : Colors.Red;
				LeftDump.VersionLabel.Foreground = new SolidColorBrush(foreColor);
				LeftDump.VersionLabel.Background = new SolidColorBrush(backColor);
				RightDump.VersionLabel.Foreground = new SolidColorBrush(foreColor);
				RightDump.VersionLabel.Background = new SolidColorBrush(backColor);
			});
		}

		private void OnDumpSortOrderChanged(string property)
		{
			if (property != m_treeSortPropery)
			{
				m_treeSortPropery = property;
				m_treeSortDirection = ListSortDirection.Ascending;
			}
			else
			{
				m_treeSortDirection = m_treeSortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
			}
			LeftDump.SortDumpItems(m_treeSortPropery, m_treeSortDirection);
			RightDump.SortDumpItems(m_treeSortPropery, m_treeSortDirection);
		}

		private void OnDumpSelectionChanged(DumpControl dump, int index)
		{
			dump.DumpListView.SelectedIndex = index;
		}

		private void OnDumpTypeTreeSelected(int classID)
		{
			LeftDump.ShowTypeTreeView(classID);
			RightDump.ShowTypeTreeView(classID);
			RightDump.TypeTreeListBox.Focus();
		}
		
		private void OnDumpHeaderSizeChanged(DumpControl dest, DumpControl source)
		{
			dest.DumpIDHeader.Width = source.DumpIDHeader.Width;
			dest.DumpNameHeader.Width = source.DumpNameHeader.Width;
		}

		private void OnDumpScrollChanged(DumpControl dump, double offset)
		{
			dump.SetDumpScrollPosition(offset);
		}

		private void OnTypeTreeBackClicked()
		{
			LeftDump.ShowDumpView();
			RightDump.ShowDumpView();
			ListViewItem listItem = (ListViewItem)RightDump.DumpListView.ItemContainerGenerator.ContainerFromIndex(RightDump.DumpListView.SelectedIndex);
			if (listItem != null)
			{
				listItem.Focus();
			}
		}

		private void OnTypeTreeSelectionChanged(DumpControl dump, int index)
		{
			dump.TypeTreeListBox.SelectedIndex = index;
		}
		
		private void OnTypeTreeScrollChanged(DumpControl dump, double offset)
		{
			dump.SetTypeTreeScrollPosition(offset);
		}

		// =================================
		// Form events
		// =================================

		private void OnDragEnter(object sender, DragEventArgs e)
		{
			if (sender == this)
			{
				LeftDump.ShowDragAndDrop();
				RightDump.ShowDragAndDrop();
			}
		}

		private void OnDragLeave(object sender, DragEventArgs e)
		{
			if (sender == this)
			{
				LeftDump.HideDragAndDrop();
				RightDump.HideDragAndDrop();
			}
		}
	}
}

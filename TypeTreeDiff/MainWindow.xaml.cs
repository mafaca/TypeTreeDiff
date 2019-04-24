using System.ComponentModel;
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
			
		}

		// =================================
		// Custom events
		// =================================

		private void OnDumpDropped()
		{
			LeftDump.HideDragAndDrop();
			RightDump.HideDragAndDrop();
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

			DumpDiff diff = new DumpDiff(LeftDump.Dump, RightDump.Dump);
			Dispatcher.InvokeAsync(() =>
			{
				LeftDump.FillLeftDump(diff);
				RightDump.FillRightDump(diff);
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
			LeftDump.FillTypeTree(classID);
			RightDump.FillTypeTree(classID);
		}
		
		private void OnDumpHeaderSizeChanged(DumpControl dest, DumpControl source)
		{
			dest.DumpIDHeader.Width = source.DumpIDHeader.Width;
			dest.DumpNameHeader.Width = source.DumpNameHeader.Width;
		}

		private void OnDumpScrollChanged(DumpControl dump, double offset)
		{
			Decorator border = (Decorator)VisualTreeHelper.GetChild(dump.DumpListView, 0);
			ScrollViewer scrollViewer = (ScrollViewer)border.Child;
			scrollViewer.ScrollToVerticalOffset(offset);
		}

		private void OnTypeTreeBackClicked()
		{
			LeftDump.DumpListView.Visibility = Visibility.Visible;
			LeftDump.TypeTreeArea.Visibility = Visibility.Hidden;
			RightDump.DumpListView.Visibility = Visibility.Visible;
			RightDump.TypeTreeArea.Visibility = Visibility.Hidden;
		}

		private void OnTypeTreeSelectionChanged(DumpControl dump, int index)
		{
			dump.TypeTreeListBox.SelectedIndex = index;
		}
		
		private void OnTypeTreeScrollChanged(DumpControl dump, double offset)
		{
			Decorator border = (Decorator)VisualTreeHelper.GetChild(dump.TypeTreeListBox, 0);
			ScrollViewer scrollViewer = (ScrollViewer)border.Child;
			scrollViewer.ScrollToVerticalOffset(offset);
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

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TypeTreeDiff
{
	public partial class DropArea : UserControl
	{
		public event Action<string> EventFileDropped;

		public static readonly DependencyProperty ActiveDropColorProperty =
			DependencyProperty.Register(nameof(ActiveDropColor), typeof(Color), typeof(DropArea));
		public static readonly DependencyProperty InactiveDropColorProperty =
			DependencyProperty.Register(nameof(InactiveDropColor), typeof(Color), typeof(DropArea), new PropertyMetadata(OnInactiveDropColorChanged));

		public Color ActiveDropColor
		{
			get { return (Color)GetValue(ActiveDropColorProperty); }
			set { SetValue(ActiveDropColorProperty, value); }
		}

		public Color InactiveDropColor
		{
			get { return (Color)GetValue(InactiveDropColorProperty); }
			set { SetValue(InactiveDropColorProperty, value); }
		}

		public DropArea()
		{
			InitializeComponent();
		}

		private static void OnInactiveDropColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			DropArea dropArea = (DropArea)d;
			dropArea.Area.Background = new SolidColorBrush((Color)e.NewValue);
		}

		// =================================
		// Events
		// =================================

		private void OnDragEnter(object sender, DragEventArgs e)
		{
			Area.Background = new SolidColorBrush(ActiveDropColor);
		}

		private void OnDragLeave(object sender, DragEventArgs e)
		{
			Area.Background = new SolidColorBrush(InactiveDropColor);
		}

		private void OnDropped(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				EventFileDropped?.Invoke(files[0]);
			}
		}
	}
}

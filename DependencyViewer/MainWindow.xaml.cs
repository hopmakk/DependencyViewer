using System.Windows;
using DependencyViewer.Pages;

namespace DependencyViewer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private AppCenter _center = AppCenter.GetInstanse();

		public MainWindow()
		{
			InitializeComponent();
			_center.MainWindow = this;
			mainPageFrame.Navigate(_center.DragAndDropPage);
		}
	}
}
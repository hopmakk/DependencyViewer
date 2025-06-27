using System.Windows.Controls;
using DependencyViewer.Pages;

namespace DependencyViewer
{
	class AppCenter
    {
		private AppCenter() { }

        private static AppCenter Instance;

        public static AppCenter GetInstanse()
        {
            if (Instance == null)
			{
				Instance = new AppCenter();
				Instance.Init();
			}
				


            return Instance;
		}


		public DragAndDropPage DragAndDropPage { get; set; }
		public DependencyViewPage DependencyViewPage { get; set; }
		public MainWindow MainWindow { get; set; }


		public void Navigate(Page page)
		{
			MainWindow.mainPageFrame.Navigate(page);
		}


		private void Init()
		{
			DragAndDropPage = new DragAndDropPage();
			DependencyViewPage = new DependencyViewPage();
		}
	}
}

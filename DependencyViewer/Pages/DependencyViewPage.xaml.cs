using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DependencyViewer.Models;
using DependencyViewer.Services;

namespace DependencyViewer.Pages
{
	/// <summary>
	/// Логика взаимодействия для DependencyViewPage.xaml
	/// </summary>
	public partial class DependencyViewPage : Page
    {
		private const int METHOD_GAP = 30; // Расстояние между методами одного класса
		private const int CANVAS_EXPANSION_PER_CLASS = 100; // На сколько пикселей увеличить Canvas при добавлении класса
		private const int CLASSES_PER_ROW = 5; // классов в строке (при первой отрисовке)
		private const int CLASS_WIDTH = 100; // Ширина класса

		public List<ClassInfo> ClassesInfo { get; set; }
        public List<MethodInfo> MethodsInfo { get; set; }
        public List<DependencyLine> Dependencies { get; set; }

		private AppCenter _center = AppCenter.GetInstanse();
		private ClassInfo[,] _classesFirstPositions;

		private bool _isDragClass;
		private ClassInfo _dragClassInfo;
		private Vector2 _lastMouseClickPos;

		private Color _colorGray = Color.FromArgb(255, 80, 80, 100);
		private Color _colorBlue1 = Color.FromArgb(255, 65, 90, 110);
		private Color _colorBlue2 = Color.FromArgb(255, 130, 170, 200);
		private Color _colorCyan = Color.FromArgb(255, 78, 201, 176);
		private Color _colorYellow1 = Color.FromArgb(255, 140, 140, 100);
		private Color _colorYellow2 = Color.FromArgb(255, 220, 220, 170);


        public DependencyViewPage()
        {
            InitializeComponent();
			ClassesInfo = new List<ClassInfo>();
			MethodsInfo = new List<MethodInfo>();
			Dependencies = new List<DependencyLine>();
			_lastMouseClickPos = new Vector2();
			CanvasGrid.MouseDown += CanvasGrid_MouseDown;
			CanvasGrid.MouseUp += CanvasGrid_MouseUp;
		}


		public void CreateDiagram()
		{
			DiagramCanvas.Width = 1000;
			DiagramCanvas.Height = (ClassesInfo.Count + 2) * CANVAS_EXPANSION_PER_CLASS; // 2 - чтоб побольше было, на всякий

			foreach (ClassInfo classInfo in ClassesInfo)
			{
				var height = METHOD_GAP * (classInfo.Methods.Count + 1);
				classInfo.Height = height;
			}

			_classesFirstPositions = new ClassInfo[CLASSES_PER_ROW, ClassesInfo.Count / CLASSES_PER_ROW + 1]; // 1 - чтоб хватило места, на всякий

			CalculatePositions();
			DrawClasses();
			DrawDependencies();
		}


		private void CalculatePositions()
		{
			int x;
			int y;
			var firstPos = 30;
			ClassesInfo[0].Position = new Vector2(firstPos, firstPos);

			var lastClassIndex = 0;
			// назначим классам позиции
			for (int i = 0; i < _classesFirstPositions.GetLength(0); i++)
			{
				for (int j = 0; j < _classesFirstPositions.GetLength(1); j++)
				{
					if (lastClassIndex < ClassesInfo.Count)
					{
						_classesFirstPositions[i, j] = ClassesInfo[lastClassIndex];
						lastClassIndex++;
					}
				}
			}

			// определим координаты позиции классов на позициях
			for (int i = 0; i < _classesFirstPositions.GetLength(0); i++)
			{
				for (int j = 0; j < _classesFirstPositions.GetLength(1); j++)
				{
					if (_classesFirstPositions[i, j] != null)
					{
						if (i == 0)
							y = firstPos;
						else
							y = (int)_classesFirstPositions[i - 1, j].Position.Y + _classesFirstPositions[i - 1, j].Height + 50;

						if (j == 0)
							x = firstPos;
						else
							x = (int)_classesFirstPositions[i, j - 1].Position.X + CLASS_WIDTH * 2;

						_classesFirstPositions[i, j].Position = new Vector2(x, y);
					}
				}
			}
		}


		private void DrawClasses()
		{
			// проходим по каждому классу
			for (int i = 0; i < ClassesInfo.Count; i++)
			{
				DrawClass(ClassesInfo[i]);
			}
		}


		private void DrawClass(ClassInfo classInfo)
		{
			
			var stopGradientColor = _colorBlue1;
			stopGradientColor.A = 0;

			GradientStopCollection gsc =
			[
				new GradientStop() { Color = _colorBlue1, Offset = 0.0 },
				new GradientStop() { Color = stopGradientColor, Offset = 1.0 },
			];

			Rectangle rect = new Rectangle();
			rect.StrokeThickness = 0;
			rect.Width = 100;
			rect.Height = classInfo.Height;
			rect.Fill = new LinearGradientBrush(gsc, 0);
			rect.MouseDown += ClassRect_OnClick;
			rect.MouseEnter += ClassRect_MouseEnter;
			rect.MouseLeave += ClassRect_MouseLeave;
			rect.MouseUp += Rect_MouseUp;
			DiagramCanvas.Children.Add(rect);

			Line line = new Line();
			line.StrokeThickness = 1;
			line.Stroke = new SolidColorBrush(_colorBlue2);
			DiagramCanvas.Children.Add(line);

			TextBlock classNameTB = new TextBlock();
			classNameTB.Text = classInfo.ShortPath;
			classNameTB.Foreground = new SolidColorBrush(_colorCyan);
			DiagramCanvas.Children.Add(classNameTB);

			for (int i = 0; i < classInfo.Methods.Count; i++)
			{
				var methodPos = new Vector2(classInfo.Position.X - 2, 
											classInfo.Position.Y + METHOD_GAP * (i + 1));
				classInfo.Methods[i].Position = methodPos;
				DrawMethod(classInfo.Methods[i]);
			}

			classInfo.UIElements.Add(rect);
			classInfo.UIElements.Add(line);
			classInfo.UIElements.Add(classNameTB);

			RedrawClass(classInfo);
		}


		public void RedrawClass(ClassInfo classInfo)
		{
            foreach (var item in classInfo.UIElements)
            {
                if (item is Rectangle)
				{
					Canvas.SetLeft(item, classInfo.Position.X);
					Canvas.SetTop(item, classInfo.Position.Y);
				}
				else if (item is Line)
				{
					var line = (Line) item;
					line.X1 = classInfo.Position.X;
					line.Y1 = classInfo.Position.Y;
					line.X2 = classInfo.Position.X;
					line.Y2 = classInfo.Position.Y + classInfo.Height;
				}
				else if (item is TextBlock)
				{
					Canvas.SetLeft(item, classInfo.Position.X + 5);
					Canvas.SetTop(item, classInfo.Position.Y);
				}
			}

			for (int i = 0; i < classInfo.Methods.Count; i++)
			{
				var methodPos = new Vector2(classInfo.Position.X - 2,
											classInfo.Position.Y + METHOD_GAP * (i + 1));
				classInfo.Methods[i].Position = methodPos;
				RedrawMethod(classInfo.Methods[i]);
			}
		}


		private void DrawMethod(MethodInfo methodInfo)
		{
			Ellipse circle = new Ellipse();
			circle.StrokeThickness = 1;
			circle.Width = 5;
			circle.Height = 5;
			circle.Stroke = new SolidColorBrush(_colorYellow2);
			circle.Fill = new SolidColorBrush(_colorYellow2);
			DiagramCanvas.Children.Add(circle);

			TextBlock methodNameTB = new TextBlock();
			methodNameTB.Text = methodInfo.Name + "( )";
			if (methodInfo.NestedMethods.Count > 0) 
				methodNameTB.Foreground = new SolidColorBrush(_colorYellow2);
			else
				methodNameTB.Foreground = new SolidColorBrush(_colorYellow1);
			DiagramCanvas.Children.Add(methodNameTB);

			methodInfo.UIElements.Add(circle);
			methodInfo.UIElements.Add(methodNameTB);

			RedrawMethod(methodInfo);
		}


		public void RedrawMethod(MethodInfo methodInfo)
		{
			foreach (var item in methodInfo.UIElements)
			{
				if (item is Ellipse)
				{
					Canvas.SetLeft(item, methodInfo.Position.X);
					Canvas.SetTop(item, methodInfo.Position.Y);
				}
				else if (item is TextBlock)
				{
					Canvas.SetLeft(item, methodInfo.Position.X + 8);
					Canvas.SetTop(item, methodInfo.Position.Y);
				}
			}
		}


		private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
		{
			ScrollViewer scrollviewer = sender as ScrollViewer;

			if (scrollviewer == null) return;

			if (Keyboard.IsKeyDown(Key.LeftShift))
			{
				if (e.Delta > 0)
					scrollviewer.LineLeft();
				else
					scrollviewer.LineRight();
			}
			else
			{
				if (e.Delta > 0)
					scrollviewer.LineUp();
				else
					scrollviewer.LineDown();
			}

			e.Handled = true;
		}


		private void ClassRect_OnClick(object sender, MouseButtonEventArgs e)
		{
			Rectangle rect = (Rectangle)sender;
			var pos = new Vector2((float)Canvas.GetLeft(rect), (float)Canvas.GetTop(rect));
			
			ClassInfo classInfo = FindClassFromPos(pos);

			if (classInfo != null)
			{
				// начали перетаскивать класс
				_isDragClass = true;
				var mousePos = Mouse.GetPosition(_center.MainWindow);
				_lastMouseClickPos = new Vector2((float)mousePos.X, (float)mousePos.Y);
				_dragClassInfo = classInfo;

				TextLog.Text = classInfo.LongPath;

				// отобразить код
				var code = "";
				classInfo.Code.ForEach(x => code += x + '\n');

				CodeRTB.Document.Blocks.Clear();
				CodeRTB.Document.Blocks.Add(new Paragraph(new Run(code)));

				// подсвечиваем методы
				for (int i = 0; i < classInfo.Methods.Count; i++)
				{
					Color color;

					if (classInfo.Methods[i].NestedMethods.Count > 0)
						color = _colorYellow2;
					else 
						color = _colorYellow1;

					TextManipulation.FromTextPointer(CodeRTB.Selection.Start, CodeRTB.Selection.End, classInfo.Methods[i].Name + '(', new FontStyle(), new FontWeight(), new SolidColorBrush(color), Brushes.Transparent, 14);
				}
			}
		}


		private ClassInfo FindClassFromPos(Vector2 pos)
		{
			return ClassesInfo.Where(c => c.Position == pos).FirstOrDefault();
		}


		private void ClassRect_MouseEnter(object sender, MouseEventArgs e)
		{
			Rectangle rect = (Rectangle)sender;

			var stopGradientColor = _colorBlue2;
			stopGradientColor.A = 0;

			GradientStopCollection gsc =
			[
				new GradientStop() { Color = _colorBlue1, Offset = 0.0 },
				new GradientStop() { Color = stopGradientColor, Offset = 1.0 },
			];
			rect.Fill = new LinearGradientBrush(gsc, 0);
		}


		private void ClassRect_MouseLeave(object sender, MouseEventArgs e)
		{
			Rectangle rect = (Rectangle)sender;

			var stopGradientColor = _colorBlue1;
			stopGradientColor.A = 0;

			GradientStopCollection gsc =
			[
				new GradientStop() { Color = _colorBlue1, Offset = 0.0 },
				new GradientStop() { Color = stopGradientColor, Offset = 1.0 },
			];
			rect.Fill = new LinearGradientBrush(gsc, 0);
		}


		private void Rect_MouseUp(object sender, MouseButtonEventArgs e)
		{
			_isDragClass = false;
		}


		private void CanvasGrid_MouseDown(object sender, MouseButtonEventArgs e)
		{
			_isDragClass = false;
		}


		private void CanvasGrid_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (_isDragClass)
			{
				_isDragClass = false;
				var mousePos = Mouse.GetPosition(_center.MainWindow);
				var mousePosVector = new Vector2((float)mousePos.X, (float)mousePos.Y);
				var deltaPos = mousePosVector - _lastMouseClickPos;

				var classInfo = _dragClassInfo;
				classInfo.Position += deltaPos;
				RedrawClass(classInfo);
				RedrawDependencies(classInfo);
			}
		}


		private void DrawDependencies()
		{
			foreach (var method in MethodsInfo)
			{
				foreach (var nestedMethod in method.NestedMethods)
				{
					var dependency = new DependencyLine(method, nestedMethod);
					Dependencies.Add(dependency);

					dependency.Line.StrokeThickness = 1;
					dependency.Line.Stroke = new SolidColorBrush(_colorYellow1);
					DiagramCanvas.Children.Add(dependency.Line);
				}
			}
		}


		private void RedrawDependencies(ClassInfo classInfo)
		{
			foreach (var method in classInfo.Methods)
			{
				method.DependencyLines.ForEach(d => d.UpdateLineCoords());
			}
		}


		private void HideUndependClasses_Click(object sender, RoutedEventArgs e)
		{
            foreach (var classInfo in ClassesInfo)
            {
				var classHasNotDependencies = true;

                foreach (var methodInfo in classInfo.Methods)
                {
					if (methodInfo.DependencyLines.Count != 0)
					{
						classHasNotDependencies = false;
						break;
					}

				}

                if (classHasNotDependencies)
				{
                    foreach (var item1 in classInfo.UIElements)
                    {
						DiagramCanvas.Children.Remove(item1);
					}

					foreach (var methodInfo in classInfo.Methods)
					{
						foreach (var item1 in methodInfo.UIElements)
						{
							DiagramCanvas.Children.Remove(item1);
						}
					}
				}
            }
        }


		private void HideUndependMethods_Click(object sender, RoutedEventArgs e)
		{
			foreach (var methodInfo in MethodsInfo)
			{
				if (methodInfo.DependencyLines.Count == 0)
				{
					foreach (var item1 in methodInfo.UIElements)
					{
						DiagramCanvas.Children.Remove(item1);
					}
				}
			}
		}


		private void OpenClassMethodsDiagram_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}

using System.Windows.Shapes;

namespace DependencyViewer.Models
{
	public class DependencyLine
	{
		public MethodInfo Method1 { get; set; }
		public MethodInfo Method2 { get; set; }
		public Line Line { get; set; }
		public Rectangle Arrow { get; set; }


		public DependencyLine(MethodInfo method1, MethodInfo method2)
		{
			Method1 = method1;
			Method2 = method2;

			Line = new Line();
			UpdateLineCoords();

			Method1.DependencyLines.Add(this);
			Method2.DependencyLines.Add(this);
		}


		public void UpdateLineCoords()
		{
			Line.X1 = Method1.Position.X;
			Line.Y1 = Method1.Position.Y;

			Line.X2 = Method2.Position.X;
			Line.Y2 = Method2.Position.Y;
		}
	}
}

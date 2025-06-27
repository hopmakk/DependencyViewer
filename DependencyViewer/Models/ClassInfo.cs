using System.Numerics;
using System.Windows;

namespace DependencyViewer.Models
{
	public class ClassInfo
    {
        public string Name { get; set; }
        public string ShortPath { get; set; }
        public string LongPath { get; set; }
		public List<string> Code { get; set; }
		public List<MethodInfo> Methods { get; set; }

		public Vector2 Position { get; set; }
		public int Height { get; set; }
		public List<UIElement> UIElements { get; set; }


		public ClassInfo(string name, string shortPath, string longPath, List<string> code, List<MethodInfo> methods)
		{
			Name = name;
			ShortPath = shortPath;
			LongPath = longPath;
			Code = code;
			Methods = methods;

			Position = new Vector2(0,0);
			UIElements = new List<UIElement>();
		}
	}
}

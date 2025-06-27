using System.Numerics;
using System.Windows;

namespace DependencyViewer.Models
{
	public class MethodInfo
    {
        public string Name { get; set; }
        public string Full { get; set; }
        public List<string> Code { get; set; }
        public ClassInfo ClassInfo { get; set; }
        public List<string> NestedMethodsNames { get; set; }
        public List<MethodInfo> NestedMethods { get; set; }

		public List<UIElement> UIElements { get; set; }
		public List<DependencyLine> DependencyLines { get; set; }
		public Vector2 Position
		{
			get { return _position; }
			set
			{
				_position = value;
				foreach (var item in DependencyLines) { item.UpdateLineCoords(); } // обновить все линии зависимостей
			}
		}
		private Vector2 _position;


		public MethodInfo(string name, string full, List<string> code, ClassInfo classInfo, List<string> nestedMethodsNames, List<MethodInfo> nestedMethods)
		{
			Name = name;
			Full = full;
			Code = code;
			ClassInfo = classInfo;
			NestedMethodsNames = nestedMethodsNames;
			NestedMethods = nestedMethods;

			DependencyLines = new List<DependencyLine>();
			UIElements = new List<UIElement>();
			Position = new Vector2(0, 0);
		}
	}
}

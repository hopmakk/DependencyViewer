using System.IO;
using System.Windows;
using System.Windows.Controls;
using DependencyViewer.Models;

namespace DependencyViewer.Pages
{
	/// <summary>
	/// Логика взаимодействия для DragAndDropView.xaml
	/// </summary>
	public partial class DragAndDropPage : Page
	{
		private AppCenter _center = AppCenter.GetInstanse();

		private List<string> _filter = new List<string>()
		{
			"List",
			"FromArgb",
		};


		public DragAndDropPage()
		{
			InitializeComponent();
		}


		// drag and drop
		private void Grid_Drop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
				var filePathsCs = TakeFilesFromFolder(filePaths[0]);

				if (filePathsCs.Count != 0)
				{
					var info = ParseFilesData(filePathsCs);
					_center.DependencyViewPage.ClassesInfo = info.Item1;
					_center.DependencyViewPage.MethodsInfo = info.Item2;
					_center.DependencyViewPage.CreateDiagram();
					_center.Navigate(_center.DependencyViewPage);
				}
			}
		}


		// взять все cs файлы из папки
		private List<string> TakeFilesFromFolder(string path)
		{
			var nestedFiles = new List<string>();

			if (Directory.Exists(path))
			{
				nestedFiles = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories).ToList();
			}

			return nestedFiles;
		}


		private Tuple< List<ClassInfo>, List<MethodInfo> > ParseFilesData(List<string> filePaths)
		{
			var filesData = new Dictionary<string, List<string>>(); // путь + код

			// соотносим путь файла с его содержимым (кодом)
			foreach (string path in filePaths)
			{
				var fileData = LoadFile(path);
				filesData.Add(path, fileData);
			}

			var classesInfo = new List<ClassInfo>();
			var methodsInfo = new List<MethodInfo>();

			// распарсим содержимое всех файлов (код) на классы
			foreach (var data in filesData)
			{
				// пройдемся по каждой строке в файле
				for (int i = 0; i < data.Value.Count; i++)
				{
					var str = data.Value[i];
					var findingStr = " class ";
					var indexOfClassInit = str.IndexOf(findingStr);

					// если нашли класс - делаем инфу о нем
					if (indexOfClassInit != -1)
					{
						// позиция имени
						var nameIndex = indexOfClassInit + findingStr.Length;

						// если в конце строки нет пробела - ставим
						if (str.IndexOf(' ', nameIndex) == -1)
							str += ' ';

						// длинна имени класса
						var nameLength = str.IndexOf(' ', nameIndex) - nameIndex;

						var classInfo = new ClassInfo(
							str.Substring(nameIndex, nameLength),
							GetShortPath(data.Key),
							data.Key,
							data.Value,
							new List<MethodInfo>()
							);

						// найдем все методы класса
						for (int j = i; j < data.Value.Count; j++)
						{
							// забираем название метода (если метод есть в этой строке)
							var methodName = GetMethodNameIfExistInStr(data.Value[j]);

							// если метод есть
							if (methodName != "" &&
								(data.Value[j].Contains("public") || data.Value[j].Contains("private") || data.Value[j].Contains("protected")))
							{
								// находим его начало и конец в коде
								var methodStartIndex = j;
								var methodEndIndex = FindDownBoundIndexOfMethod(data.Value, methodStartIndex);

								if (methodEndIndex == -1)
									continue;
								
								// забираем код метода
								var methodCode = new List<string>();
								for (int strIndex = methodStartIndex; strIndex < methodEndIndex + 1; strIndex++)
								{
									methodCode.Add(data.Value[strIndex]);
								}

								// распаршиваем его в информацию о методе
								var methodInfo = GetMethodInfo(methodCode, methodName, classInfo);

								// Сохраняем
								classInfo.Methods.Add(methodInfo);
								methodsInfo.Add(methodInfo);

								// пропускаем проверку тела метода
								j = methodEndIndex;
							}
						}

						classesInfo.Add(classInfo);
						break; // переходим к след файлу (предполагается: один файл = один класс)
					}
				}
			}

			// Теперь, когда мы нашли все методы во всех классах, свяжем методы друг с другом (заполним NestedMethods)
			FindDependencies(methodsInfo);

			return new Tuple<List<ClassInfo>, List<MethodInfo>>(classesInfo, methodsInfo);
		}


		private List<string> LoadFile(string path)
		{
			return new List<string>(File.ReadAllLines(path));
		}


		private string GetShortPath(string path)
		{
			int index = 0;
			for (int i = path.Length - 1; i > 0; i--)
			{
				if (path[i] == '/' || path[i] == '\\')
				{
					index = i + 1;
					break;
				}
			}
			return path.Substring(index);
		}


		private string GetMethodNameIfExistInStr(string str)
		{
			var roundBracketsIndex = str.IndexOf('(');
			var nameIndex = -1;

			if (roundBracketsIndex == -1)
				return "";

			// находим индекс первого символа в названии метода
			for (int i = roundBracketsIndex; i >= 0; i--)
			{
				if (str[i] == ' ' || str[i] == '\t')
				{
					nameIndex = i;
					break;
				}
			}

			if (nameIndex == -1)
				return "";

			var methodName = str.Substring(nameIndex + 1, roundBracketsIndex - nameIndex - 1);

			// фильтрация
			var filtered = true;
			foreach (var filterName in _filter)
			{
				if (methodName.Contains(filterName))
				{
					filtered = false;
					break;
				}
			}

			// если имя найденного метода не пустое и проходит по фильтру
			if (methodName != "" && filtered)
				return methodName;

			return "";
		}


		private int FindDownBoundIndexOfMethod(List<string> file, int startIndex)
		{
			// проверка на (например) "private AppCenter _center = AppCenter.GetInstanse();"
			if (file[startIndex].Contains('=') && !file[startIndex].Contains("=="))
				return startIndex;

			// проверка на (например) "private void Test() { }"
			if (file[startIndex].Contains('{') && file[startIndex].Contains('}'))
				return startIndex;

			var bracesCount = 0;

			// найдем индекс закрывающей метод фигурной скобки
			for (int i = startIndex + 1; i < file.Count; i++)
			{
				if (file[i].Contains('{'))
					bracesCount++;
				
				if (file[i].Contains('}'))
					bracesCount--;

				if (bracesCount == 0) return i;
			}
			return -1;
		}


		private MethodInfo GetMethodInfo(List<string> methodCode, string methodName, ClassInfo classInfo)
		{
			var methodInfo = new MethodInfo(
				methodName,
				methodCode[0],
				methodCode,
				classInfo,
				new List<string>(),
				new List<MethodInfo>()
				);

			// обнулим первую строку, чтобы не считать за метод свой же метод
			methodCode[0] = "";

			foreach (var str in methodCode)
			{
				var nestedMethodName = GetMethodNameIfExistInStr(str);

				// если имя найденного вложенного метода не пустое
				if (nestedMethodName != "")
				{
					// добавляем его
					methodInfo.NestedMethodsNames.Add(nestedMethodName);
				}
			}

			return methodInfo;
		}


		private void FindDependencies(List<MethodInfo> methodsInfo)
		{
			// Проверяем каждый метод
			foreach (var methodInfo in methodsInfo)
			{
				// берем имя вызываемого в нем метода
				foreach (var nestedMethodName in methodInfo.NestedMethodsNames)
				{
					// и (снова) проходимся по всем возможным методам, сравнивая имена
					foreach (var item in methodsInfo)
					{
						// если имя совпадает
						if (item.Name == nestedMethodName)
						{
							// добавлем информацию о методе
							methodInfo.NestedMethods.Add(item);
							break;
						}
					}
				}
			}
		}
	}
}

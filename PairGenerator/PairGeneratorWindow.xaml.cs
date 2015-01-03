using System;
using System.IO;
using System.Windows;

namespace PairGenerator
{
	/// <summary>
	/// Логика взаимодействия для PairGeneratorWindow.xaml
	/// </summary>
	public partial class PairGeneratorWindow : Window
	{
		public PairGeneratorWindow()
		{
			InitializeComponent();
		}

		private void GeneratePairs(object sender, RoutedEventArgs e)
		{
			var min = (int) Minimum.Value.GetValueOrDefault();
			var max = (int)Maximum.Value.GetValueOrDefault();
			var com = (int)Components.Value.GetValueOrDefault();
			var prs = (int)Pairs.Value.GetValueOrDefault();
			var fil = (int)FileName.Value.GetValueOrDefault();

			FileName.Value++;
			var dirout = new DirectoryInfo(OutPath.Text);
			if (!dirout.Exists)
			{
				dirout.Create();
			}
			using (var stream = File.Create(dirout.FullName+"\\"+fil.ToString("D10")))//Создаём файл с порядковым номером для записи пар
			{
				var siz = 8;
				//var buf = new byte[siz];
				var rnd = new Random((int)DateTime.Now.Ticks);
				for (int i = 0; i < prs; i++) //Количество пар
				{
					var p1 = rnd.Next(min, max);
					var p2 = rnd.Next(min, max);
					stream.Write(BitConverter.GetBytes(p1), 0, 4);
					stream.Write(BitConverter.GetBytes(p2), 0, 4);
				}
			}
		}
	}
}

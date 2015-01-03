using System;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace IndexCreator
{
	/// <summary>
	/// Логика взаимодействия для IndexCreatorWindow.xaml
	/// </summary>
	public partial class IndexCreatorWindow : Window
	{
		public IndexCreatorWindow()
		{
			InitializeComponent();
		}

		static DirectoryInfo dirin;
		static DirectoryInfo dirout;

		private void Isolate_Click(object sender, RoutedEventArgs e)
		{
			dirin = new DirectoryInfo(InPath.Text);
			if (!dirin.Exists)
			{
				dirin.Create();
			}
			dirout = new DirectoryInfo(OutPath.Text);
			if (!dirout.Exists)
			{
				dirout.Create();
			}
			var wor = new BackgroundWorker() { WorkerReportsProgress = true };
			wor.ProgressChanged += (o, args) =>
			{
				Status.Content = args.UserState.ToString() + " обработано " + args.ProgressPercentage + " пар";
			};
			wor.DoWork += WorOnDoWork;

			wor.RunWorkerAsync();
		}

		private void WorOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs)
		{
			short icnt = 0;
			var pcnt = 0;
			var maxfilesize = 80000000;
			var buf = new byte[8];
			var data = new MemoryStream();
			var index = new MemoryStream();
			foreach (FileInfo file in dirin.EnumerateFiles())
			{
				using (var stream = file.OpenRead())
				{
					try
					{
						while (true)
						{
							if (8 != stream.Read(buf, 0, 8))
							{
								break;
							}
							//index.Write(BitConverter.GetBytes((short)dcnt), 0, 2);
							//index.Write(BitConverter.GetBytes((short)1), 0, 2);
							index.Write(BitConverter.GetBytes((int)data.Position), 0, 4);
							index.Write(buf, 0, 4);
							data.Write(buf, 4, 4);

							//index.Write(BitConverter.GetBytes((short)dcnt), 0, 2);
							//index.Write(BitConverter.GetBytes((short)1), 0, 2);
							index.Write(BitConverter.GetBytes((int)data.Position), 0, 4);
							index.Write(buf, 4, 4);
							data.Write(buf, 0, 4);

							if (index.Length >= maxfilesize)
							{
								using (var indstr = File.Create(dirout.FullName + "\\index-" + icnt.ToString("D5")))
								{
									index.Position = 0;
									index.CopyTo(indstr);
									index.Close();
								}
								using (var datstr = File.Create(dirout.FullName + "\\datas-" + icnt.ToString("D5")))
								{
									data.Position = 0;
									data.CopyTo(datstr);
									data.Close();
								}
								index = new MemoryStream();
								data = new MemoryStream();
								icnt++;
							}
							++pcnt;
							if (pcnt % 10000 == 0)
							{
								((BackgroundWorker)sender).ReportProgress(pcnt, (string)(file.Name));
							}
						}
					}
					catch (Exception e)
					{

					}
				}

			}
			if (data.Length > 0)
			{
				using (var datstr = File.Create(dirout.FullName + "\\datas-" + icnt.ToString("D5")))
				{
					data.Position = 0;
					data.CopyTo(datstr);
					data.Close();
				}
			} if (index.Length > 0)
			{
				using (var indstr = File.Create(dirout.FullName + "\\index-" + icnt.ToString("D5")))
				{
					index.Position = 0;
					index.CopyTo(indstr);
					index.Close();
				}
			}
			((BackgroundWorker)sender).ReportProgress(pcnt, "Всего");
		}
	}
}

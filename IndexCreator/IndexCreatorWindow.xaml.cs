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
		static short icnt;
		static long maxfilesize;
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
				Status.Content = args.UserState.ToString();
			};
			wor.DoWork += WorOnDoWork;
			icnt = (short)FileName.Value.GetValueOrDefault();
			maxfilesize = MaxFileSize.Value.GetValueOrDefault();
			wor.RunWorkerAsync();
		}

		private void WorOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs)
		{
			
			long pcnt = 0;
			var buf = new byte[8];
			var datas = new MemoryStream((int) maxfilesize);
			var index = new MemoryStream((int) maxfilesize);
			var start = DateTime.Now;
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
							//index.Write(BitConverter.GetBytes((int)datas.Position), 0, 4);
							//index.Write(buf, 0, 4);
							//datas.Write(buf, 4, 4);

							//index.Write(BitConverter.GetBytes((int)datas.Position), 0, 4);
							//index.Write(buf, 4, 4);
							//datas.Write(buf, 0, 4);
							
							index.Write(buf, 0, 8);
							datas.Write(buf, 4, 4);
							datas.Write(buf, 0, 4);


							if (index.Length >= maxfilesize)
							{
								using (var indstr = File.Create(dirout.FullName + "\\index-" + icnt.ToString("D5")))
								{
									index.Position = 0;
									index.CopyTo(indstr);
									//index.Close();
								}
								using (var datstr = File.Create(dirout.FullName + "\\datas-" + icnt.ToString("D5")))
								{
									datas.Position = 0;
									datas.CopyTo(datstr);
									//datas.Close();
								}
								index.Position = 0;
								index.SetLength(0);
								datas.Position = 0;
								datas.SetLength(0);
								//index = new MemoryStream();
								//datas = new MemoryStream();
								icnt++;
							}
							++pcnt;
							if (pcnt % 10000 == 0)
							{
								((BackgroundWorker)sender).ReportProgress(0, file.Name + " обработано " + pcnt + " пар");
							}
						}
					}
					catch (Exception e)
					{

					}
				}

			}
			if (datas.Length > 0)
			{
				using (var datstr = File.Create(dirout.FullName + "\\datas-" + icnt.ToString("D5")))
				{
					datas.Position = 0;
					datas.CopyTo(datstr);
					datas.Close();
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
			((BackgroundWorker)sender).ReportProgress(0, pcnt + " индексов за " + (DateTime.Now - start).TotalSeconds.ToString("F2") + " секунд");
		}
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace IndexSorter
{
	/// <summary>
	/// Логика взаимодействия для IndexSorterWindow.xaml
	/// </summary>
	public partial class IndexSorterWindow : Window
	{
		public IndexSorterWindow()
		{
			InitializeComponent();
		}

		static DirectoryInfo dirin;
		static DirectoryInfo dirout;

		private void Sort_Click(object sender, RoutedEventArgs e)
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

			wor.RunWorkerAsync();
		}

		private void WorOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs)
		{
			long pcnt = 0;
			var buf = new byte[4];
			var sort = new SortedSet<long>();
			var start = DateTime.Now;
			long lon;
			foreach (FileInfo file in dirin.EnumerateFiles("index-*"))
			{
				try
				{
					using (var stream = file.OpenRead())
					{
						while (true)
						{
							lon = stream.Position;
							if (4 != stream.Read(buf, 0, 4))
							{
								break;
							}
							lon += (long)(BitConverter.ToInt32(buf, 0)) << 32;
							sort.Add(lon);
							++pcnt;
							if (pcnt % 100000 == 0)
							{
								((BackgroundWorker)sender).ReportProgress(0, file.Name + " обработано " + pcnt + " пар");
							}
						}
					}

					var datfile = new FileInfo(file.FullName.Replace("index-", "datas-"));

					var datbuf = File.ReadAllBytes(datfile.FullName);

					using (var datas = new MemoryStream())
					{
						using (var index = new MemoryStream())
						{
							short len = 0;
							int last = int.MinValue;
							int lastpos = 0;
							foreach (long l in sort)
							{
								buf = BitConverter.GetBytes(l);
								var pos = BitConverter.ToInt32(buf, 0);
								var key = BitConverter.ToInt32(buf, 4);
								if (last != key)
								{
									if (len != 0)
									{
										index.Write(BitConverter.GetBytes(last), 0, 4);
										index.Write(BitConverter.GetBytes(lastpos), 0, 4);
										index.Write(BitConverter.GetBytes(len), 0, 2);
									}
									last = key;
									lastpos = (int)datas.Position;
									len = 1;
								}
								else
								{
									len++;
								}
								datas.Write(datbuf, pos, 4);
								++pcnt;
								if (pcnt % 100000 == 0)
								{
									((BackgroundWorker)sender).ReportProgress(0, file.Name + " обработано " + pcnt + " пар");
								}
							}

							if (len != 0)
							{
								index.Write(BitConverter.GetBytes(last), 0, 4);
								index.Write(BitConverter.GetBytes(lastpos), 0, 4);
								index.Write(BitConverter.GetBytes(len), 0, 2);
							}

							using (var str = File.Create(dirout.FullName + "\\" + file.Name))
							{
								index.Position = 0;
								index.CopyTo(str);
							}
						}
						using (var datstr = File.Create(dirout.FullName + "\\" + datfile.Name))
						{
							datas.Position = 0;
							datas.CopyTo(datstr);
						}
					}
					sort.Clear();
				}
				catch (Exception e)
				{
				}
			}
			((BackgroundWorker)sender).ReportProgress(0, pcnt + " индексов за " + (DateTime.Now - start).TotalSeconds.ToString("F2") + " секунд");
		}
	}
}

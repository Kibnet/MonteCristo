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

		//[StructLayout(LayoutKind.Sequential, Pack = 2)]
		//public struct Index
		//{
		//	public int Key;
		//	//public short File;
		//	//public short Length;
		//	public int Position;

		//	public void Write(Stream stream)
		//	{
		//		stream.Write(BitConverter.GetBytes(Key), 0, 4);
		//		//stream.Write(BitConverter.GetBytes(File), 0, 2);
		//		//stream.Write(BitConverter.GetBytes(Length), 0, 2);
		//		stream.Write(BitConverter.GetBytes(Position), 0, 4);
		//	}

		//	public Index(byte[] bytes)
		//	{
		//		if (bytes.Length == 12)
		//		{
		//			Key = BitConverter.ToInt32(bytes, 0);
		//			//File = BitConverter.ToInt16(bytes, 4);
		//			//Length = BitConverter.ToInt16(bytes, 6);
		//			Position = BitConverter.ToInt32(bytes, 8);
		//		}
		//		else
		//		{
		//			Key = 0;
		//			//File = 0;
		//			//Length = 0;
		//			Position = 0;
		//		}
		//	}
		//}

		private void WorOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs)
		{
			short dcnt = 0;
			short icnt = 0;
			var pcnt = 0;
			var maxfilesize = 80000064;
			var buf = new byte[8];
			var sort = new SortedSet<long>();
			foreach (FileInfo file in dirin.EnumerateFiles("index-*"))
			{
				try
				{
					using (var stream = new MemoryStream(File.ReadAllBytes(file.FullName)))
					{
						while (true)
						{
							if (8 != stream.Read(buf, 0, 8))
							{
								break;
							}
							var lon = BitConverter.ToInt64(buf, 0);
							sort.Add(lon);
							++pcnt;
							if (pcnt % 100000 == 0)
							{
								((BackgroundWorker)sender).ReportProgress(0, (string)(file.Name + " " + pcnt + " индексов"));
							}
						}


					}

					var datfile = new FileInfo(file.FullName.Replace("index-", "datas-"));

					var datbuf = File.ReadAllBytes(datfile.FullName);

					using (var datstream = new MemoryStream())
					{

						using (var stream = new MemoryStream())
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
										stream.Write(BitConverter.GetBytes(last), 0, 4);
										stream.Write(BitConverter.GetBytes(lastpos), 0, 4);
										stream.Write(BitConverter.GetBytes(len), 0, 2);
									}
									last = key;
									lastpos = (int)datstream.Position;
									len = 1;
								}
								else
								{
									len++;
								}
								datstream.Write(datbuf, pos, 4);
								++pcnt;
								if (pcnt % 100000 == 0)
								{
									((BackgroundWorker)sender).ReportProgress(0, (string)(file.Name + " " + pcnt + " индексов"));
								}


							}

							if (len != 0)
							{
								stream.Write(BitConverter.GetBytes(last), 0, 4);
								stream.Write(BitConverter.GetBytes(lastpos), 0, 4);
								stream.Write(BitConverter.GetBytes(len), 0, 2);
							}

							using (var str = File.Create(dirout.FullName + "\\" + file.Name))
							{
								stream.Position = 0;
								stream.CopyTo(str);
							}

						}
						using (var datstr = File.Create(dirout.FullName + "\\" + datfile.Name))
						{
							datstream.Position = 0;
							datstream.CopyTo(datstr);
						}
					}
					sort.Clear();
				}
				catch (Exception e)
				{

				}
			}
			((BackgroundWorker)sender).ReportProgress(pcnt, "Всего");
		}
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security;
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

		[SecuritySafeCritical]
		public static unsafe void GetBytes(int value, ref byte[] numArray, int position = 0)
		{
			fixed (byte* numPtr = numArray)
				*(int*)(numPtr+position) = value;
		}

		public static unsafe void GetBytes(long value, ref byte[] numArray)
		{
			fixed (byte* numPtr = numArray)
				*(long*)numPtr = value;
		}
		
		static DirectoryInfo dirin;
		static DirectoryInfo dirout;
		static short icnt;
		static long maxfilesize;

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

			icnt = (short)FileName.Value.GetValueOrDefault();
			maxfilesize = MaxFileSize.Value.GetValueOrDefault();
			wor.RunWorkerAsync();
		}

		private void WorOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs)
		{
			long pcnt = 0;
			long dcnt = 0;
			short len;
			int last;
			int lastpos;
			var buf = new byte[12];
			var list = new List<long>((int)maxfilesize / 4);
			var datas = new MemoryStream((int)maxfilesize);
			var index = new MemoryStream((int)maxfilesize);
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
							Array.Copy(buf, 0, buf, 8, 4);
							list.Add(BitConverter.ToInt64(buf, 0));
							list.Add(BitConverter.ToInt64(buf, 4));
							dcnt += 2;
							++pcnt;
							if (pcnt % 100000 == 0)
							{
								((BackgroundWorker)sender).ReportProgress(0, file.Name + " загружено " + pcnt.ToString("N0") + " пар");
							}

							
							if (dcnt * 4 >= maxfilesize)
							{
								((BackgroundWorker)sender).ReportProgress(0, file.Name + " сортировка " + dcnt.ToString("N0") + " пар");
								list.Sort();
								len = 0;
								last = int.MinValue;
								lastpos = 0;
								dcnt = 0;
								foreach (long l in list)
								{
									GetBytes(l, ref buf);
									var dat = BitConverter.ToInt32(buf, 0);
									var key = BitConverter.ToInt32(buf, 4);
									if (last != key)
									{
										if (len != 0)
										{
											GetBytes(last, ref buf);
											GetBytes(lastpos, ref buf, 4);
											GetBytes(len, ref buf, 8);
											index.Write(buf, 0, 10);
										}
										last = key;
										lastpos = (int)datas.Position;
										len = 1;
									}
									else
									{
										len++;
									}
									GetBytes(dat, ref buf);
									datas.Write(buf, 0, 4);
									++dcnt;
									if (dcnt % 100000 == 0)
									{
										((BackgroundWorker)sender).ReportProgress(0, file.Name + " сохранено " + dcnt.ToString("N0") + " пар");
									}
								}

								if (len != 0)
								{
									GetBytes(last, ref buf);
									GetBytes(lastpos, ref buf, 4);
									GetBytes(len, ref buf, 8);
									index.Write(buf, 0, 10);
								}
								
								Flush(index, datas);
								index.Position = 0;
								index.SetLength(0);
								datas.Position = 0;
								datas.SetLength(0);
								icnt++;
								dcnt = 0;
								list.Clear();
							}
						}
					}
					catch (Exception e)
					{
					}
				}
			}
			Flush(index, datas);

			datas.Close();
			index.Close();

			((BackgroundWorker)sender).ReportProgress(0, pcnt.ToString("N0") + " индексов за " + (DateTime.Now - start).TotalSeconds.ToString("F2") + " секунд");
		}

		private static void Flush(MemoryStream index, MemoryStream datas)
		{
			if (datas.Length > 0)
			{
				using (var datstr = File.Create(dirout.FullName + "\\datas-" + icnt.ToString("D5")))
				{
					datas.Position = 0;
					datas.CopyTo(datstr);
				}
			}
			if (index.Length > 0)
			{
				using (var indstr = File.Create(dirout.FullName + "\\index-" + icnt.ToString("D5")))
				{
					index.Position = 0;
					index.CopyTo(indstr);
				}
			}
		}
	}
}

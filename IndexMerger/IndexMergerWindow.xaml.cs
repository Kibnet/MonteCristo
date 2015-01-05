using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IndexMerger
{
	/// <summary>
	/// Логика взаимодействия для IndexMergerWindow.xaml
	/// </summary>
	public partial class IndexMergerWindow : Window
	{
		public IndexMergerWindow()
		{
			InitializeComponent();
		}

		static DirectoryInfo dirin;
		static DirectoryInfo dirout;

		private void Merge_Click(object sender, RoutedEventArgs e)
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
			short dcnt = 0;
			short icnt = 0;
			short len = 0;
			int readed = 0;
			int min;
			List<int> minlist = new List<int>();
			List<int> remlist = new List<int>();
			long pcnt = 0L;
			const int maxfilesize = 40000000;
			var buf = new byte[10];
			const int bufsize = 4096;
			var datbuf = new byte[bufsize];
			var sort = new SortedDictionary<int, Dictionary<short, short>>();
			var indexStreams = new SortedDictionary<short, Stream>();
			var datasStreams = new SortedDictionary<short, Stream>();
			var datas = new MemoryStream();
			var index = new MemoryStream();
			var start = DateTime.Now;
			try
			{
				foreach (FileInfo file in dirin.EnumerateFiles("index-*").ToArray())
				{
					try
					{
						var filenum = file.Name.Split('-')[1];
						var key = short.Parse(filenum);
						indexStreams[key] = file.OpenRead();
						datasStreams[key] = new FileInfo(file.FullName.Replace("index-", "datas-")).OpenRead();
					}
					catch (Exception e)
					{

					}
				}
				while (true)
				{
					minlist.Clear();
					foreach (var stream in indexStreams)
					{
						try
						{
							if (10 != stream.Value.Read(buf, 0, 10))
							{
								continue;
							}
							var key = BitConverter.ToInt32(buf, 0);
							minlist.Add(key);
							if (!sort.ContainsKey(key))
							{
								sort[key] = new Dictionary<short, short>();
							}
							sort[key].Add(stream.Key, BitConverter.ToInt16(buf, 8));

							++pcnt;
							if (pcnt % 100000 == 0)
							{
								((BackgroundWorker)sender).ReportProgress(0, (string)(pcnt + " индексов"));
							}
						}
						catch (Exception e)
						{
						}
					}
					if (sort.Count == 0)
					{
						break;
					}
					
					min = minlist.Count == 0 ? int.MaxValue : minlist.Min();
					remlist.Clear();
					foreach (var first in sort.TakeWhile(pair => pair.Key <= min))
					{
						remlist.Add(first.Key);
						len = 0;
						index.Write(BitConverter.GetBytes(first.Key), 0, 4);
						index.Write(BitConverter.GetBytes((int)datas.Position), 0, 4);
						foreach (var ind in first.Value)
						{
							var datstream = datasStreams[ind.Key];
							var lenbuf = ind.Value * 4;
							readed = 0;
							if (lenbuf > bufsize)
							{
								while (lenbuf - readed > bufsize)
								{
									if (datstream.Read(datbuf, 0, bufsize) == bufsize)
									{
										readed += bufsize;
										datas.Write(datbuf, 0, bufsize);
									}
									else
									{
										break;
									}
								}
								readed = lenbuf - readed;
								if (datstream.Read(datbuf, 0, readed) == readed)
								{
									datas.Write(datbuf, 0, readed);
								}
							}
							else
							{
								if (datstream.Read(datbuf, 0, lenbuf) != lenbuf)
								{
									continue;
								}
								datas.Write(datbuf, 0, lenbuf);
							}
							len += ind.Value;
							++pcnt;
							if (pcnt % 100000 == 0)
							{
								((BackgroundWorker)sender).ReportProgress(0, (string)(pcnt + " индексов"));
							}
						}
						index.Write(BitConverter.GetBytes(len), 0, 2);


						if (datas.Length >= maxfilesize)
						{
							using (var indstr = File.Create(dirout.FullName + "\\index-" + icnt.ToString("D5")))
							{
								index.Position = 0;
								index.CopyTo(indstr);
							}
							using (var datstr = File.Create(dirout.FullName + "\\datas-" + icnt.ToString("D5")))
							{
								datas.Position = 0;
								datas.CopyTo(datstr);
							}
							index.Position = 0;
							index.SetLength(0);
							datas.Position = 0;
							datas.SetLength(0);
							icnt++;
						}
						//sort.Remove(first.Key);
					}
					foreach (var i in remlist)
					{
						sort.Remove(i);
					}
				}
			}
			finally
			{
				foreach (var stream in indexStreams)
				{
					stream.Value.Close();
				}
				foreach (var stream in datasStreams)
				{
					stream.Value.Close();
				}
				if (datas.Length > 0)
				{
					using (var datstr = File.Create(dirout.FullName + "\\datas-" + icnt.ToString("D5")))
					{
						datas.Position = 0;
						datas.CopyTo(datstr);
						datas.Close();
					}
				}
				if (index.Length > 0)
				{
					using (var indstr = File.Create(dirout.FullName + "\\index-" + icnt.ToString("D5")))
					{
						index.Position = 0;
						index.CopyTo(indstr);
						index.Close();
					}
				}
			}
			((BackgroundWorker)sender).ReportProgress(0, pcnt + " индексов за " + (DateTime.Now - start).TotalSeconds.ToString("F2") + " секунд");
		}
	}
}

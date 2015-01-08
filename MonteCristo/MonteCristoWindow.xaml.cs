using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace MonteCristo
{
	/// <summary>
	/// Логика взаимодействия для MonteCristoWindow.xaml
	/// </summary>
	public partial class MonteCristoWindow : Window
	{
		public MonteCristoWindow()
		{
			InitializeComponent();
		}

		static DirectoryInfo dirin;
		static SortedDictionary<int, long> sort = new SortedDictionary<int, long>();
		static SortedDictionary<short, Stream> datasStreams = new SortedDictionary<short, Stream>();

		private void LoadIndexes(object sender, RoutedEventArgs e)
		{
			dirin = new DirectoryInfo(InPath.Text);
			if (!dirin.Exists)
			{
				dirin.Create();
			}

			sort.Clear();
			foreach (var stream in datasStreams)
			{
				try
				{
					stream.Value.Close();
				}
				catch (Exception)
				{

				}
			}
			datasStreams.Clear();

			var start = DateTime.Now;
			var buf = new byte[12];
			long indexcount = 0;
			foreach (FileInfo file in dirin.EnumerateFiles("index-*"))
			{
				try
				{
					long position = 0;
					var filekey = long.Parse(file.Name.Split('-')[1]);
					datasStreams[(short)filekey] = new FileInfo(file.FullName.Replace("index-", "datas-")).OpenRead();
					using (var stream = file.OpenRead())
					{
						while (true)
						{
							if (6 != stream.Read(buf, 0, 6))
							{
								break;
							}
							var length = BitConverter.ToInt16(buf, 4);
							sort[BitConverter.ToInt32(buf, 0)] = (filekey << 48) + ((long)length << 32) + (position);
							position += length*4;
							indexcount++;
						}
					}
				}
				catch (Exception)
				{

				}
			}
			Status.Content = indexcount + " индексов за " + (DateTime.Now - start).TotalSeconds.ToString("F2") + " секунд";
		}

		private void FindVertex(object sender, RoutedEventArgs e)
		{
			GraphView.Items.Clear();
			added.Clear();
			var start = DateTime.Now;
			int root = (int)VertexBox.Value.GetValueOrDefault();
			int depth = (int)DepthBox.Value.GetValueOrDefault();
			GraphView.Items.Add(RecursiveFind(root, depth));
			Status.Content = added.Count + " индексов за " + (DateTime.Now - start).TotalSeconds.ToString("F2") + " секунд";
		}

		SortedDictionary<int, Vertex> added = new SortedDictionary<int, Vertex>();

		private Vertex RecursiveFind(int root, int depth)
		{
			if (added.ContainsKey(root))
			{
				return added[root];
			}
			
			var vert = new Vertex() { ID = root };
			added[root] = vert;
			if (depth > 0)
			{
				var info = sort[root];
				var buf = BitConverter.GetBytes(info);
				var pos = BitConverter.ToInt32(buf, 0);
				var len = BitConverter.ToInt16(buf, 4);
				var fil = BitConverter.ToInt16(buf, 6);
				var stream = datasStreams[fil];
				stream.Position = pos;
				var tmp = new byte[len * 4];
				var readed = stream.Read(tmp, 0, tmp.Length);
				if (readed == tmp.Length)
				{
					for (int i = 0; i < len; i++)
					{
						
						vert.Items.Add(RecursiveFind(BitConverter.ToInt32(tmp, i * 4), depth - 1));
					}
				}

			}
			return vert;
		}

	}
	public class Vertex
	{
		public int ID { get; set; }
		public ObservableCollection<Vertex> Items { get; set; }

		public Vertex()
		{
			Items = new ObservableCollection<Vertex>();
		}
	}
}

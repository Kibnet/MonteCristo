using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using GraphX;
using GraphX.Controls;
using GraphX.GraphSharp.Algorithms.Layout.Simple.FDP;
using GraphX.GraphSharp.Algorithms.OverlapRemoval;
using GraphX.Logic;
using QuickGraph;
using YAXLib;

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
			ZoomControl.SetViewFinderVisibility(zoomctrl, Visibility.Visible);
			zoomctrl.ZoomToFill();
			var LogicCore = new LogicCoreExample();
			LogicCore.Graph = new GraphExample();
			Area.LogicCore = LogicCore;
			//LogicCore.DefaultOverlapRemovalAlgorithm = GraphX.OverlapRemovalAlgorithmTypeEnum.FSA;
			//LogicCore.DefaultOverlapRemovalAlgorithmParams = LogicCore.AlgorithmFactory.CreateOverlapRemovalParameters(GraphX.OverlapRemovalAlgorithmTypeEnum.FSA);
			//((OverlapRemovalParameters)LogicCore.DefaultOverlapRemovalAlgorithmParams).HorizontalGap = 50;
			//((OverlapRemovalParameters)LogicCore.DefaultOverlapRemovalAlgorithmParams).VerticalGap = 50;
			//GraphAreaExample_Setup();
			GeneralGraph_Constructor();
		}

		private void GraphAreaExample_Setup()
		{
			var LogicCore = new LogicCoreExample();
			LogicCore.Graph = new GraphExample();
			LogicCore.DefaultLayoutAlgorithm = GraphX.LayoutAlgorithmTypeEnum.KK;
			LogicCore.DefaultLayoutAlgorithmParams = LogicCore.AlgorithmFactory.CreateLayoutParameters(GraphX.LayoutAlgorithmTypeEnum.KK);
			((KKLayoutParameters)LogicCore.DefaultLayoutAlgorithmParams).MaxIterations = 1000;
			LogicCore.DefaultOverlapRemovalAlgorithm = GraphX.OverlapRemovalAlgorithmTypeEnum.FSA;
			LogicCore.DefaultOverlapRemovalAlgorithmParams = LogicCore.AlgorithmFactory.CreateOverlapRemovalParameters(GraphX.OverlapRemovalAlgorithmTypeEnum.FSA);
			((OverlapRemovalParameters)LogicCore.DefaultOverlapRemovalAlgorithmParams).HorizontalGap = 50;
			((OverlapRemovalParameters)LogicCore.DefaultOverlapRemovalAlgorithmParams).VerticalGap = 50;
			LogicCore.DefaultEdgeRoutingAlgorithm = GraphX.EdgeRoutingAlgorithmTypeEnum.SimpleER;
			LogicCore.AsyncAlgorithmCompute = false;
			Area.LogicCore = LogicCore;// as IGXLogicCore<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>>;
		}

		static DirectoryInfo dirin;
		static Dictionary<int, long> sort = new Dictionary<int, long>();
		static Dictionary<short, Stream> datasStreams = new Dictionary<short, Stream>();

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
							position += length * 4;
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

		private int maxvert = 1000;
		private void FindVertex(object sender, RoutedEventArgs e)
		{
			//GraphView.Items.Clear();
			added.Clear();
			Edges.Clear();
			Area.LogicCore.Graph.Clear();
			var start = DateTime.Now;
			//var dataGraph = new GraphExample();
			int root = (int)VertexBox.Value.GetValueOrDefault();
			int depth = (int)DepthBox.Value.GetValueOrDefault();
			maxvert = (int)MaxVertBox.Value.GetValueOrDefault();
			RecursiveFind(Area.LogicCore.Graph, root, depth);
			Status.Content = added.Count + " индексов за " + (DateTime.Now - start).TotalSeconds.ToString("F2") + " секунд";

			Area.GetLogicCore<LogicCoreExample>().DefaultEdgeRoutingAlgorithm = (EdgeRoutingAlgorithmTypeEnum)erg_eralgo.SelectedItem;
			switch ((EdgeRoutingAlgorithmTypeEnum)erg_eralgo.SelectedItem)
			{
				case EdgeRoutingAlgorithmTypeEnum.SimpleER:
					Area.GetLogicCore<LogicCoreExample>().DefaultEdgeRoutingAlgorithmParams = erg_SimpleERParameters;
					break;
				case EdgeRoutingAlgorithmTypeEnum.PathFinder:
					Area.GetLogicCore<LogicCoreExample>().DefaultEdgeRoutingAlgorithmParams = erg_PFERParameters;
					// erg_Area.SideExpansionSize = new Size(erg_PFERParameters.SideGridOffset, erg_PFERParameters.SideGridOffset);
					break;
			}
			//Area.LogicCore.Graph = dataGraph;
			Area.GenerateGraph(true, true);

			Area.SetVerticesDrag(true, true);

			Area.SetEdgesDashStyle(GraphX.EdgeDashStyle.Solid);
			Area.ShowAllEdgesArrows(true);
			Area.ShowAllEdgesLabels(true);
			zoomctrl.ZoomToFill();
		}

		Dictionary<int, DataVertex> added = new Dictionary<int, DataVertex>();
		SortedSet<long> Edges = new SortedSet<long>(); 

		private DataVertex RecursiveFind(BidirectionalGraph<DataVertex, DataEdge> graph, int root, int depth)
		{
			if (added.ContainsKey(root))
			{
				return added[root];
			}
			var thisVert = new DataVertex(root.ToString("N0")) { ID = root,Depth = depth};
			graph.AddVertex(thisVert);
			added[root] = thisVert;
			if (depth > 0 && added.Count < maxvert)
			{
				if (sort.ContainsKey(root))
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
							var destVert = BitConverter.ToInt32(tmp, i * 4);
							long edge;
							if (root>destVert)
							{
								edge = root + (long)destVert << 32;
							}
							else
							{
								edge = destVert + (long)root << 32;
							}
							if (Edges.Add(edge))
							{
								var dataEdge = new DataEdge(thisVert, RecursiveFind(graph, destVert, depth - 1))
								{
									Text = string.Format("{0} -> {1}", root, destVert),
								};
								graph.AddEdge(dataEdge);
							}
						}
					}
				}
			}
			return thisVert;
		}
	}

	public class LogicCoreExample : GXLogicCore<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>> { }

	public class GraphAreaExample : GraphArea<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>> { }

	public class GraphExample : BidirectionalGraph<DataVertex, DataEdge> { }

	public class DataVertex : VertexBase
	{
		/// <summary>
		/// Some string property for example purposes
		/// </summary>
		public string Text { get; set; }

		public int Depth { get; set; }

		#region Calculated or static props
		[YAXDontSerialize]
		public DataVertex Self
		{
			get { return this; }
		}

		public override string ToString()
		{
			return Text;
		}

		#endregion

		/// <summary>
		/// Default parameterless constructor for this class
		/// (required for YAXLib serialization)
		/// </summary>
		public DataVertex()
			: this("")
		{
		}

		public DataVertex(string text = "")
		{
			Text = text;
		}
	}

	public class DataEdge : EdgeBase<DataVertex>
	{
		/// <summary>
		/// Default constructor. We need to set at least Source and Target properties of the edge.
		/// </summary>
		/// <param name="source">Source vertex data</param>
		/// <param name="target">Target vertex data</param>
		/// <param name="weight">Optional edge weight</param>
		public DataEdge(DataVertex source, DataVertex target, double weight = 1)
			: base(source, target, weight)
		{
		}
		/// <summary>
		/// Default parameterless constructor (for serialization compatibility)
		/// </summary>
		public DataEdge()
			: base(null, null, 1)
		{
		}

		/// <summary>
		/// Custom string property for example
		/// </summary>
		public string Text { get; set; }

		#region GET members
		public override string ToString()
		{
			return Text;
		}

		[YAXDontSerialize]
		public DataEdge Self
		{
			get { return this; }
		}
		#endregion
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using GraphX;
using GraphX.Controls;
using GraphX.GraphSharp.Algorithms.EdgeRouting;
using GraphX.GraphSharpComponents.EdgeRouting;
using Microsoft.Win32;
using QuickGraph;

namespace MonteCristo
{
	public partial class MonteCristoWindow
	{
		private void GeneralGraph_Constructor()
		{
			//var gg_Logic = new LogicCoreExample();
			//Area.LogicCore = gg_Logic;

			//Area.DefaulOverlapRemovalAlgorithm
			gg_layalgo.ItemsSource = Enum.GetValues(typeof(LayoutAlgorithmTypeEnum)).Cast<LayoutAlgorithmTypeEnum>();
			gg_layalgo.SelectedIndex = 0;
			gg_oralgo.ItemsSource = Enum.GetValues(typeof(OverlapRemovalAlgorithmTypeEnum)).Cast<OverlapRemovalAlgorithmTypeEnum>();
			gg_oralgo.SelectedIndex = 0;
			gg_eralgo.ItemsSource = Enum.GetValues(typeof(EdgeRoutingAlgorithmTypeEnum)).Cast<EdgeRoutingAlgorithmTypeEnum>();
			gg_eralgo.SelectedIndex = 2;
			//gg_but_randomgraph.Click += gg_but_randomgraph_Click;
			gg_vertexCount.Text = "30";
			gg_async.Checked += gg_async_Checked;
			gg_async.Unchecked += gg_async_Checked;
			Area.RelayoutFinished += AreaRelayoutFinished;
			Area.GenerateGraphFinished += AreaGenerateGraphFinished;
			//gg_Logic.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None;






			Area.LogicCore.ParallelEdgeDistance = 20;

			erg_BundleEdgeRoutingParameters = (BundleEdgeRoutingParameters)Area.LogicCore.AlgorithmFactory.CreateEdgeRoutingParameters(EdgeRoutingAlgorithmTypeEnum.Bundling);
			erg_SimpleERParameters = (SimpleERParameters)Area.LogicCore.AlgorithmFactory.CreateEdgeRoutingParameters(EdgeRoutingAlgorithmTypeEnum.SimpleER);
			erg_PFERParameters = (PathFinderEdgeRoutingParameters)Area.LogicCore.AlgorithmFactory.CreateEdgeRoutingParameters(EdgeRoutingAlgorithmTypeEnum.PathFinder);

			erg_useExternalERAlgo.Checked += erg_useExternalERAlgo_Checked;
			erg_useExternalERAlgo.Unchecked += erg_useExternalERAlgo_Checked;
			erg_eralgo.ItemsSource = Enum.GetValues(typeof(EdgeRoutingAlgorithmTypeEnum)).Cast<EdgeRoutingAlgorithmTypeEnum>();
			erg_eralgo.SelectedIndex = 0;
			erg_eralgo.SelectionChanged += erg_eralgo_SelectionChanged;
			erg_recalculate.Checked += erg_recalculate_Checked;
			erg_recalculate.Unchecked += erg_recalculate_Checked;
			erg_useCurves.Checked += erg_useCurves_Checked;
			erg_useCurves.Unchecked += erg_useCurves_Checked;






			ZoomControl.SetViewFinderVisibility(zoomctrl, System.Windows.Visibility.Visible);

			zoomctrl.IsAnimationDisabled = false;
			zoomctrl.MaxZoomDelta = 2;

			this.Loaded += GG_Loaded;
		}

		void GG_Loaded(object sender, RoutedEventArgs e)
		{
			GG_RegisterCommands();
		}

		#region Commands

		#region GGRelayoutCommand

		private bool GGRelayoutCommandCanExecute(object sender)
		{
			return true;
		}

		private void GGRelayoutCommandExecute(object sender)
		{
			if (Area.LogicCore.AsyncAlgorithmCompute)
				gg_loader.Visibility = System.Windows.Visibility.Visible;
			Area.RelayoutGraph(true);
		}
		#endregion

		#region SaveStateCommand
		public static RoutedCommand SaveStateCommand = new RoutedCommand();
		private void SaveStateCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = Area.LogicCore.Graph != null && Area.VertexList.Count > 0;
		}

		private void SaveStateCommandExecute(object sender, ExecutedRoutedEventArgs e)
		{
			if (Area.StateStorage.ContainsState("exampleState"))
				Area.StateStorage.RemoveState("exampleState");
			Area.StateStorage.SaveState("exampleState", "My example state");
		}
		#endregion

		#region LoadStateCommand
		public static RoutedCommand LoadStateCommand = new RoutedCommand();
		private void LoadStateCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = Area.StateStorage.ContainsState("exampleState");
		}

		private void LoadStateCommandExecute(object sender, ExecutedRoutedEventArgs e)
		{
			if (Area.StateStorage.ContainsState("exampleState"))
				Area.StateStorage.LoadState("exampleState");
		}
		#endregion

		#region SaveLayoutCommand
		public static RoutedCommand SaveLayoutCommand = new RoutedCommand();
		private void SaveLayoutCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = Area.LogicCore.Graph != null && Area.VertexList.Count > 0;
		}

		private void SaveLayoutCommandExecute(object sender, ExecutedRoutedEventArgs e)
		{
			var dlg = new SaveFileDialog() { Filter = "All files|*.*", Title = "Select layout file name", FileName = "laytest.xml" };
			if (dlg.ShowDialog() == true)
			{
				Area.SaveIntoFile(dlg.FileName);
			}
		}
		#endregion

		#region LoadLayoutCommand

		#region Save / load visual graph example

		/// <summary>
		/// Temporary storage for example vertex data objects used on save/load mechanics
		/// </summary>
		private Dictionary<int, DataVertex> exampleVertexStorage = new Dictionary<int, DataVertex>();
		//public DataVertex gg_getVertex(int id)
		//{
		//	var item = DataSource.FirstOrDefault(a => a.ID == id);
		//	if (item == null) item = new Models.DataItem() { ID = id, Text = id.ToString() };
		//	exampleVertexStorage.Add(id, new DataVertex() { ID = item.ID, Text = item.Text, DataImage = new BitmapImage(new Uri(@"pack://application:,,,/GraphX.Controls;component/Images/help_black.png", UriKind.Absolute)) { CacheOption = BitmapCacheOption.OnLoad } });
		//	return exampleVertexStorage.Last().Value;
		//}

		//public DataEdge gg_getEdge(int ids, int idt)
		//{
		//	return new DataEdge(exampleVertexStorage.ContainsKey(ids) ? exampleVertexStorage[ids] : null,
		//		exampleVertexStorage.ContainsKey(idt) ? exampleVertexStorage[idt] : null);
		//}

		#endregion

		public static RoutedCommand LoadLayoutCommand = new RoutedCommand();
		private void LoadLayoutCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void LoadLayoutCommandExecute(object sender, ExecutedRoutedEventArgs e)
		{
			var dlg = new OpenFileDialog() { Filter = "All files|*.*", Title = "Select layout file", FileName = "laytest.xml" };
			if (dlg.ShowDialog() == true)
			{
				exampleVertexStorage.Clear();
				try
				{
					Area.LoadFromFile(dlg.FileName);
					Area.SetVerticesDrag(true);
					Area.UpdateAllEdges();
				}
				catch (Exception ex)
				{
					MessageBox.Show(string.Format("Failed to load layout file:\n {0}", ex.ToString()));
				}
			}
		}
		#endregion

		void GG_RegisterCommands()
		{
			CommandBindings.Add(new CommandBinding(SaveStateCommand, SaveStateCommandExecute, SaveStateCommandCanExecute));
			gg_saveState.Command = SaveStateCommand;
			CommandBindings.Add(new CommandBinding(LoadStateCommand, LoadStateCommandExecute, LoadStateCommandCanExecute));
			gg_loadState.Command = LoadStateCommand;

			CommandBindings.Add(new CommandBinding(SaveLayoutCommand, SaveLayoutCommandExecute, SaveLayoutCommandCanExecute));
			gg_saveLayout.Command = SaveLayoutCommand;
			CommandBindings.Add(new CommandBinding(LoadLayoutCommand, LoadLayoutCommandExecute, LoadLayoutCommandCanExecute));
			gg_loadLayout.Command = LoadLayoutCommand;

			gg_but_relayout.Command = new SimpleCommand(GGRelayoutCommandCanExecute, GGRelayoutCommandExecute);
		}


		#endregion

		void gg_async_Checked(object sender, RoutedEventArgs e)
		{
			Area.LogicCore.AsyncAlgorithmCompute = (bool)gg_async.IsChecked;
		}

		private void gg_saveAsPngImage_Click(object sender, RoutedEventArgs e)
		{
			Area.ExportAsImage(ImageType.PNG);
		}

		private void gg_printlay_Click(object sender, RoutedEventArgs e)
		{
			Area.PrintDialog("GraphX layout printing");
		}

		private void gg_vertexCount_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			//e.Handled = CustomHelper.IsIntegerInput(e.Text) && Convert.ToInt32(e.Text) <= datasourceSize;
		}

		private void gg_layalgo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			Area.GetLogicCore<LogicCoreExample>().DefaultLayoutAlgorithm = (LayoutAlgorithmTypeEnum)gg_layalgo.SelectedItem;
			//if (Area.LogicCore.Graph == null) gg_but_randomgraph_Click(null, null);
			//else 
			Area.RelayoutGraph();
			Area.GenerateAllEdges();
		}

		private void gg_useExternalLayAlgo_Checked(object sender, RoutedEventArgs e)
		{
			//if (gg_useExternalLayAlgo.IsChecked == true)
			//{
			//	var graph = Area.LogicCore.Graph == null ? GenerateDataGraph(Convert.ToInt32(gg_vertexCount.Text)) : Area.LogicCore.Graph;
			//	Area.LogicCore.Graph = graph;
			//	AssignExternalLayoutAlgorithm(graph);
			//}
			//else 
			Area.GetLogicCore<LogicCoreExample>().ExternalLayoutAlgorithm = null;
		}

		private void AssignExternalLayoutAlgorithm(BidirectionalGraph<DataVertex, DataEdge> graph)
		{
			Area.GetLogicCore<LogicCoreExample>().ExternalLayoutAlgorithm = Area.LogicCore.AlgorithmFactory.CreateLayoutAlgorithm(LayoutAlgorithmTypeEnum.ISOM, graph, null, null, null);
		}

		private void gg_oralgo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			Area.GetLogicCore<LogicCoreExample>().DefaultOverlapRemovalAlgorithm = (OverlapRemovalAlgorithmTypeEnum)gg_oralgo.SelectedItem;
			//if (Area.LogicCore.Graph == null) gg_but_randomgraph_Click(null, null);
			//else 
			Area.RelayoutGraph();
			Area.GenerateAllEdges();
		}

		private void gg_useExternalORAlgo_Checked(object sender, RoutedEventArgs e)
		{
			if (gg_useExternalORAlgo.IsChecked == true)
			{
				Area.GetLogicCore<LogicCoreExample>().ExternalOverlapRemovalAlgorithm = Area.LogicCore.AlgorithmFactory.CreateOverlapRemovalAlgorithm(OverlapRemovalAlgorithmTypeEnum.FSA, new Dictionary<DataVertex, Rect>(), null);
			}
			else Area.GetLogicCore<LogicCoreExample>().ExternalOverlapRemovalAlgorithm = null;
		}

		private void gg_eralgo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			Area.GetLogicCore<LogicCoreExample>().DefaultEdgeRoutingAlgorithm = (EdgeRoutingAlgorithmTypeEnum)gg_eralgo.SelectedItem;
			//if (Area.LogicCore.Graph == null) gg_but_randomgraph_Click(null, null);
			//else 
			Area.RelayoutGraph();
			Area.GenerateAllEdges();
		}

		private void gg_useExternalERAlgo_Checked(object sender, RoutedEventArgs e)
		{
			if (gg_useExternalERAlgo.IsChecked == true)
			{
				var graph = Area.LogicCore.Graph;
				Area.LogicCore.Graph = graph;
				Area.GetLogicCore<LogicCoreExample>().ExternalEdgeRoutingAlgorithm = Area.LogicCore.AlgorithmFactory.CreateEdgeRoutingAlgorithm(EdgeRoutingAlgorithmTypeEnum.SimpleER, new Rect(Area.DesiredSize), graph, null, null, null);
			}
			else Area.GetLogicCore<LogicCoreExample>().ExternalEdgeRoutingAlgorithm = null;
		}

		void AreaRelayoutFinished(object sender, EventArgs e)
		{
			if (Area.LogicCore.AsyncAlgorithmCompute)
				gg_loader.Visibility = System.Windows.Visibility.Collapsed;
			zoomctrl.ZoomToFill();
			zoomctrl.Mode = ZoomControlModes.Custom;
		}

		/// <summary>
		/// Use this event in case we have chosen async computation
		/// </summary>
		void AreaGenerateGraphFinished(object sender, EventArgs e)
		{

			Area.GenerateAllEdges();
			if (Area.LogicCore.AsyncAlgorithmCompute)
				gg_loader.Visibility = System.Windows.Visibility.Collapsed;

			zoomctrl.ZoomToFill();
			zoomctrl.Mode = ZoomControlModes.Custom;
		}

		//private void gg_but_randomgraph_Click(object sender, RoutedEventArgs e)
		//{
		//	Area.ClearLayout();
		//	var graph = GenerateDataGraph(Convert.ToInt32(gg_vertexCount.Text));
		//	//assign graph again as we need to update Graph param inside and i have no independent examples
		//	if (Area.GetLogicCore<LogicCoreExample>().ExternalLayoutAlgorithm != null)
		//		AssignExternalLayoutAlgorithm(graph);

		//	//supplied graph will be automaticaly be assigned to GraphArea::LogicCore.Graph property
		//	Area.GenerateGraph(graph);
		//	Area.SetVerticesDrag(true);

		//	if (Area.LogicCore.AsyncAlgorithmCompute)
		//		gg_loader.Visibility = System.Windows.Visibility.Visible;
		//}

		private void erg_toggleVertex_Click(object sender, RoutedEventArgs e)
		{
			if (Area.VertexList.First().Value.Visibility == System.Windows.Visibility.Visible)
				foreach (var item in Area.VertexList)
					item.Value.Visibility = System.Windows.Visibility.Collapsed;
			else foreach (var item in Area.VertexList)
					item.Value.Visibility = System.Windows.Visibility.Visible;

		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
		}

		private PathFinderEdgeRoutingParameters ergpf;
		public PathFinderEdgeRoutingParameters erg_PFERParameters { get { return ergpf; } set { ergpf = value; OnPropertyChanged("erg_PFERParameters"); } }
		private SimpleERParameters ergsimple;
		public SimpleERParameters erg_SimpleERParameters { get { return ergsimple; } set { ergsimple = value; OnPropertyChanged("erg_SimpleERParameters"); } }
		private BundleEdgeRoutingParameters ergbundle;
		public BundleEdgeRoutingParameters erg_BundleEdgeRoutingParameters { get { return ergbundle; } set { ergbundle = value; OnPropertyChanged("erg_BundleEdgeRoutingParameters"); } }

		void erg_useExternalERAlgo_Checked(object sender, System.Windows.RoutedEventArgs e)
		{
			if (erg_useExternalERAlgo.IsChecked == true)
			{
				//if (Area.LogicCore.Graph == null) erg_but_randomgraph_Click(null, null);
				Area.GetLogicCore<LogicCoreExample>().ExternalEdgeRoutingAlgorithm = Area.LogicCore.AlgorithmFactory.CreateEdgeRoutingAlgorithm(EdgeRoutingAlgorithmTypeEnum.SimpleER, new Rect(Area.DesiredSize), Area.LogicCore.Graph, null, null, null);
			}
			else Area.GetLogicCore<LogicCoreExample>().ExternalEdgeRoutingAlgorithm = null;
		}

		void erg_eralgo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			erg_recalculate.IsEnabled = true;
			if ((EdgeRoutingAlgorithmTypeEnum)erg_eralgo.SelectedItem == EdgeRoutingAlgorithmTypeEnum.None)
				erg_prmsbox.Visibility = System.Windows.Visibility.Collapsed;
			else
			{
				//clean prms
				erg_prmsbox.Visibility = System.Windows.Visibility.Visible;
				if ((EdgeRoutingAlgorithmTypeEnum)erg_eralgo.SelectedItem == EdgeRoutingAlgorithmTypeEnum.SimpleER)
				{
					simpleer_prms_dp.Visibility = System.Windows.Visibility.Visible;
					bundleer_prms_dp.Visibility = System.Windows.Visibility.Collapsed;
					pfer_prms_dp.Visibility = System.Windows.Visibility.Collapsed;
				}
				if ((EdgeRoutingAlgorithmTypeEnum)erg_eralgo.SelectedItem == EdgeRoutingAlgorithmTypeEnum.PathFinder)
				{
					simpleer_prms_dp.Visibility = System.Windows.Visibility.Collapsed;
					bundleer_prms_dp.Visibility = System.Windows.Visibility.Collapsed;
					pfer_prms_dp.Visibility = System.Windows.Visibility.Visible;
				}
				if ((EdgeRoutingAlgorithmTypeEnum)erg_eralgo.SelectedItem == EdgeRoutingAlgorithmTypeEnum.Bundling)
				{
					simpleer_prms_dp.Visibility = System.Windows.Visibility.Collapsed;
					bundleer_prms_dp.Visibility = System.Windows.Visibility.Visible;
					pfer_prms_dp.Visibility = System.Windows.Visibility.Collapsed;
					//bundling doesn't support single edge routing
					erg_recalculate.IsChecked = false;
					erg_recalculate.IsEnabled = false;
				}
			}
		}

		private void erg_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			e.Handled = CustomHelper.IsIntegerInput(e.Text);
		}

		private void erg_to1_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			e.Handled = CustomHelper.IsDoubleInput(e.Text);
			if (e.Handled) return;
			double res = 0.0;
			if (double.TryParse((sender as TextBox).Text, out res))
				if (res < 0.0 || res > 1.0) e.Handled = false;
		}

		private void erg_tominus1_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			e.Handled = CustomHelper.IsDoubleInput(e.Text);
			if (e.Handled) return;
			double res = 0.0;
			if (double.TryParse((sender as TextBox).Text, out res))
				if (res < -1.0 || res > 0.0) e.Handled = false;
		}

		void erg_recalculate_Checked(object sender, RoutedEventArgs e)
		{
			foreach (var item in Area.GetAllVertexControls())
				DragBehaviour.SetUpdateEdgesOnMove(item, (bool)erg_recalculate.IsChecked);

		}
		void erg_useCurves_Checked(object sender, RoutedEventArgs e)
		{
			//update edge curving property
			Area.LogicCore.EdgeCurvingEnabled = (bool)erg_useCurves.IsChecked;
			Area.UpdateAllEdges();
		}
	}

	public class SimpleCommand : ICommand
	{
		public Predicate<object> CanExecuteDelegate { get; set; }
		public Action<object> ExecuteDelegate { get; set; }

		public SimpleCommand(Predicate<object> can, Action<object> ex)
		{
			CanExecuteDelegate = can;
			ExecuteDelegate = ex;
		}

		#region ICommand Members

		public bool CanExecute(object parameter)
		{
			if (CanExecuteDelegate != null)
				return CanExecuteDelegate(parameter);
			return true;// if there is no can execute default to true
		}

		public event EventHandler CanExecuteChanged
		{
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		public void Execute(object parameter)
		{
			if (ExecuteDelegate != null)
				ExecuteDelegate(parameter);
		}

		#endregion
	}
}

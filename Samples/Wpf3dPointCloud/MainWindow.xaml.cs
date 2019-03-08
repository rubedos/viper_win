using Ros.Net.utilities;
using Rubedos.RosToolsApplicationBase.Pointcloud;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Wpf3dPointCloud
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    #region Constructor / Initialization

    /// <summary>
    /// Constructor
    /// </summary>
    public MainWindow()
    {
      Closing += MainWindow_Closing;

      InitializeComponent();
      InitializeBusinessLogic();
    }

    /// <summary>
    /// Initialization
    /// </summary>
    public void InitializeBusinessLogic()
    {
      ConfigurationHelper.RegisterSettings(Properties.Settings.Default);
      pointcloudViewModel = new PointcloudViewModel(new SharpDX.Size2(0, 0));
      PointcloudView.ViewModel = pointcloudViewModel;
      PointcloudView.InitializeScene();
      DataContext = pointcloudViewModel;

      rosControlBase.RosConnected += RosControlBase_RosConnected;
      rosControlBase.RosDisconnected += RosControlBase_RosDisconnected;
      rosControlBase.CvmDeviceInfoChaged += RosControlBase_CvmDeviceInfoChaged;
    } 

    #endregion

    #region Fields

    /// <summary>
    /// View model of PointClout
    /// </summary>
    private PointcloudViewModel pointcloudViewModel;

    #endregion

    #region Methods

    /// <summary>
    /// Releases resources
    /// </summary>
    public void Dispose()
    {
      rosControlBase.RosConnected -= RosControlBase_RosConnected;
      rosControlBase.RosDisconnected -= RosControlBase_RosDisconnected;
      rosControlBase.CvmDeviceInfoChaged -= RosControlBase_CvmDeviceInfoChaged;
      pointcloudViewModel.Dispose();
    }

    #endregion

    #region Event handlers

    /// <summary>
    /// Event handler is called when CVM device info has changed
    /// </summary>
    /// <param name="sender">Sender</param>
    /// <param name="e">Arguments</param>
    private void RosControlBase_CvmDeviceInfoChaged(object sender, EventArgs e)
    {
      double f = rosControlBase.Device.DeviceInfo.FocalPoint;
      double B = rosControlBase.Device.DeviceInfo.Baseline;

      if (pointcloudViewModel.ImagingPipeline != null)
      {
        pointcloudViewModel.ImagingPipeline.SetCameraInfo(B, f, rosControlBase.Device.DeviceInfo.PrincipalPoint);
      }
      PointcloudView.AddFov(rosControlBase.Device.DeviceInfo.FovV, rosControlBase.Device.DeviceInfo.FovH, 1f, 10f, rosControlBase.Device.DeviceInfo.Baseline);
    }

    /// <summary>
    /// Event handler is called when successfully connected to ROS master server
    /// </summary>
    /// <param name="sender">Sender</param>
    /// <param name="e">Arguments</param>
    private void RosControlBase_RosConnected(object sender, EventArgs e)
    {
      try
      {
        // NOTE: GPU filters can be initialized only when View3D of HelixToolkit has been launched.
        PointcloudView.InitializePipeline(Properties.Settings.Default.ForceCpuFiltering, 1);
        if (rosControlBase.Device.DeviceInfo != null)
        {
          pointcloudViewModel.ImagingPipeline.SetCameraInfo(rosControlBase.Device.DeviceInfo.Baseline, 
            rosControlBase.Device.DeviceInfo.FocalPoint, rosControlBase.Device.DeviceInfo.PrincipalPoint);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Fatal error initializing pipeline: {ex.GetBaseException().Message}");
        throw;
      }

      //     RGBPreview.Subscribe();
      //    DisparityPreview.Subscribe();
    }

    /// <summary>
    /// Event handler is called when disconnected from ROS master server
    /// </summary>
    /// <param name="sender">Sender</param>
    /// <param name="e">Arguments</param>
    private void RosControlBase_RosDisconnected(object sender, EventArgs e)
    {
      MessageBox.Show("Disconnected.");
    }

    /// <summary>
    /// Event handler is called when Application started exiting
    /// </summary>
    /// <param name="sender">Sender</param>
    /// <param name="e">Arguments</param>
    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      Closing -= MainWindow_Closing;
      Dispose();
    }

    #endregion
  }
}

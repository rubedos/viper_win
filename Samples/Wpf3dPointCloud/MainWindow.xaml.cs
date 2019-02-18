﻿using Ros.Net.utilities;
using Rubedos.RosToolsApplicationBase.PointCloud;
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
      pointCloudViewModel = new PointCloudViewModel(new SharpDX.Size2(0, 0));
      PointCloudView.ViewModel = pointCloudViewModel;
      PointCloudView.InitalizeScene();
      DataContext = pointCloudViewModel;

      rosControlBase.RosConnected += RosControlBase_RosConnected;
      rosControlBase.RosDisconnected += RosControlBase_RosDisconnected;
      rosControlBase.CvmDeviceInfoChaged += RosControlBase_CvmDeviceInfoChaged;
    } 

    #endregion

    #region Fields

    /// <summary>
    /// View model of PointClout
    /// </summary>
    private PointCloudViewModel pointCloudViewModel;

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
      pointCloudViewModel.Dispose();
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
      double f = rosControlBase.ViperDevice.DeviceInfo.FocalPoint;
      double B = rosControlBase.ViperDevice.DeviceInfo.Baseline;

      if (pointCloudViewModel.ImagingPipeline != null)
      {
        pointCloudViewModel.ImagingPipeline.SetCameraInfo(B, f, rosControlBase.ViperDevice.DeviceInfo.PrincipalPoint);
      }
      PointCloudView.SetFOVs(rosControlBase.ViperDevice.DeviceInfo.FovV, rosControlBase.ViperDevice.DeviceInfo.FovH, 1f, 10f, rosControlBase.ViperDevice.DeviceInfo.Baseline);
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
        PointCloudView.InitializePipeLine(Properties.Settings.Default.ForceCpuFiltering, 1);
        if (rosControlBase.ViperDevice.DeviceInfo != null)
        {
          pointCloudViewModel.ImagingPipeline.SetCameraInfo(rosControlBase.ViperDevice.DeviceInfo.Baseline, 
            rosControlBase.ViperDevice.DeviceInfo.FocalPoint, rosControlBase.ViperDevice.DeviceInfo.PrincipalPoint);
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

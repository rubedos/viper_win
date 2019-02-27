using Ros.Net.utilities;
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
using HelixToolkit.Wpf.SharpDX;
using SharpDX;

namespace SceneBoundingBox
{
  /// <summary>
  /// This sample requires VIPER to be hanged vertically above flat scene of observation. Objects placed inside the scene are 
  /// surrounded by bounding box in realtime.
  /// </summary>
  public partial class MainWindow : Window
  {
    #region privates
    /// <summary>
    /// View model of PointClout
    /// </summary>
    private PointCloudViewModel pointCloudViewModel;

    /// <summary>
    /// Distance from camera to the ground
    /// </summary>
    float groundZ = 1.85f;

    /// <summary>
    /// Width of the observed area on the ground
    /// </summary>
    float groundW = 1.6f;

    /// <summary>
    /// Depth of the observed area on the ground
    /// </summary>
    float groundD = 0.9f;

    /// <summary>
    /// Bounding box 3D models
    /// </summary>
    GroupModel3D boundingBoxGroup;

    /// <summary>
    /// For 3D label displaying
    /// </summary>
    BillboardText3D labels;

    /// <summary>
    /// RGBD points buffer
    /// </summary>
    float[] points = null;

    #endregion

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

      // Moving Camera vertically up and turning it to look down
      PointCloudView.ViewModel.SetCvmPosition(new System.Windows.Media.Media3D.Vector3D(0, 0, groundZ), 
        new System.Windows.Media.Media3D.Vector3D(180, 0, 90));

      var planeColor = System.Windows.Media.Color.FromArgb(20, 50, 255, 50).ToColor4();
      // Only points from this rectangular area are included and outliers are ignored
      var scenePlane = PointCloudView.ViewModel.CreatePlane(planeColor, groundD, groundW, new SharpDX.Vector3(0, 0, 1f));
      PointCloudView.SceneRoot.Children.Add(scenePlane);

      // For 3D texts
      BillboardTextModel3D text = new BillboardTextModel3D();
      PointCloudView.SceneRoot.Children.Add(text);
      labels = new BillboardText3D();
      text.Geometry = labels;

      boundingBoxGroup = new GroupModel3D();
      PointCloudView.SceneRoot.Children.Add(boundingBoxGroup);

      rosControlBase.RosConnected += RosControlBase_RosConnected;
      rosControlBase.RosDisconnected += RosControlBase_RosDisconnected;
      rosControlBase.CvmDeviceInfoChaged += RosControlBase_CvmDeviceInfoChaged;
    } 

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
      rosControlBase.ViperDevice.Dispose();
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
        pointCloudViewModel.ImagingPipeline.ImageDataProcessed += ImagingPipeline_ImageDataProcessed;
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
    /// Convenience method - extract point from RGBD buffer
    /// </summary>
    /// <param name="buffer">the point cloud</param>
    /// <param name="x">x</param>
    /// <param name="y">y</param>
    /// <param name="w">width</param>
    /// <param name="h">height</param>
    /// <returns></returns>
    Vector3 GetPoint(float[] buffer, int x, int y, int w, int h)
    {
      float X = buffer[(y * w + x) * 4 + 1];
      float Y = buffer[(y * w + x) * 4 + 2];
      float Z = buffer[(y * w + x) * 4 + 3];
      return new Vector3(X, Y, Z);
    }

    /// <summary>
    /// Process point cloud as soon as it arrives
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ImagingPipeline_ImageDataProcessed(object sender, Rubedos.PointcloudProcessing.ImagingPipelineProcessedEventArgs e)
    {
      var rgbd = pointCloudViewModel.ImagingPipeline.RgbdOut;
      if (points == null)
      {
        points = new float[rgbd.Cols * rgbd.Rows * rgbd.Channels];
      }
      int w = rgbd.Cols, h = rgbd.Rows;
      // hint: working with data buffer is much faster than accessing individual points in Cv.Mat
      System.Runtime.InteropServices.Marshal.Copy(rgbd.Data, points, 0, points.Length);

      float minZ = groundZ,
        maxX = -groundW, minX = groundW, minY = groundD, maxY = -groundD;
      for (int y = 1; y < h; y += 1)
        for (int x = 1; x < w; x += 1)
        {
          var current = GetPoint(points, x, y, w, h);
          if (current.Z > groundZ || current.Z < 1.0f ||
            current.X > groundW / 2 || current.X < -groundW / 2 ||
            current.Y > groundD / 2 || current.Y < -groundD / 2)
            continue; // Out of space of interest
          if (current.X > maxX) maxX = current.X;
          if (current.X < minX) minX = current.X;
          if (current.Y > maxY) maxY = current.Y;
          if (current.Y < minY) minY = current.Y;
          if (current.Z < minZ) minZ = current.Z;
        }
      Console.WriteLine("Boundaries: X ({0:0.00}; {1:0.00}) Y ({2:0.00}; {3:0.00}) Z ({4:0.00}; {5:0.00})",
        maxX, minX, maxY, minY, groundZ, minZ);

      // Synchronizing with 3D rendering thread
      pointCloudViewModel.Context.Send((o) =>
      {
        boundingBoxGroup.Children.Clear();
        MeshBuilder mb = new MeshBuilder();
        var center = new Vector3((maxY + minY) / 2, (maxX + minX) / 2, (groundZ - minZ) / 2);
        float xlen = maxY - minY, ylen = maxX - minX, zlen = groundZ - minZ;
        mb.AddBox(center, xlen, ylen, zlen);

        MeshGeometryModel3D mmodel = new MeshGeometryModel3D();
        boundingBoxGroup.Children.Add(mmodel);
        mmodel.Geometry = mb.ToMeshGeometry3D();

        var boxColor = System.Windows.Media.Color.FromArgb(30, 50, 0, 255).ToColor4();
        mmodel.Material = new PhongMaterial()
        {
          AmbientColor = boxColor,// System.Windows.Media.Colors.Gray.ToColor4(),
          DiffuseColor = boxColor,//System.Windows.Media.Colors.Yellow.ToColor4(), 
                                    //System.Windows.Media.Color.FromArgb(100, 255, 0 , 0).ToColor4(), 
          SpecularColor = boxColor,// System.Windows.Media.Colors.Gray.ToColor4(),
          SpecularShininess = 100f,
        };

        LineGeometryModel3D linem = new LineGeometryModel3D();
        var lb = new LineBuilder();
        lb.AddBox(center, xlen, ylen, zlen);
        linem.Thickness = 1;
        linem.Color = System.Windows.Media.Colors.Yellow; //System.Windows.Media.Color.FromArgb(30, 50, 0, 255).ToColor4();
        linem.Geometry = lb.ToLineGeometry3D();
        boundingBoxGroup.Children.Add(linem);
        float scale = 1.0f;
        labels.TextInfo.Clear();
        labels.TextInfo.Add(new TextInfo()
          { Text = String.Format("H = {0:0.0} m", zlen),
          Origin = new Vector3(xlen / 2 + 0.1f, -(ylen/2 + 0.1f), zlen/2) + center, Foreground = Colors.Black.ToColor4(), Scale = scale
        });

        labels.TextInfo.Add(new TextInfo()
        { 
          Text = String.Format("W = {0:0.0} m", xlen),
          Origin = new Vector3(0, ylen / 2 + 0.1f, zlen + 0.1f) + center,
          Foreground = Colors.Black.ToColor4(),
          Scale = scale
        });

        labels.TextInfo.Add(new TextInfo()
        {
          Text = String.Format("D = {0:0.0} m", ylen),
          Origin = new Vector3(xlen / 2 + 0.1f, 0f, zlen + 0.1f) + center,
          Foreground = Colors.Black.ToColor4(),
          Scale = scale
        });

      }, null);


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

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Collections.Generic;
using Ros.Net;
using Ros.Net.utilities;
using Rubedos.Viper.Net.Iface;

namespace WpfFollowMe
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    #region Constructor && Initialization

    /// <summary>
    /// Constructor
    /// </summary>
    public MainWindow()
    {
      InitializeComponent();
      InitializeBusinessLogic();
    }

    /// <summary>
    /// Initialization
    /// </summary>
    public void InitializeBusinessLogic()
    {
      ConfigurationHelper.RegisterSettings(Properties.Settings.Default);

      EnableTopic = Properties.Settings.Default.EnableTopic;
      BoundingBoxesTopic = Properties.Settings.Default.BoundingBoxesTopic;
      TargetBoundingBoxTopic = Properties.Settings.Default.TargetBoundingBoxTopic;
      TargetPositionTopic = Properties.Settings.Default.TargetPositionTopic;
      SetTargetTopic = Properties.Settings.Default.SetTargetTopic;

      DataContext = this;
      rosControlBase.RosConnected += RosControlBase_RosConnected;
      rosControlBase.RosDisconnecting += RosControlBase_RosDisconnecting;
      DetectionImage.ImageSink.Updated += ImageSink_Updated;
      Closing += MainWindow_Closing;

      trackedBoxes = new List<RectangleOfInterest>();
      targets.Children.Add(targetRoi.Geom);

      var timer = new DispatcherTimer();
      timer.Interval = TimeSpan.FromSeconds(0.3);
      timer.Tick += (s,e) => UpdateRectanglesVisibility();
      timer.Start();
    }

    #endregion

    #region Fields

    private bool firstUpdate = true;
    private bool isFollowMeEnabled = false;

    private string EnableTopic;
    private string BoundingBoxesTopic;
    private string TargetBoundingBoxTopic;
    private string TargetPositionTopic;
    private string SetTargetTopic;

    private NodeHandle mainHandle;
    private List<RectangleOfInterest> trackedBoxes;
    private RectangleOfInterest targetRoi = new RectangleOfInterest();

    private Publisher<Ros.Net.Messages.std_msgs.Bool> isEnabledPublisher;
    private Publisher<Ros.Net.Messages.cvm_msgs.BoundingBox> setTargetPublisher;

    #endregion

    #region Event handlers

    /// <summary>
    /// Application closing started
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="e">Arguments.</param>
    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      Dispose();
    }
    
    /// <summary>
    /// Event is fired when successfully connected to ROS master server
    /// </summary>
    /// <param name="sender">Sender</param>
    /// <param name="e">Arguments</param>
    private void RosControlBase_RosConnected(object sender, EventArgs e)
    {
      DetectionImage.Visibility = Visibility.Visible;

      mainHandle = new NodeHandle();

      // Enable / Disable follow me topic
      isEnabledPublisher = mainHandle.advertise<Ros.Net.Messages.std_msgs.Bool>(EnableTopic, 1, false);

      // Followed bounding boxes
      mainHandle.subscribe<Ros.Net.Messages.cvm_msgs.BoundingBoxes>(BoundingBoxesTopic, 1, (boundingBoxes) => ReDrawRectangles(boundingBoxes));
      DetectionImage.Subscribe();

      // Set target
      setTargetPublisher = mainHandle.advertise<Ros.Net.Messages.cvm_msgs.BoundingBox>(SetTargetTopic, 1, true);

      // Target Bounding Box
      mainHandle.subscribe<Ros.Net.Messages.cvm_msgs.BoundingBox>(TargetBoundingBoxTopic, 1, (boundingBox) => ReDrawTargetRectangle(boundingBox));

      // Target distance
      mainHandle.subscribe<Ros.Net.Messages.geometry_msgs.Point>(TargetPositionTopic, 1, (targetPosition) => UpdateTargetDistance(targetPosition));

      // Enabling button
      enableDisableFollowMeButton.IsEnabled = true;
    }

    /// <summary>
    /// Event is fired when disconnected from ROS master server
    /// </summary>
    /// <param name="sender">Sender</param>
    /// <param name="e">Arguments</param>
    private void RosControlBase_RosDisconnecting(object sender, EventArgs e)
    {
      DetectionImage.Visibility = Visibility.Hidden;

      // In case follow me is left working, user is asked if he wants it be shutdown
      AskToDisableFollowMe();

      // Disabling button
      enableDisableFollowMeButton.IsEnabled = false;

      // Disconnecting
      mainHandle.Dispose();
    }

    /// <summary>
    /// Image updated
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="e">Arguments.</param>
    private void ImageSink_Updated(object sender, EventArgs e)
    {
      Dispatcher.Invoke(new Action(() =>
      {
        if (firstUpdate)
        {
          firstUpdate = false;
          DetectionImage.SetActualSize();
        }
      }));
    }

    /// <summary>
    /// Mouse clicked on rectangle
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="e">Arguments.</param>
    private void DetectionImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
      var point = e.GetPosition(DetectionImage);
      foreach (var roi in trackedBoxes)
      {
        if (roi.Rect.Contains(point))
        {
          System.Diagnostics.Debug.WriteLine("Contained by roi #{0}", roi.SeqId);
          var bboxMsg = new Ros.Net.Messages.cvm_msgs.BoundingBox();
          bboxMsg.xmin = (uint)roi.Rect.X;
          bboxMsg.ymin = (uint)roi.Rect.Y;
          bboxMsg.xmax = (uint)(roi.Rect.X + roi.Rect.Width);
          bboxMsg.ymax = (uint)(roi.Rect.Y + roi.Rect.Height);
          setTargetPublisher.publish(bboxMsg);
        }
      }
    }

    /// <summary>
    /// Event handler of <see cref="enableDisableFollowMeButton.Click"/> event.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void EnableDisableFollowMeButtonClick(object sender, RoutedEventArgs e)
    {
      isFollowMeEnabled = !isFollowMeEnabled;
      isEnabledPublisher?.publish(new Ros.Net.Messages.std_msgs.Bool() { data = isFollowMeEnabled });

      if (isFollowMeEnabled) enableDisableFollowMeButton.Content = "Stop";
      else enableDisableFollowMeButton.Content = "Start";
    }

    #endregion

    #region Methods

    /// <summary>
    /// Releases resources
    /// </summary>
    public void Dispose()
    {
      AskToDisableFollowMe();

      Closing -= MainWindow_Closing;
      rosControlBase.RosConnected -= RosControlBase_RosConnected;
      rosControlBase.RosDisconnecting -= RosControlBase_RosDisconnecting;
      if (DetectionImage != null && DetectionImage.ImageSink != null) DetectionImage.ImageSink.Updated -= ImageSink_Updated;
    }

    /// <summary>
    /// In case follow me is enabled, user is asked if he want's it to be disabled.
    /// </summary>
    private void AskToDisableFollowMe()
    {
      if (!isFollowMeEnabled) return;

      var answer = MessageBox.Show("Follow me is in progress, would you like to stop it?", "Follow me", MessageBoxButton.YesNo, MessageBoxImage.Question);
      if (answer == MessageBoxResult.Yes)
      {
        EnableDisableFollowMeButtonClick(null, null);
      }
    }

    /// <summary>
    /// Checks if provided rectangle of interest belongs to target
    /// </summary>
    /// <param name="roi">Rectangle of interest</param>
    /// <returns>True if equals, otherwise - false.</returns>
    private bool IsRectangleOfInterestIsTarget(RectangleOfInterest roi)
    {
      if (TargetPath.Visibility == Visibility.Hidden) return false;

      var target = new Rect(targetRoi.Rect.Location, targetRoi.Rect.Size);
      target.Intersect(roi.Rect);
      if (target.IsEmpty) return false;

      double intersectionPercent = 0.3;
      if (Math.Abs(1 - target.Width / targetRoi.Rect.Width) > intersectionPercent || Math.Abs(1 - target.Height / targetRoi.Rect.Height) > intersectionPercent) return false;

      return true;
    }

    /// <summary>
    /// Updates target rectangle within View
    /// </summary>
    /// <param name="boundingBox">Bounding boxes of rectangle.</param>
    private void ReDrawTargetRectangle(Ros.Net.Messages.cvm_msgs.BoundingBox boundingBox)
    {
      if (boundingBox == null) return;
      Dispatcher.Invoke(new Action(() => targetRoi.UpdateRoi(new RectangleOfInterest(boundingBox))));
    }

    /// <summary>
    /// Updates distance label of target.
    /// </summary>
    /// <param name="targetPosition">Distance.</param>
    private void UpdateTargetDistance(Ros.Net.Messages.geometry_msgs.Point targetPosition)
    {
      if (targetPosition == null) return;

      Dispatcher.Invoke(new Action(() =>
      {
        targetRoi.Depth = targetPosition.z;
        targetLabel.Content = string.Format("Distance: {0:0.00} m", targetPosition.z);
      }));
    }

    /// <summary>
    /// Updates rectangles visibility in case they are expired or new
    /// </summary>
    private void UpdateRectanglesVisibility()
    {
      var toRemove = new List<RectangleOfInterest>();
      allDetections.Children.Clear();
      lostDetections.Children.Clear();
      foreach (var item in trackedBoxes)
      {
        double idle = item.IdleTime();
        if (idle > 1.0) toRemove.Add(item);
        else if (idle > 0.3)
        {
          lostDetections.Children.Add(item.Geom);
        }
        else
        {
          allDetections.Children.Add(item.Geom);
        }
      }

      if (targetRoi.IdleTime() > 2.0)
      {
        TargetPath.Visibility = Visibility.Hidden;
        targetLabel.Content = "Distance: -- m";
      }
      else
      {
        TargetPath.Visibility = Visibility.Visible;
      }
    }

    /// <summary>
    /// Updates rectangles within View
    /// </summary>
    /// <param name="boundingBoxes">Bounding boxes of Rectangles</param>
    private void ReDrawRectangles(Ros.Net.Messages.cvm_msgs.BoundingBoxes boundingBoxes)
    {
      if (boundingBoxes == null || boundingBoxes.boundingBoxes == null) return;

      Dispatcher.Invoke(new Action(() =>
      {
        foreach (var boundingBox in boundingBoxes.boundingBoxes)
        {
          var updated = false;
          var rectangleOfInterest = new RectangleOfInterest(boundingBox);
          foreach (var currentBox in trackedBoxes)
          {
            if (currentBox.IsTheSame(rectangleOfInterest))
            {
              currentBox.UpdateRoi(rectangleOfInterest);
              updated = true;
              break;
            }
          }

          if (!updated && !IsRectangleOfInterestIsTarget(rectangleOfInterest))
          {
            trackedBoxes.Add(rectangleOfInterest);
          }
        }
      }));
    }

    #endregion
  }
}

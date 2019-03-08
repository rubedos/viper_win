using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Ros.Net.utilities;
using Rubedos.Viper.Net.PerceptionApps;

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

      DataContext = this;
      followMeApp = new FollowMeApp(rosControlBase.Device);
      
      followMeApp.OnTargetDistanceChanged += FollowMeApp_OnTargetDistanceChanged;

      rosControlBase.RosConnected += RosControlBase_RosConnected;
      rosControlBase.RosDisconnecting += RosControlBase_RosDisconnecting;
      DetectionImage.ImageSink.Updated += ImageSink_Updated;
      Closing += MainWindow_Closing;

      dispatcherTimer = new DispatcherTimer();
      dispatcherTimer.Interval = TimeSpan.FromSeconds(0.3);
      dispatcherTimer.Tick += (s, e) => UpdateRectanglesVisibility();
    }

    #endregion

    #region Fields

    private bool firstUpdate = true;
    private FollowMeApp followMeApp;
    private DispatcherTimer dispatcherTimer;


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
      DetectionImage.Subscribe();

      // Enabling button
      enableDisableFollowMeButton.IsEnabled = true;

      dispatcherTimer.Start();
    }

    /// <summary>
    /// Event is fired when disconnected from ROS master server
    /// </summary>
    /// <param name="sender">Sender</param>
    /// <param name="e">Arguments</param>
    private void RosControlBase_RosDisconnecting(object sender, EventArgs e)
    {
      // Unsubscribing from image stream
      DetectionImage.Visibility = Visibility.Hidden;
      DetectionImage.Unsubscribe();

      // In case follow me is left working, user is asked if he wants it be shutdown
      AskToDisableFollowMe();

      // Disabling button
      enableDisableFollowMeButton.IsEnabled = false;

      firstUpdate = true;

      dispatcherTimer.Stop();
    }

    /// <summary>
    /// Image updated
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="e">Arguments.</param>
    private void ImageSink_Updated(object sender, EventArgs e)
    {
      if (!firstUpdate) return;

      Dispatcher.Invoke(new Action(() =>
      {
        firstUpdate = false;
        DetectionImage.SetActualSize();
      }));
    }

    /// <summary>
    /// Mouse clicked on rectangle
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="e">Arguments.</param>
    private void DetectionImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (!followMeApp.IsEnabled) return;

      var point = e.GetPosition(DetectionImage);
      var person = followMeApp.DetectedPersons.FirstOrDefault(i => i.Rectangle.Rect.Contains(point));

      if (person == null)
      {
        followMeApp.StopTracking();
        TargetPath.Visibility = Visibility.Hidden;
        targetLabel.Content = "Distance: -- m";
        return;
      }
      followMeApp.StartTracking(person);
    }

    /// <summary>
    /// Event handler of <see cref="enableDisableFollowMeButton.Click"/> event.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void EnableDisableFollowMeButtonClick(object sender, RoutedEventArgs e)
    {
      if (followMeApp.IsEnabled)
      {
        followMeApp.Stop();
        enableDisableFollowMeButton.Content = "Start";

        allDetections.Children.Clear();
        lostDetections.Children.Clear();
        targets.Children.Clear();

        TargetPath.Visibility = Visibility.Hidden;
        targetLabel.Content = "Distance: -- m";
        return;
      }

      followMeApp.Start();
      enableDisableFollowMeButton.Content = "Stop";
    }

    /// <summary>
    /// Event handler is triggered when target (<see cref="Person"/>) distance changes.
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="e">Arguments.</param>
    private void FollowMeApp_OnTargetDistanceChanged(object sender, Rubedos.Viper.Net.PerceptionApps.EventArgs.PersonDistanceChangedEventArgs e)
    {
      Dispatcher.Invoke(new Action(() =>
      {
        targetLabel.Content = string.Format("Distance: {0:0.00} m", e.Distance);
      }));
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

      followMeApp.OnTargetDistanceChanged -= FollowMeApp_OnTargetDistanceChanged;
      followMeApp.Dispose();
    }

    /// <summary>
    /// In case follow me is enabled, user is asked if he want's it to be disabled.
    /// </summary>
    private void AskToDisableFollowMe()
    {
      if (!followMeApp.IsEnabled) return;

      var answer = MessageBox.Show("Follow me is in progress, would you like to stop it?", "Follow me", MessageBoxButton.YesNo, MessageBoxImage.Question);
      if (answer == MessageBoxResult.Yes)
      {
        EnableDisableFollowMeButtonClick(null, null);
      }
    }

    /// <summary>
    /// Updates rectangles visibility in case they are expired or new
    /// </summary>
    private void UpdateRectanglesVisibility()
    {
      if (!Dispatcher.CheckAccess())
      {
        Dispatcher.Invoke(new Action(() => UpdateRectanglesVisibility()));
        return;
      }

      // Cleaning rectangles
      targets.Children.Clear();
      allDetections.Children.Clear();
      lostDetections.Children.Clear();

      if (!followMeApp.IsEnabled) return;

      // Adding new rectangles
      foreach (var person in followMeApp.DetectedPersons.Where(i => !i.IsTarget))
      {
        var idle = person.Rectangle.IdleTime();

        var rectangle = new System.Windows.Media.RectangleGeometry(person.Rectangle.Rect, 0, 0);
        if (idle > 0.3 && idle < 2.0) lostDetections.Children.Add(rectangle);
        else if (idle <= 0.3) allDetections.Children.Add(rectangle);
      }

      // Showing / hiding target
      if (!followMeApp.IsTracking || followMeApp.Target == null) return;

      if (followMeApp.Target.Rectangle.IdleTime() > 2.0)
      {
        TargetPath.Visibility = Visibility.Hidden;
        targetLabel.Content = "Distance: -- m";
        return;
      }

      var targetRectangle = new System.Windows.Media.RectangleGeometry(followMeApp.Target.Rectangle.Rect, 0, 0);
      targets.Children.Add(targetRectangle);
      TargetPath.Visibility = Visibility.Visible;
    }

    #endregion
  }
}

using System;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using Rubedos.Viper.Net;
using Ros.Net.utilities;
using ImageStreamWpf.Helpers;
using ImageStreamWpf.ViewModels.Base;

namespace ImageStreamWpf.ViewModels.Windows
{
  class MainWindowViewModel : ViewModelBase
  {
    #region Properties

    private List<string> topicsList;
    /// <summary>
    /// Topics List
    /// </summary>
    public List<string> TopicsList
    {
      get { return topicsList; }
      set
      {
        if (topicsList != value)
        {
          topicsList = value;
          OnPropertyChanged(nameof(TopicsList));
        }
      }
    }

    public string selectedTopic;
    /// <summary>
    /// Current selected topic
    /// </summary>
    public string SelectedTopic
    {
      get { return selectedTopic; }
      set
      {
        if (selectedTopic != value)
        {
          selectedTopic = value;
          OnPropertyChanged(nameof(SelectedTopic));
          if (viperDevice != null && viperDevice.IsConnected) StartImageStream();
        }
      }
    }

    private string cameraIpAddress;

    /// <summary>
    /// Camera IP address
    /// </summary>
    public string CameraIpAddress
    {
      get { return cameraIpAddress; }
      set
      {
        if (cameraIpAddress != value)
        {
          cameraIpAddress = value;
          OnPropertyChanged(nameof(CameraIpAddress));
        }
      }
    }

    private bool isCameraIpAddressEnabled;
    /// <summary>
    /// <see cref="CameraIpAddress"/> field is either Enabled or Disabled.
    /// </summary>
    public bool IsCameraIpAddressEnabled
    {
      get { return isCameraIpAddressEnabled; }
      set
      {
        if (isCameraIpAddressEnabled != value)
        {
          isCameraIpAddressEnabled = value;
          OnPropertyChanged(nameof(IsCameraIpAddressEnabled));
        }
      }
    }

    public string deviceStatus;
    /// <summary>
    /// <see cref="viperDevice"/> status: Connecting, Connected, Disconnecting or Disconnected.
    /// </summary>
    public string DeviceStatus
    {
      get { return deviceStatus; }
      set
      {
        if (deviceStatus != value)
        {
          deviceStatus = value;
          OnPropertyChanged(nameof(DeviceStatus));
        }
      }
    }

    private string connectDisconnectButtonContent;
    /// <summary>
    /// Connect button content, either 'Connect' or 'Disconnect'.
    /// </summary>
    public string ConnectDisconnectButtonContent
    {
      get { return connectDisconnectButtonContent; }
      set
      {
        if (connectDisconnectButtonContent != value)
        {
          connectDisconnectButtonContent = value;
          OnPropertyChanged(nameof(ConnectDisconnectButtonContent));
        }
      }
    }

    private bool isConnectDisconnectButtonEnabled;
    /// <summary>
    /// Connect button enabled or disabled.
    /// </summary>
    public bool IsConnectDisconnectButtonEnabled
    {
      get { return isConnectDisconnectButtonEnabled; }
      set
      {
        if (isConnectDisconnectButtonEnabled != value)
        {
          isConnectDisconnectButtonEnabled = value;
          OnPropertyChanged(nameof(IsConnectDisconnectButtonEnabled));
        }
      }
    }

    public Bitmap deviceImage;
    /// <summary>
    /// Right lense image from <see cref="viperDevice"/>.
    /// </summary>
    public Bitmap DeviceImage
    {
      get { return deviceImage; }
      set
      {
        if (deviceImage != value)
        {
          deviceImage = value;
          OnPropertyChanged(nameof(DeviceImage));
        }
      }
    }

    #endregion

    #region Commands

    private ICommand connectButtonClickCommand;

    /// <summary>
    /// Connect button click command
    /// </summary>
    public ICommand ConnectButtonClickCommand
    {
      get { return connectButtonClickCommand; }
      set { connectButtonClickCommand = value; }
    }

    #endregion

    #region Fields

    /// <summary>
    /// Camera
    /// </summary>
    private CvmDevice viperDevice;

    private BitmapSink imageSink;
    private IImageSubscriber imageSubscriber;

    #endregion

    #region IDisposable

    /// <summary>
    /// Releases resources
    /// </summary>
    public override void Dispose()
    {
      if (viperDevice != null && viperDevice.IsConnected) DisconnectDevice();

      StopImageStream();

      base.Dispose();
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Constructor
    /// </summary>
    public MainWindowViewModel()
    {
      DeviceStatus = "Disconnected";
      cameraIpAddress = "http://192.168.1.170:11311";

      var list = new List<string>();
      list.Add("${TopicPrefix}/left/image_rect");
      list.Add("${TopicPrefix}/right/image_rect");
      list.Add("${TopicPrefix}/left/image_raw");
      list.Add("${TopicPrefix}/right/image_raw");
      list.Add("${TopicPrefix}/stereo/image_rect");
      TopicsList = list;

      SelectedTopic = TopicsList.First();

      IsCameraIpAddressEnabled = true;
      UpdateConnectDisconnectButtonContent();
      IsConnectDisconnectButtonEnabled = true;
      ConnectButtonClickCommand = new RelayCommand(new Action<object>(ConnectButtonClicked));
    }

    #endregion

    #region Methods

    /// <summary>
    /// Connects to <see cref="viperDevice"/>.
    /// </summary>
    private void ConnectDevice()
    {
      DeviceStatus = "Connecting..";
      IsCameraIpAddressEnabled = false;

      viperDevice = new CvmDevice(cameraIpAddress, null);
      viperDevice.Connected += ViperDevice_Connected;
      viperDevice.Disconnected += ViperDevice_Disconnected;

      IsConnectDisconnectButtonEnabled = false;
      ThreadPool.QueueUserWorkItem((e) =>
      {
        try
        {
          viperDevice.Connect();
        }
        catch (Ros.Net.RosException ex)
        {
          MessageBox.Show(ex.GetBaseException().Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
          IsConnectDisconnectButtonEnabled = true;
          IsCameraIpAddressEnabled = true;
        }
      });
    }

    /// <summary>
    /// Disconnect from <see cref="viperDevice"/>.
    /// </summary>
    private  void DisconnectDevice()
    {
      DeviceStatus = "Disconnecting..";
      IsConnectDisconnectButtonEnabled = false;

      viperDevice.Disconnect();

      viperDevice.Connected -= ViperDevice_Connected;
      viperDevice.Disconnected -= ViperDevice_Disconnected;

      viperDevice.Dispose();

      viperDevice = null;
    }

    /// <summary>
    /// Starts image stream
    /// </summary>
    private void StartImageStream()
    {
      if (imageSink != null)
      {
        imageSink.Updated -= ImageSinkUpdated;
        imageSink = null;
        imageSubscriber = null;
        DeviceImage = null;
      }

      imageSink = new BitmapSink();
      imageSink.Updated += ImageSinkUpdated;
      imageSubscriber = new ImageSubscriber<ImageHandler>(SelectedTopic, imageSink);
    }

    /// <summary>
    /// Stops image stream
    /// </summary>
    private void StopImageStream()
    {
      if (imageSink != null)
      {
        imageSink.Updated -= ImageSinkUpdated;
        imageSink = null;
        imageSubscriber = null;
      }
    }

    /// <summary>
    /// Updates Connect/Disconnect button content with text either 'Connect' or 'Disconnect'.
    /// </summary>
    private void UpdateConnectDisconnectButtonContent()
    {
      ConnectDisconnectButtonContent = viperDevice != null && viperDevice.IsConnected ? nameof(viperDevice.Disconnect) : nameof(viperDevice.Connect);
    }

    #endregion

    #region Event handler

    /// <summary>
    /// Connect button event handler
    /// </summary>
    /// <param name="data">Hello world!</param>
    private void ConnectButtonClicked(object data)
    {
      if (viperDevice != null && viperDevice.IsConnected)
      {
        DisconnectDevice();
        return;
      }

      ConnectDevice();
    }

    /// <summary>
    /// This handler receives images
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ImageSinkUpdated(object sender, EventArgs e)
    {
      DeviceImage = imageSink.Bitmap;
    }

    /// <summary>
    /// Event handler fired when disconnected from <see cref="viperDevice"/>.
    /// </summary>
    /// <param name="sender">Sender</param>
    /// <param name="e">Args</param>
    private void ViperDevice_Disconnected(object sender, EventArgs e)
    {
      DeviceStatus = "Disconnected";
      UpdateConnectDisconnectButtonContent();
      IsConnectDisconnectButtonEnabled = true;
      IsCameraIpAddressEnabled = true;
      StopImageStream();
    }

    /// <summary>
    /// Event handler called when connected to <see cref="viperDevice"/>.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ViperDevice_Connected(object sender, EventArgs e)
    {
      DeviceStatus = "Connected";
      UpdateConnectDisconnectButtonContent();
      IsConnectDisconnectButtonEnabled = true;

      StartImageStream();
    }

    #endregion
  }
}

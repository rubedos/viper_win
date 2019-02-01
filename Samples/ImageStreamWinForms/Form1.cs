using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ros.Net;
using Ros.Net.utilities;

namespace ImageStreamWinForms
{
  public partial class Form1 : Form
  {
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    Ros.Net.utilities.IImageSubscriber imageSubscriber;
    BitmapSink bmpSink;
    IPublisher pub;
    NodeHandle nh;
    Rubedos.Viper.Net.CvmDevice ViperDevice = new Rubedos.Viper.Net.CvmDevice();

    public Form1()
    {
      InitializeComponent();
      log.InfoFormat("Running application {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

      ViperDevice.Connected += ViperDevice_Connected;

    }

    private void ViperDevice_Connected(object sender, EventArgs e)
    {
      if (InvokeRequired)
        Invoke( new Action(() =>
        {
          nh = new NodeHandle();
          bmpSink = new BitmapSink();
          bmpSink.Updated += BmpSink_Updated;
          imageSubscriber = new Ros.Net.utilities.ImageSubscriber<ImageHandler>("${TopicPrefix}/left/image_rect", bmpSink);
        })
      );
    }

    /// <summary>
    /// This handler receives images
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BmpSink_Updated(object sender, EventArgs e)
    {
      var bmp = bmpSink.Bitmap;
      imageBox.Invoke(new Action(() =>
      {
        // Consume the bitmap
        imageBox.Image = bmp;
      }
      ));
    }

    private void Connect_Click(object sender, EventArgs e)
    {
      // Connect is blocking, therefore it is recommended to run it on another thread
      System.Threading.ThreadPool.QueueUserWorkItem((cb) =>
      {
        try
        {
          ViperDevice.Connect(new Uri(uriTextBox.Text));
        }
        catch (Exception ex)
        {
          log.ErrorFormat("Connection failed {0}", ex);
        }
      });
    }

    private void Form1_FormClosed(object sender, FormClosedEventArgs e)
    {
      if (nh != null)
      {
        nh.Dispose();
        nh = null;
      }
      if (ViperDevice != null)
      {
        ViperDevice.Dispose();
        ViperDevice = null;
      }
      Application.Exit();
    }
  }
}

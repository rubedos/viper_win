using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ros.Net;
using Ros.Net.utilities;
using Rubedos.Viper.Net;

namespace PointCloudSample
{
  /// <summary>
  /// Following sample shows how to read point cloud from camera. The received data frame is described in ROS documentation:
  /// http://docs.ros.org/melodic/api/sensor_msgs/html/msg/PointCloud2.html
  ///
  /// This sample exports first frame of the pointcloud into text file that can be opened and viewed with RosTools PointCloudViewer application.
  /// </summary>
  class Program
  {
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    static void Main(string[] args)
    {
      try
      {
        var currentDir = new System.IO.FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).Directory;
        string fileName = String.Format(@"{1}\rgbd.{0}.txt", DateTime.Now.ToLongTimeString().Replace(":", "-"), currentDir);
        System.IO.FileInfo fi = new System.IO.FileInfo(fileName);

        // Specify VIPER IP here
        using (CvmDevice module = new CvmDevice("http://192.168.1.170:11311", "192.168.1.51"))
        {
          module.RosNode = "PointCloudSample";
          module.Connect();
          if (!module.IsConnected)
            throw new InvalidOperationException("Cannot connect to VIPER!");

          bool firstFrame = true;
          NodeHandle handle = new NodeHandle();
          handle.subscribe<Ros.Net.Messages.sensor_msgs.PointCloud2>("${TopicPrefix}/points2", 1, (msg) =>
          {
            if (msg == null)
              return;
            if (firstFrame)
            {
              firstFrame = false;
              log.InfoFormat("Received point cloud {0} x {1}", msg.width, msg.height);
              log.InfoFormat("Is dense: {0}", msg.is_dense ? "Yes" : "No");
              log.InfoFormat("Point step: {0}", msg.point_step);
              log.InfoFormat("Is bigendian: {0}", msg.is_bigendian ? "Yes" : "No");
              log.InfoFormat("Row step: {0}", msg.row_step);
              log.InfoFormat("Total data: {0}", msg.data.Length);
              // More information about PointField structure: http://docs.ros.org/melodic/api/sensor_msgs/html/msg/PointField.html
              foreach (var field in msg.fields)
                log.InfoFormat("{0} @ {1} x {3}, type id {2}", field.name, field.offset, field.datatype, field.count);

              using (System.IO.StreamWriter writer = new System.IO.StreamWriter(fi.FullName))
              {
                writer.WriteLine("{0}", msg.width * msg.height);
                writer.WriteLine("Grid size: {0} x {1}", msg.width, msg.height);
                writer.WriteLine(@"X; Y;  Z;  R;  G;  B;");

                for (int j = 0; j < msg.height; j++)
                  for (int i = 0; i < msg.row_step; i += (int)msg.point_step)
                  {
                    int ix = j * (int)msg.row_step + i;
                    float x = BitConverter.ToSingle(msg.data, ix);
                    float y = BitConverter.ToSingle(msg.data, ix + 4);
                    float z = BitConverter.ToSingle(msg.data, ix + 8);
                    byte r = msg.data[ix + 16];
                    byte g = msg.data[ix + 17];
                    byte b = msg.data[ix + 18];
                    
                    if (float.IsNaN(x) || x < 0)
                    {
                      writer.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture, @"{0:0.000}; {1:0.000};  {2:0.000};  {3};  {4};  {5};", 0, 0, 0, 0, 0, 0));
                    }
                    else
                      // NOTE: coordinates are in ROS space. Converting to VIPER space using transform: X->Z, Y->Z, Z->Y
                      writer.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture, @"{0:0.000}; {1:0.000};  {2:0.000};  {3};  {4};  {5};", y, z, x, r, g, b));
                  }
              }
            }
          });

          System.Threading.Thread.Sleep(30 * 1000); // 30 secs
          module.Disconnect();
        }
      }
      catch (Exception ex)
      {
        log.Error(ex);
      }
    }
  }
}

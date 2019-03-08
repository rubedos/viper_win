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
  /// </summary>
  class Program
  {
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    static void Main(string[] args)
    {
      try
      {
        // Specify VIPER IP here
        using (CvmDevice module = new CvmDevice("http://192.168.1.157:11311", null))
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
              log.InfoFormat("Is bigendian: {0}", msg.is_bigendian?"Yes" : "No");
              log.InfoFormat("Row step: {0}", msg.row_step);
              log.InfoFormat("Total data: {0}", msg.data.Length);
              // More information about PointField structure: http://docs.ros.org/melodic/api/sensor_msgs/html/msg/PointField.html
              foreach (var field in msg.fields)
                log.InfoFormat("{0} @ {1} x {3}, type id {2}", field.name, field.offset, field.datatype, field.count);
            }
            // Point cloud has 4x4 bytes per point: X(F32b), Y(F32b), Z(F32b) and RGB(4x8b).
            for (int j = 0; j < msg.height; j++)
              for (int i = 0; i < msg.row_step; i += 4 * (int)msg.point_step)
              {
                int ix = j * (int)msg.row_step + i;
                float x = BitConverter.ToSingle(msg.data, ix);
                float y = BitConverter.ToSingle(msg.data, ix + 4);
                float z = BitConverter.ToSingle(msg.data, ix + 8);
                byte r = msg.data[ix + 9];
                byte g = msg.data[ix + 10];
                byte b = msg.data[ix + 11];

                // TODO: process point...
                //Console.WriteLine("{0} {1} {2} ({3} {4} {5})", x, y, z, r, g, b);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ros.Net.utilities;
using Rubedos.Viper.Net;

namespace ReadTopicsSample
{
  class Program
  {
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    static void Main(string[] args)
    {
      try
      {
        // Specify VIPER IP here
        using (CvmDevice module = new CvmDevice("http://192.168.1.170:11311"))
        {
          module.RosNode = "ReadTopicsSample";
          module.Connect();
          if (!module.IsConnected)
            throw new InvalidOperationException("Cannot connect to VIPER!");

          // Each image stream is associated with specific named topic. To receive images on needs to subscribe to the topic
          // and attach a sink where images must be cached when received. Since there are several image types in .NET, use 
          // a corresponding sink type, e.g. BitmapSink for System.Drawing.Bitmap.
          // One image subscriber can have multiple sinks, if multiple types of the same topic image are needed.
          foreach (var topic in module.GetListOfImageTopics())
          {
            log.InfoFormat("Getting image from topic '{0}'", topic);
            using (ImageSubscriber<ImageHandler> subscriber = new ImageSubscriber<ImageHandler>())
            {
              BitmapSink sink = new BitmapSink();
              subscriber.AddSink(sink);
              AutoResetEvent waitHandle = new AutoResetEvent(false);
              
              // Once subsribed, this event shall notify when there are new images available in the topic. If event is not raised, it means image
              // has not been published for some reason.
              sink.Updated += (s, e) =>
              {
                log.InfoFormat("Received image of size {0}", sink.Bitmap.Size);
                waitHandle.Set();
              };

              subscriber.Subscribe(topic);
              if (!waitHandle.WaitOne(3000))
                log.WarnFormat("Topic has no images published");
            }
          }
          
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

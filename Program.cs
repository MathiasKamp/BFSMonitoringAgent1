

using System.ServiceProcess;

namespace BFSMonitoringAgent1
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            
            
            using (var service = new BfsMonitoringService())
            {
                //ServiceBase.Run(service);
                
                service.OnDebug();
            }
        }
    }
}
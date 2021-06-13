using System;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace BFSMonitoringAgent1
{
    public class BfsMonitoringService : ServiceBase
    {
        private ConfigCollector ConfigCollector = new ConfigCollector();
        private Thread Worker = null;

        public BfsMonitoringService()
        {
            ServiceName = "BFSMonitoringService_" + ConfigCollector.GetMonitoringAgentName();
            CanStop = true;
            CanShutdown = true;
        }

        protected override void OnStart(string[] args)
        {
            var start = new ThreadStart(CheckFiles);
            Worker = new Thread(start);
            Worker.Start();
        }

        private string CreateLog()
        {
            string logDirectory = ConfigCollector.GetRootDirectoryForOutput() + @"\log";
            var logFile = logDirectory + $@"\log{DateTime.Now:dd-MM-yyyy}.log";
            if (!Directory.Exists(logDirectory)) Directory.CreateDirectory(logDirectory);
            if (!File.Exists(logFile)) File.Create(logFile).Close();

            return logFile;
        }

        private void CheckFiles()
        {
            var nSleep = 0.5;

            {
                try
                {
                    while (true)
                    {
                        var log = CreateLog();

                        if (!File.Exists(log)) File.Create(log).Close();


                        using (var sw = new StreamWriter(log, true))
                        {
                            sw.WriteLine(string.Format("BFSMonitoring checked : " +
                                                       DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss") + ""));
                            sw.Close();

                            var val = CheckFileTimeStampDifference();

                            if (val.Length > 0)
                            {
                                CreateMessage(ServiceName, val, DateTime.Now.ToString("dd_MM_yyyy_hh_mm_ss"));
                            }
                        }

                        Thread.Sleep((int) ((long) (nSleep * 60 * 1000)));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        protected override void OnStop()
        {
            try
            {
                Worker.Abort();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        public bool IsWithin(int value, int minimum, int maximum)
        {
            return value >= minimum && value <= maximum;
        }


        public string CheckFileTimeStampDifference()
        {
            string val = null;
            try
            {
                string filePath = ConfigCollector.GetPathToBeChecked() + @"\";

                var oldestFile = new DirectoryInfo(filePath).GetFiles()
                    .OrderBy(f => f.LastWriteTime).First();

                if (oldestFile.Exists)
                {
                    var fileDate = oldestFile.LastWriteTime;
                    var currentDate = DateTime.Now;
                    var timeDifference = currentDate - fileDate;
                    int timeDif = timeDifference.Minutes;
                    var greenDif = ConfigCollector.GetMinuteDifferenceForGreen();
                    var yellowDif = ConfigCollector.GetMinuteDifferenceForYellow();


                    if (IsWithin(timeDif, 0, greenDif))
                    {
                        val = "green";
                        return val;
                    }

                    if (IsWithin(timeDif, greenDif, yellowDif))
                    {
                        val = "yellow";
                        return val;
                    }
                    
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return val = "red";
        }
        
        public void CreateMessage(string agentName, string statusCode, string checkedTimeStamp)
        {
            string path = ConfigCollector.GetRootDirectoryForOutput() + @"\messagesToSend";
            string message = path + $@"\{agentName}_{DateTime.Now:dd_MM_yy_HH_mm_ss}.csv";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            if (!File.Exists(message)) File.Create(message).Close();

            try
            {
                using (var sw = new StreamWriter(message, true))
                {
                    sw.WriteLine("agentName, statusCode, checkedTimeStamp");
                    sw.WriteLine(agentName + "," + statusCode + "," + checkedTimeStamp);
                    sw.Close();
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
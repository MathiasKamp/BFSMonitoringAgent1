using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace BFSMonitoringAgent1
{
    public class BfsMonitoringService : ServiceBase
    {
        private readonly ConfigCollector ConfigCollector = new ConfigCollector();
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

                            var val = GetOldestFileInDirectories();

                            DateTime? oldest = null;
                            if (val.Count > 0)
                            {
                                oldest = GetOldestFileDate(val);
                            }
                            
                            var statusCode = GetFileTimeStampStatusCode(oldest);


                            if (!string.IsNullOrEmpty(statusCode))
                            {
                                CreateMessage(ServiceName, statusCode, DateTime.Now.ToString("dd_MM_yyyy_hh_mm_ss"));
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

        private bool IsWithin(double value, double minimum, double maximum)
        {
            return value >= minimum && value <= maximum;
        }

        public List<DateTime> GetOldestFileInDirectories()
        {
            string filePath = ConfigCollector.GetPathToBeChecked();
            var filePathList = filePath.Split(',');
            var fileDates = new List<DateTime>();

            if (filePathList.Length > 0)
            {
                foreach (var dir in filePathList)
                {
                    var tempFile = new DirectoryInfo(dir);

                    if (tempFile.GetFiles().Length > 0)
                    {
                        var oldestFile = new DirectoryInfo(dir).GetFiles()
                            .OrderBy(f => f.LastWriteTime).First();

                        fileDates.Add(oldestFile.LastWriteTime);
                    }

                    else
                    {
                        fileDates.Add(DateTime.Now);
                    }
                }
            }

            return fileDates;
        }

        public DateTime GetOldestFileDate(List<DateTime> fileDates)
        {
            DateTime oldestTime = fileDates.OrderBy(a => a).First();

            return oldestTime;
        }


        private string GetFileTimeStampStatusCode(DateTime? oldestFileTimeStamp)
        {
            string val = null;
            try
            {
                var currentDate = DateTime.Now;
                TimeSpan? timeDifference = currentDate - oldestFileTimeStamp;
                if (timeDifference != null)
                {
                    var timeDif = timeDifference.Value.TotalMinutes;
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

        private void CreateMessage(string agentName, string statusCode, string checkedTimeStamp)
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
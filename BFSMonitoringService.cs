using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;


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
            var nSleep = 0.5; // run worker every 30 seconds

            {
                try
                {
                    while (true)
                    {
                        var log = CreateLog();

                        if (!File.Exists(log)) File.Create(log).Close();


                        using (var sw = new StreamWriter(log, true))
                        {
                            string agentName = ConfigCollector.GetMonitoringAgentName();
                            sw.WriteLine(agentName + " has started to check files at : " +
                                         DateTime.Now.ToString(@"dd-MM-yyyy hh:mm:ss"));
                            
                            sw.Close();
                            
                        }

                        var statusMessage = CreateStatusMessage();

                        CreateMessage(statusMessage);
                        

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

        private static bool IsWithin(double value, double minimum, double maximum)
        {
            return value >= minimum && value <= maximum;
        }

        public StatusMessage CreateStatusMessage()
        {
            var file = GetOldestFileInDirectories();
            string agentName = ConfigCollector.GetMonitoringAgentName();
            string val = null;
            if (file.Count > 0)
            {
                var oldestFile = GetOldestFile(file);

                var fileStatus = GetFileTimeStampStatusCode(oldestFile.LastWriteTime);

                return new StatusMessage(
                    agentName: agentName,
                    directory: oldestFile.DirectoryName,
                    fileName: oldestFile.Name,
                    dateChecked: DateTime.Now,
                    lastModifiedDate: Convert.ToDateTime(oldestFile.LastWriteTime),
                    status: fileStatus
                );
            }

            else
            {
                return new StatusMessage(agentName: agentName, status: GetFileTimeStampStatusCode(DateTime.Now));
            }
        }

        private List<FileInfo> GetOldestFileInDirectories()
        {
            var filePath = ConfigCollector.GetPathToBeChecked();
            var filePathList = filePath.Split(',');
            var oldFiles = new List<FileInfo>();

            if (filePathList.Length > 0)
            {
                foreach (var dir in filePathList)
                {
                    var tempFile = new DirectoryInfo(dir);

                    if (tempFile.GetFiles().Length > 0)
                    {
                        var oldestFile = new DirectoryInfo(dir).GetFiles()
                            .OrderBy(f => f.LastWriteTime).First();

                        oldFiles.Add(oldestFile);
                    }
                }
            }

            return oldFiles;
        }

        private static FileInfo GetOldestFile(List<FileInfo> files)
        {
            var oldestFile = files.OrderBy(a => a.LastWriteTime).First();

            return oldestFile;
        }


        private string GetFileTimeStampStatusCode(DateTime oldestFileTimeStamp)
        {
            string val = null;
            try
            {
                var currentDate = DateTime.Now;
                var timeDifference = currentDate - oldestFileTimeStamp;
                if (timeDifference.TotalMinutes > 0)
                {
                    var timeDif = timeDifference.TotalMinutes;
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

        private void CreateMessage(StatusMessage statusMessage)
        {
            var outPutMethod = ConfigCollector.GetOutputMethod();

            switch (outPutMethod)
            {
                case 1:
                    var path = ConfigCollector.GetRootDirectoryForOutput() + @"\messagesToSend";
                    var message = path + $@"\{statusMessage.AgentName}_{DateTime.Now:dd_MM_yy_HH_mm_ss}.csv";
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    if (!File.Exists(message)) File.Create(message).Close();

                    try
                    {
                        using (var sw = new StreamWriter(message, true))
                        {
                            if (statusMessage.FileName != null)
                            {
                                sw.WriteLine(statusMessage.MessageWithFile());
                                sw.Close();
                            }

                            else
                            {
                                sw.WriteLine(statusMessage.MessageWithDummyFile());
                                sw.Close();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                    break;
                
                
                case 2:
                    PostStatusMessage(statusMessage);
                    break;
                
            }
            
            
        }

        private async Task PostStatusMessage(StatusMessage statusMessage)
        {
            var client = new HttpClient();
            string webApiAddress = ConfigCollector.GetWebApiAddress();
            client.BaseAddress = new Uri(webApiAddress);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var content = new StringContent(JsonConvert.SerializeObject(statusMessage), Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync("BfsAgent", content);

            
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("data posted");
                }

                else
                {
                    Console.WriteLine($"Failed to post data. Status code:{response.StatusCode}"); 
                }
            
            
        }
    }
}
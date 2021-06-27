using System;

namespace BFSMonitoringAgent1
{
    public class StatusMessage
    {
        
        public string AgentName { get; private set; }

        public string Directory { get; private set; }

        public string Status { get; private set; }

        public DateTime LastModifiedDate { get; private set; }

        public DateTime DateChecked { get; private set; }

        public string FileName { get; private set; }


        public StatusMessage(string agentName, DateTime lastModifiedDate, string status, string directory, DateTime dateChecked,
            string fileName)
        {
            AgentName = agentName;
            LastModifiedDate = lastModifiedDate;
            Status = status;
            Directory = directory;
            DateChecked = dateChecked;
            FileName = fileName;

        }

        public StatusMessage(string agentName, string status)
        {
            AgentName = agentName;
            Status = status;
        }

        public string MessageWithFile()
        {
            return AgentName + "," + Directory + "," + FileName + "," + DateChecked + "," + LastModifiedDate + "," +
                   Status.ToString();
        }

        public string MessageWithDummyFile()
        {
            return AgentName + "," + "noFilesFound" + "," + Status.ToString();
        }
    }
}
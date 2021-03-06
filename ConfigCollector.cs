using System;
using System.Configuration;
using System.Text;

namespace BFSMonitoringAgent1
{
    public class ConfigCollector
    {
        private StringBuilder sb = new StringBuilder();

        public string GetWebApiAddress()
        {
            return ConfigurationSettings.AppSettings["WebApiAddress"];
        }
        
        public string GetPathToBeChecked()
        {
            return ConfigurationSettings.AppSettings["PathToBeChecked"];
        }

        public int GetOutputMethod()
        {
            return Convert.ToInt32(ConfigurationSettings.AppSettings["OutputMethod"]);
        }

        public  string GetMonitoringAgentName()
        {
            return ConfigurationSettings.AppSettings["MonitoringAgentName"];
        }
        
        
        public int GetMinuteDifferenceForGreen()
        {
            return Convert.ToInt32(ConfigurationSettings.AppSettings["MinutesToTurnGreen"]);
        }
        
        public int GetMinuteDifferenceForYellow()
        {
            return Convert.ToInt32(ConfigurationSettings.AppSettings["MinutesToTurnYellow"]);
        }
        

        public string GetRootDirectoryForOutput()
        {
            return ConfigurationSettings.AppSettings["RootDirectoryForOutput"];
        }
        
       
    }
}
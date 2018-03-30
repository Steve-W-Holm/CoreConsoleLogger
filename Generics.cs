using System;
using System.Collections;
//using System.Runtime.Remoting;
//using System.Runtime.Remoting.Channels;
//using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using System.Text;

namespace Itea.Logger
{
    public static class Generics
    {
        public static string MyPath()
        {
            string myPath = System.IO.Path.GetDirectoryName(
                       System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            return myPath.Substring(myPath.IndexOf(@"\") + 1);
        }
        public static string MyName()
        {
            string myPath = MyPath();
            return myPath.Substring(myPath.LastIndexOf(@"\") + 1);
        }

        public static string IntToZeroPadString(int intValue, int StringLength)
        {
            return Convert.ToString(intValue).PadLeft(StringLength, '0');
        }

        //public static void WriteApplicationLogError(string strErrorMessage, string strSource)
        //{
        //    Console.WriteLine(strErrorMessage);

        //    System.Diagnostics.EventLog EvLog = new System.Diagnostics.EventLog();
        //    if (!System.Diagnostics.EventLog.SourceExists("AppLog.exe"))
        //    {
        //        System.Diagnostics.EventLog.CreateEventSource("AppLog.exe", "Application");
        //    }

        //    EvLog.Source = strSource;

        //    EvLog.WriteEntry(strErrorMessage, System.Diagnostics.EventLogEntryType.Information);

        //    EvLog.Close();
        //    EvLog = null;
        //}
        //public static void WriteApplicationLogEntry(string strErrorMessage, string strSource)
        //{
        //    System.Diagnostics.EventLog EvLog = new System.Diagnostics.EventLog();
        //    if (!System.Diagnostics.EventLog.SourceExists("AppLog.exe"))
        //    {
        //        System.Diagnostics.EventLog.CreateEventSource("AppLog.exe", "Application");
        //    }

        //    EvLog.Source = strSource;

        //    EvLog.WriteEntry(strErrorMessage, System.Diagnostics.EventLogEntryType.Information);

        //    EvLog.Close();
        //    EvLog = null;
        //}

        public static bool HasABitMatch(long lngOne, long lngTwo)
        {
            return System.Convert.ToBoolean(lngOne & lngTwo);
        }

    }
}

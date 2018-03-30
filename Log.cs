using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Itea.Logger
{
    public delegate void ExitAppRequestHandler();

    /// <summary>
    /// Static implementation of the ConsoleLogger methods
    /// </summary>
    public class Log
    {
        private static Logger _log;
        public static ExitAppRequestHandler OnExitAppRequest;
        
        // Can only be instantiated by ConsoleLogger itself
        internal Log(Logger log)
        {
            _log = log;
        }
        public static void Dispose()
        {
            if (_log != null)
                _log.Dispose();

            _log = null;
        }

        private void _log_OnExitAppRequest()
        {
            OnExitAppRequest?.Invoke();
        }

        public static void LogMessage(string strMessage)
        {
            if (_log != null)
                _log.LogMessage(strMessage);
            else
                throw new Exception("ConsoleLogger has not been instantiated.");
        }
        public static void LogMessage(string strMessage, MessageType messageType)
        {
            if (_log != null)
                _log.LogMessage(strMessage, messageType);
            else
                Console.WriteLine(strMessage);

//                throw new Exception("ConsoleLogger has not been instantiated.");
        }
        public static void LogMessage(string strMessage, MessageType messageType, bool ForceFileWrite)
        {
            if (_log != null)
                _log.LogMessage(strMessage, messageType, ForceFileWrite);
            else
                throw new Exception("ConsoleLogger has not been instantiated.");
        }
        public static void LogMessage(string strMessage, MessageType messageType, Exception ex)
        {
            if (_log != null)
                _log.LogMessage(strMessage, messageType, ex);
            else
                throw new Exception("ConsoleLogger has not been instantiated.");
        }

        public static void RegisterCommand(ConsoleCommandDelegate CallbackMethod, string CommandName, string Description, string UsageString)
        {
            if (_log != null)
                _log.RegisterCommand(CallbackMethod, CommandName, Description, UsageString);
            else
                throw new Exception("ConsoleLogger has not been instantiated.");
        }
        public static void RegisterCommand(ConsoleCommandDelegate CallbackMethod, string CommandName, string Description)
        {
            if (_log != null)
                _log.RegisterCommand(CallbackMethod, CommandName, Description);
            else
                throw new Exception("ConsoleLogger has not been instantiated.");
        }

        //public static string CmdShowMessageTypes(string[] args)
        //{
        //    return _log.CmdShowMessageTypes(args);
        //}

    }
}

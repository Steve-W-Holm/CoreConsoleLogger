using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.IO.Pipes;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Itea.Logger
{
    public class Logger : IDisposable
    {
        #region Parameters
        private bool disposed;
        private long ConsoleTracing = 0xFFFFFFFF;
        private long LogFileTracing = 0xFFFFFFFF;
        private long PipeTracing =    0xFFFFFFFF;   // <-- NEW
        private int HoursToKeepLogFiles = 168; // 1 week
        private string strCurrentLogFileName = "";
        private System.Text.StringBuilder sbLogMessages;
        private string strMyName;
        private string strMyPath;
        //private LoggerConfiguration _logConfig;
        private System.Timers.Timer tmrDeleteOldLogFiles;
        private IsWritingLogFile isWritingLogFile = new IsWritingLogFile();
        private ArrayList _pipes;               // <-- NEW
        private int _pipeCount;                 // <-- NEW
        private static int _maxPipeCount = 5;   // <-- NEW
        private ArrayList _commands;            // <-- NEW
        private bool _includePipes;
        private Type _messageType;
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the Logger for operation. The Logger instance is added as static member to the Log class and accessible from static methods.
        /// </summary>
        /// <param name="MyName">Application name</param>
        /// <param name="MyPath">Application location</param>
        /// <param name="messageType">Passing an extended or derived MessageType value will force the values to instantiate</param>
        public Logger(string MyName, string MyPath, MessageType messageType) : this(MyName, MyPath, messageType, true) { }
 
        /// <summary>
        /// Initializes the Logger for operation. The Logger instance is added as static member to the Log class and accessible from static methods.
        /// </summary>
        /// <param name="MyName">Application name</param>
        /// <param name="MyPath">Application location</param>
        /// <param name="messageType">Passing an extended or derived MessageType value will force the values to instantiate</param>
        /// <param name="IncludePipes">Indicate whether remote pipe messaging will be enabled. Default is 'true'.</param>
        public Logger(string MyName, string MyPath, MessageType messageType, bool IncludePipes)
        {
            if (messageType == null) 
                throw new Exception("Oops!");
            else                
                _messageType = messageType.GetType();

            strMyName = MyName;
            strMyPath = MyPath;
            sbLogMessages = new StringBuilder(50000, 250000);
            //_logConfig = new LoggerConfiguration();
            CheckForLogFolder();
            UpdateConfiguration();
            StartTimers();

            LogMessage("Logger - Configured Logger on ThreadID:" + Thread.CurrentThread.ManagedThreadId, MessageType.Threading);

            _includePipes = IncludePipes;
            if (_includePipes)
            {
                _pipes = new ArrayList(_maxPipeCount);  // <-- NEW
                StartNamedPipeServers();

                _commands = new ArrayList();            // <-- NEW
                RegisterLoggerCommands();
            }

            // Assign to static Log
            Log l = new Log(this);

            //SaveConfigFile(); // Make sure it contains the newest message types
        }

        public Logger(string MyName, string MyPath, MessageType messageType, object app)
        {
            if (messageType == null)
                throw new Exception("Oops!");
            else
                _messageType = messageType.GetType();

            strMyName = MyName;
            strMyPath = MyPath;
            sbLogMessages = new StringBuilder(50000, 250000);
            //_logConfig = new LoggerConfiguration();
            CheckForLogFolder();
            UpdateConfiguration();
            StartTimers();

            LogMessage("Logger - Configured Logger on ThreadID:" + Thread.CurrentThread.ManagedThreadId, MessageType.Threading);

            _includePipes = true;
            if (_includePipes)
            {
                _pipes = new ArrayList(_maxPipeCount);
                StartNamedPipeServers();

                _commands = new ArrayList();
                RegisterLoggerCommands(app);
            }

            // Assign to static Log
            Log l = new Log(this);


            //SaveConfigFile(); // Make sure it contains the newest message types
        }

        public Logger(object app) : this(Generics.MyName(), Generics.MyPath(), app) { }

        public Logger(string MyName, string MyPath, object app)
        {
            strMyName = MyName;
            strMyPath = MyPath;
            sbLogMessages = new StringBuilder(50000, 250000);
            //_logConfig = new LoggerConfiguration();
            CheckForLogFolder();
            UpdateConfiguration();
            StartTimers();

            LogMessage("Logger - Configured Logger on ThreadID:" + Thread.CurrentThread.ManagedThreadId, MessageType.Threading);

            _includePipes = true;
            if (_includePipes)
            {
                _pipes = new ArrayList(_maxPipeCount);
                StartNamedPipeServers();

                _commands = new ArrayList();
                RegisterLoggerCommands(app);
            }

            // Assign to static Log
            Log l = new Log(this);

            Log.LogMessage("Setup", MessageType.Status, true);
            //SaveConfigFile(); // Make sure it contains the newest message types
        }
        #endregion

        ~Logger()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    sbLogMessages = null;
                    //_logConfig = null;
                    //                    tmrLoadConfiguration.Enabled = false;
                    //                    tmrLoadConfiguration = null;
                    // Release Managed Resources.
                }
                // Release unmanaged resources.				
            }
            disposed = true;
        }

        private class IsWritingLogFile
        {
            private bool isWritingLogFile = false;

            public bool Currently
            {
                get { return isWritingLogFile; }
                set { isWritingLogFile = value; }
            }

        }
        private bool ShouldWriteLogFile()
        {
            if (!isWritingLogFile.Currently)
            {
                isWritingLogFile.Currently = true;
                return true;
            }
            else return false;
        }

        private void StartTimers()
        {
            tmrDeleteOldLogFiles = new System.Timers.Timer(3600000); // every hour
            tmrDeleteOldLogFiles.Elapsed += new System.Timers.ElapsedEventHandler(tmrDeleteOldLogFiles_Elapsed);
            tmrDeleteOldLogFiles.Enabled = true;
        }
        private void tmrDeleteOldLogFiles_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            DeleteOldLogFiles();
        }
        private void DeleteOldLogFiles()
        {
            System.IO.DirectoryInfo oLogDirectory = new System.IO.DirectoryInfo(RootFolderName + @"\log");
            System.IO.FileInfo[] oFiles = oLogDirectory.GetFiles();

            for (int i = 0; i < oFiles.Length - 1; i++)
            {
                if (oFiles[i].LastWriteTime < DateTime.Now.AddHours(-HoursToKeepLogFiles))
                {
                    //LogMessage("Deleting an old logfile: " + oFiles[i].Name, LogMessageType.LogFile);
                    LogMessage("Logger - Deleting an old logfile: " + oFiles[i].Name, MessageType.LogFile);
                    oFiles[i].Delete();
                }
            }
        }

        public void LogMessage(string strMessage, MessageType messageType, bool ForceFileWrite)
        {
            try
            {
                if (Generics.HasABitMatch(messageType, ConsoleTracing))
                {
                    string s = GetFormattedMessage(strMessage, messageType);
                    Console.WriteLine(s);
                }

                // Named Pipe output    // <-- NEW
                if (_includePipes && Generics.HasABitMatch(messageType, PipeTracing))
                {
                    if (_pipes != null)
                    {
                        foreach (ConsolePipe cp in _pipes)
                        {
                            if (cp.State == PipeState.OutputMode)
                            {
                                cp.WriteLine(GetFormattedMessage(strMessage, messageType));
                            }
                        }
                    }
                }

                if (Generics.HasABitMatch(messageType, LogFileTracing))
                {
                    lock (sbLogMessages)
                    {
                        sbLogMessages.Append(GetFormattedMessage(strMessage + "\r\n", messageType));
                    }
                }

                if (sbLogMessages.Length > 150000)
                {
                    lock (sbLogMessages)
                    {
                        sbLogMessages.Remove(0, 20000);
                        //sbLogMessages.Append(GetFormattedMessage(" Log Buffer exceed 50000 bytes. Deleted (lost) 20000 bytes.\r\n", LogMessageType.Error));
                        sbLogMessages.Append(GetFormattedMessage(" Log Buffer exceed 50000 bytes. Deleted (lost) 20000 bytes.\r\n", MessageType.Error));
                    }

                }
                else if ((sbLogMessages.Length > 5000 || ForceFileWrite))  // Lets flush the Log Message and write to file using a new thread that leaves our app available for other logs
                {
                    bool shouldWriteLogFile;

                    lock (isWritingLogFile)
                    {
                        shouldWriteLogFile = ShouldWriteLogFile();
                    }

                    if (shouldWriteLogFile)
                    {
                        //string strCurMessage = GetFormattedMessage("Beginning to write Log File.", LogMessageType.LogFile);
                        string strCurMessage = GetFormattedMessage("Logger - Beginning to write Log File.", MessageType.LogFile);
                        sbLogMessages.Append(strCurMessage + "\r\n");

                        if (Generics.HasABitMatch((long)MessageType.LogFile, ConsoleTracing))
                        {
                            Console.WriteLine(strCurMessage);
                        }

                        System.Threading.Thread threadLogMessage =
                            new Thread(new System.Threading.ThreadStart(WriteLogFile));
                        LogMessage("Logger - ThreadID:" + Thread.CurrentThread.ManagedThreadId + " starting a new ThreadID:" + threadLogMessage.ManagedThreadId + " Method:WriteLogFile()", MessageType.Threading);
                        threadLogMessage.Start();
                    }

                }
            }
            catch (Exception ex)
            {
                //Generics.WriteApplicationLogError(GetFormattedMessage(" Error in LogMessage(). Info: " + ex.Message, LogMessageType.Error), strMyName);
                //Generics.WriteApplicationLogError(GetFormattedMessage(" Error in LogMessage(). Info: " + ex.Message, MessageType.Error), strMyName);
            }
        }
        public void LogMessage(string strMessage, MessageType messageType)
        {
            LogMessage(strMessage, messageType, false);
        }
        public void LogMessage(string strMessage, MessageType messageType, bool ForceFileWrite, bool WriteAppLogEntry)
        {
            //if (WriteAppLogEntry)
                //Generics.WriteApplicationLogEntry(GetFormattedMessage(strMessage, messageType), strMyName);

            LogMessage(strMessage, messageType, ForceFileWrite);
        }
        public void LogMessage(MessageType messageType, string strMessage, params object[] args)
        {
            LogMessage(String.Format(strMessage, args), messageType, false);
        }
        public void LogMessage(string strMessage, MessageType messageType, Exception ex)
        {
            LogMessage(strMessage + " Details: " + GetExceptionMessage(ex, true), messageType, false);
        }
        public void LogMessage(string strMessage)
        {
            LogMessage(strMessage, MessageType.Default, false);
        }
        
        public static string GetExceptionMessage(Exception ex, bool ShowStackTrace)
        {
            System.Text.StringBuilder sb = new StringBuilder(1000);

            string stackTrace = ex.StackTrace;

            while (ex != null)
            {
                sb.AppendLine(ex.Message + "\r\n");
                //sb.AppendLine();
                ex = ex.InnerException;
            }

            if (ShowStackTrace) sb.AppendLine("StackTrace:" + stackTrace);

            return sb.ToString();

        }
        private string GetFormattedMessage(string strMessage, MessageType messageType)
        {
            DateTime now = DateTime.Now;
            //            return now.ToString().Insert(now.ToString().Length - 3, "." + now.Millisecond.ToString().PadLeft(3,'0')) + ": " + GetLogMessageString(lmtCurrentMessageType) + " " + strMessage;
            return now.ToString("M/d/yy HH:mm:ss.fff") + ": " + messageType.Character + " " + strMessage;
        }
        
        private void CheckForLogFolder()
        {
            if (!System.IO.Directory.Exists(strMyPath + @"\log"))
            {
                System.IO.Directory.CreateDirectory(strMyPath + @"\log");
            }
        }
        private string GetLogFileName()
        {
            return String.Format(RootFolderName + @"\log\{0}_", strMyName) + DateTime.Now.Year + "_" + Generics.IntToZeroPadString(DateTime.Now.Month, 2) + "_" + Generics.IntToZeroPadString(DateTime.Now.Day, 2) + "(" + Generics.IntToZeroPadString(DateTime.Now.Hour, 2) + "_" + Generics.IntToZeroPadString(DateTime.Now.Minute, 2) + ").txt";
        }
        public string RootFolderName
        {
            get { return strMyPath; }
        }

        private void WriteLogFile()
        {
            int intTicksAtStart = Environment.TickCount;

            try
            {
                if (strCurrentLogFileName == "")
                {
                    strCurrentLogFileName = GetLogFileName();
                }
                else
                {
                    try
                    {
                        System.IO.FileInfo objFileInfo = new System.IO.FileInfo(strCurrentLogFileName);

                        if (objFileInfo != null)
                        {
                            if (objFileInfo.Length >= 50000000)
                            {
                                strCurrentLogFileName = GetLogFileName();
                            }

                            objFileInfo = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        string strCurMessage = "Error finding file: " + strCurrentLogFileName + " info:" + ex.Message;
                        sbLogMessages.Append(strCurMessage + "\r\n");
                        Console.WriteLine(strCurMessage);
                    }
                }

                System.IO.FileStream fs = new System.IO.FileStream(strCurrentLogFileName, System.IO.FileMode.Append, System.IO.FileAccess.Write, System.IO.FileShare.None);

                byte[] bytLogMessage;
                lock (sbLogMessages)
                {
                    bytLogMessage = System.Text.ASCIIEncoding.UTF8.GetBytes(sbLogMessages.ToString());
                    sbLogMessages.Remove(0, sbLogMessages.Length);
                }

                fs.Write(bytLogMessage, 0, bytLogMessage.Length);

                fs.Close();
                fs = null;

                string strCurLogMessage = "Logger - Finished writing Log File " + strCurrentLogFileName + ". It took " + (Environment.TickCount - intTicksAtStart) + " ticks";
                //LogMessage(strCurLogMessage, LogMessageType.LogFile);
                LogMessage(strCurLogMessage, MessageType.LogFile);
            }
            catch (Exception ex)
            {
                //string strErrorMessage = GetFormattedMessage(strMyName + " Error! Failure writing log file.  Info:" + ex.Message, LogMessageType.Error);
                string strErrorMessage = GetFormattedMessage(strMyName + " Error! Failure writing log file.  Info:" + ex.Message, MessageType.Error);

                if (!(sbLogMessages == null)) sbLogMessages.Append(strErrorMessage + "\r\n");

                //Generics.WriteApplicationLogError(strErrorMessage, strMyName);
            }

            isWritingLogFile.Currently = false;

            LogMessage("Logger - Thread complete - ThreadID:" + Thread.CurrentThread.ManagedThreadId + " Method:WriteLogFile()", MessageType.Threading);
        }
        
        private void UpdateConfiguration()
        {
            try
            {
                //_logConfig = new LoggerConfiguration();
                //UpdateMessageTypesInConfig();

                //if (!File.Exists(RootFolderName + @"\Logger.config"))   // Create new logger.config file
                //{
                //    LoggerConfiguration.TracingRow newRow = _logConfig.Tracing.NewTracingRow();
                //    newRow.LogFileTracing = LogFileTracing;
                //    newRow.ConsoleTracing = ConsoleTracing;
                //    newRow.PipeTracing = PipeTracing;
                //    newRow.HoursToHoldLogFiles = HoursToKeepLogFiles;
                //    //_logConfig.Tracing.AddTracingRow(newRow);
                //    //_logConfig.AcceptChanges();

                //    UpdateMessageTypesInConfig();

                //    //_logConfig.WriteXml(RootFolderName + @"\Logger.config", System.Data.XmlWriteMode.IgnoreSchema); // Create file

                //    //LogMessage(strMyName + " succeeded creating initial Logger.config file", LogMessageType.Configuration, false);
                //    LogMessage(strMyName + " succeeded creating initial Logger.config file", MessageType.Configuration, true);
                //    LogMessage("Current Configuration| LogFileTracing:" + LogFileTracing.ToString() +
                //        "; ConsoleTracing:" + ConsoleTracing.ToString() +
                //        "; HoursToKeepLogFiles:" + HoursToKeepLogFiles.ToString() + ";"
                //        , MessageType.Configuration, false);
                //}
                //else                                                    // Load existing Logger.config file
                //{
                //    _logConfig.ReadXml(RootFolderName + @"\Logger.config", System.Data.XmlReadMode.IgnoreSchema);
                //    if (_logConfig.Tracing.Rows.Count > 1)
                //    {
                //        _logConfig.Tracing[0].Delete();
                //    }
                //    _logConfig.AcceptChanges();

                //    LoggerConfiguration.TracingRow tr = _logConfig.Tracing[0];

                //    LogFileTracing = tr.LogFileTracing;
                //    ConsoleTracing = tr.ConsoleTracing;
                //    PipeTracing = tr.PipeTracing;
                //    HoursToKeepLogFiles = tr.HoursToHoldLogFiles;


                LogFileTracing = 0xFFFFFFFFF;
                ConsoleTracing = 0xFFFFFFFFF;
                PipeTracing = 0xFFFFFFFFF;
                HoursToKeepLogFiles = 168;


                //    //LogMessage(strMyName + " succeeded loading Logger.config into memory", LogMessageType.Configuration, false);
                //    LogMessage(strMyName + " succeeded loading Logger.config into memory", MessageType.Configuration, true);
                //    LogMessage("Current Configuration| LogFileTracing:" + LogFileTracing.ToString() +
                //        "; ConsoleTracing:" + ConsoleTracing.ToString() +
                //        "; HoursToKeepLogFiles:" + HoursToKeepLogFiles.ToString() + ";"
                //        , MessageType.Configuration, false);

                //    // refresh MessageTypes and save to file
                //    if (_logConfig.MessageType.Count < 1)
                //    {
                //        //SaveConfigFile();
                //    }

                //}
            }
            catch (Exception ex)
            {
                string strError = strMyName + " failed loading Logger.config. All Tracing will be left on. Will keep Log files for " + HoursToKeepLogFiles + " hours. Info:" + ex.Message;
                //LogMessage(strError, LogMessageType.Error, false);
                LogMessage(strError, MessageType.Error, false);
            }
        }
        //private void UpdateMessageTypesInConfig()
        //{
        //    try
        //    {
        //        _logConfig.MessageType.Clear();

        //        IList<FieldInfo> members = new List<FieldInfo>(_messageType.GetFields());
        //        IList<FieldInfo> baseMembers = new List<FieldInfo>(_messageType.BaseType.GetFields());

        //        foreach (FieldInfo member in baseMembers)
        //        {
        //            if (member.FieldType == typeof(MessageType))
        //            {
        //                MessageType mt = (MessageType)member.GetValue(null);
        //                LoggerConfiguration.MessageTypeRow newRow = _logConfig.MessageType.NewMessageTypeRow();
        //                newRow.Name = mt.Name;
        //                newRow.Character = mt.Character;
        //                newRow.Value = mt.Value;

        //                _logConfig.MessageType.AddMessageTypeRow(newRow);
        //            }
        //        }

        //        foreach (FieldInfo member in members)
        //        {
        //            if (member.FieldType == _messageType)
        //            {
        //                MessageType mt = (MessageType)member.GetValue(null);
        //                LoggerConfiguration.MessageTypeRow newRow = _logConfig.MessageType.NewMessageTypeRow();
        //                newRow.Name = mt.Name;
        //                newRow.Character = mt.Character;
        //                newRow.Value = mt.Value;

        //                _logConfig.MessageType.AddMessageTypeRow(newRow);
        //            }
        //        }

        //        _logConfig.MessageType.AcceptChanges();
        //    }
        //    catch (Exception ex)
        //    {
        //        LogMessage("Error while updating MessageTypes in Config. ERROR: " + ex.Message, MessageType.Error, false);
        //    }
        //}

        //private void SaveConfigFile()
        //{
        //    try
        //    {
        //        LoggerConfiguration.TracingRow tr = _logConfig.Tracing[0];

        //        tr.LogFileTracing = LogFileTracing;
        //        tr.ConsoleTracing = ConsoleTracing;
        //        tr.PipeTracing = PipeTracing;

        //        UpdateMessageTypesInConfig();

        //        _logConfig.AcceptChanges();

        //        _logConfig.WriteXml(RootFolderName + @"\Logger.config", System.Data.XmlWriteMode.IgnoreSchema);
        //    }
        //    catch (Exception ex)
        //    {
        //        LogMessage("Logger - Error saving Logger configuration file. Error:" + ex.Message, MessageType.Error);
        //    }
        //}

        #region Named Pipe Methods

        private void StartNamedPipeServers()
        {
            Thread serverThread = new Thread(ManagePipes);
            LogMessage("Logger - ThreadID:" + Thread.CurrentThread.ManagedThreadId + " starting a new ThreadID:" + serverThread.ManagedThreadId + " Method:ManagePipes()", MessageType.Threading);
            serverThread.Start();

            //LogMessage("Started new Pipe Thread on ID:" + serverThread.ManagedThreadId, MessageType.Threading);
            //LogMessage("This Thread is still ID:" + Thread.CurrentThread.ManagedThreadId, MessageType.Threading);

            //Thread.Sleep(3000);  // wait for output pipes to set up    Set to 10 seconds for implementation.
        }
        private void ManagePipes()
        {
            ArrayList completedPipes;
            int waitingPipes;
            while (!disposed)
            {
                completedPipes = new ArrayList();
                waitingPipes = 0;

                lock (_pipes)
                {
                    foreach (ConsolePipe cp in _pipes)
                    {
                        switch (cp.State)
                        {
                            case PipeState.Closed:
                                cp.Dispose();
                                //LogMessage("Marking Pipe for removal. ID:" + cp.ID.ToString(), LogMessageType.Pipe);
                                LogMessage("Logger - Marking Pipe for removal. ID:" + cp.ID.ToString(), MessageType.Pipe);
                                completedPipes.Add(cp);
                                break;
                            case PipeState.Waiting:
                                waitingPipes++;

                                if (waitingPipes > 1)    // Only need 1 pipe waiting.
                                {
                                    //LogMessage("Too many pipes open.  Marking pipe for removal. ID:" + cp.ID.ToString(), LogMessageType.Pipe);
                                    LogMessage("Logger - Too many pipes open.  Marking pipe for removal. ID:" + cp.ID.ToString(), MessageType.Pipe);
                                    cp.Dispose();
                                    completedPipes.Add(cp);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    // remove Pipes that were marked for removal
                    foreach (ConsolePipe cp in completedPipes)
                    {
                        //LogMessage("Removing Pipe from memory ID:" + cp.ID.ToString(), LogMessageType.Pipe);
                        LogMessage("Logger - Removing Pipe from memory ID:" + cp.ID.ToString(), MessageType.Pipe);
                        cp.Close();
                        cp.Dispose();
                        _pipes.Remove(cp);
                        _pipeCount--;
                    }

                    // Open another pipe if needed.  Keep 1 pipe waiting for connection at all times
                    if (waitingPipes < 1 && _pipeCount < _maxPipeCount)
                    {
                        ConsolePipe pipe = new ConsolePipe(strMyName);
                        pipe.CmdReceived += CommandReceived;
                        //pipe.LogMessage += LogMessage;
                        pipe.LogMessageNew += LogMessage;
                        //LogMessage("Starting another open Pipe:" + pipe.ID.ToString(), LogMessageType.Pipe);
                        LogMessage("Logger - Starting another open Pipe:" + pipe.ID.ToString(), MessageType.Pipe);

                        Thread serverThread = new Thread(pipe.Start);
                        LogMessage("Logger - ThreadID:" + Thread.CurrentThread.ManagedThreadId + " started a new ThreadID:" + serverThread.ManagedThreadId + " Method:pipe.Start()", MessageType.Threading);
                        serverThread.Start();
                        //LogMessage("ThreadID:" + Thread.CurrentThread.ManagedThreadId + " started a new ThreadID:" + serverThread.ManagedThreadId, MessageType.Threading);

                        _pipes.Add(pipe);
                        _pipeCount++;
                    }
                }

                Thread.Sleep(1001);
            }

            LogMessage("Logger - Thread complete - ThreadID:" + Thread.CurrentThread.ManagedThreadId + " Method:ManagePipes()", MessageType.Threading);
        }

        private void RegisterLoggerCommands(object app = null)
        {
            // Following commands are handled by the PipeClient app or ConsolePipe class.
            _commands.Add(new ConsoleCommand(null, "stop", "Stop logging realtime traces to client", "stop (or any key, while traces are enabled"));
            _commands.Add(new ConsoleCommand(null, "start", "Start logging realtime traces to client", "start"));
            _commands.Add(new ConsoleCommand(null, "exit", "Exits the PipeClient application", "exit"));
            _commands.Add(new ConsoleCommand(null, "connect", "Establishes connection between PipeClient and the application", "connect"));

            // Commands handled by me (ConsoleLogger).
            _commands.Add(new ConsoleCommand(CmdHelp, "Help", "Display available commands"));
            _commands.Add(new ConsoleCommand(CmdHelp, "?", "Shortcut to display available commands"));
            _commands.Add(new ConsoleCommand(CmdDisplayTracingLevels, "ltrace", "Display the trace levels for Logging", "ltrace [file|console|pipe]"));
            _commands.Add(new ConsoleCommand(CmdSetTracingLevels, "trace", "Set trace levels for Logging", "trace file|console|pipe [MessageType|wildcard] [\\on|\\off]"));
            _commands.Add(new ConsoleCommand(CmdSaveConfigFile, "save", "Save logging configuration to file", "save"));
            _commands.Add(new ConsoleCommand(CmdReloadLogConfig, "reload", "Reloads Logger settings from configuration to file", "reload"));
            _commands.Add(new ConsoleCommand(CmdKillAllPipes, "killpipes", "Kills all open Pipe connections from clients", "killpipes"));
            _commands.Add(new ConsoleCommand(CmdClosePipe, "close", "Closes the PipeClient application", "close [ClientID]"));
            _commands.Add(new ConsoleCommand(CmdPipeStatusSummary, "PipeStatus", "Displays current status summary for all Named Pipes in use"));
            _commands.Add(new ConsoleCommand(CmdFlushLogs, "FlushLog", "Forces the log file to be written."));
            _commands.Add(new ConsoleCommand(CmdLogDuration, "logduration", "Displays or updates the length, in hours, that logfiles will be retained on disk.","logduration [duration]"));
            _commands.Add(new ConsoleCommand(CmdTraceTypes, "tracetypes", "Displays the available message types, character identifier and value."));

            // Use Reflection to Auto Register application commands
            if (app != null)
                AutoRegisterAppCommands(app);

            _commands.Sort();
        }
        private void AutoRegisterAppCommands(object app)
        {
            Type type = app.GetType();

            //Log.Write(MessageType.Debugging, "Registering Commands for {0}...", type.Name);
            Log.LogMessage("Registering Commands for " + type.Name, MessageType.Status);
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            if (app == null) bindingFlags |= System.Reflection.BindingFlags.Static;
            else bindingFlags |= System.Reflection.BindingFlags.Instance;

            var methods = type.GetMethods(bindingFlags);
            foreach (System.Reflection.MethodInfo mInfo in methods)
            {
                try
                {
                    ConsoleCommandAttribute attr = (ConsoleCommandAttribute)Attribute.GetCustomAttribute(mInfo, typeof(ConsoleCommandAttribute));
                    if (attr == null) continue;

                    if (string.IsNullOrEmpty(attr.CommandName)) attr.CommandName = mInfo.Name; // Use the method name as the default CommandName

                    Delegate d = null;
                    if (mInfo.IsStatic) d = Delegate.CreateDelegate(typeof(ConsoleCommandDelegate), type, mInfo.Name); //get the delegate that will be called
                    else if (app != null) d = Delegate.CreateDelegate(typeof(ConsoleCommandDelegate), app, mInfo.Name); //get the delegate that will be called


                    if (!string.IsNullOrEmpty(attr.UsageString)) _commands.Add(new ConsoleCommand((ConsoleCommandDelegate)d, attr.CommandName, attr.Description, attr.UsageString));
                    else _commands.Add(new ConsoleCommand((ConsoleCommandDelegate)d, attr.CommandName, attr.Description));
                }
                catch (Exception ex)
                {
                    LogMessage("Cannot register command " + type.Name + "." + mInfo.Name + ". Error: " + ex.Message, MessageType.Error);
                }
            }

            _commands.Sort();

            Log.LogMessage("Commands Registered for " + type.Name, MessageType.Status);

        }

        public void RegisterCommand(ConsoleCommandDelegate CallbackMethod, string CommandName, string Description, string UsageString)
        {
            ConsoleCommand cc = new ConsoleCommand(CallbackMethod, CommandName, Description, UsageString);
            if (!_commands.Contains(cc))
            {
                _commands.Add(cc);
                _commands.Sort();
            }
            else
            {
                LogMessage("Logger - Command already exists: '" + cc.CommandName, MessageType.MinorError);
            }
        }
        public void RegisterCommand(ConsoleCommandDelegate CallbackMethod, string CommandName, string Description)
        {
            ConsoleCommand cc = new ConsoleCommand(CallbackMethod, CommandName, Description);
            if (!_commands.Contains(cc))
            {
                _commands.Add(cc);
                _commands.Sort();
            }
        }

        private string CommandReceived(string str)
        {
            string retval = "";
            LogMessage("Logger - Received Command from Pipe Command:" + str, MessageType.Cmd);
            LogMessage("Logger - Received Pipe Command on ThreadID:" + Thread.CurrentThread.ManagedThreadId, MessageType.Cmd);

            try
            {
                string cmd = str;
                string[] args = null;
                if (str.IndexOf(' ') > 0)   // If command has parameters, break them up
                {
                    cmd = str.Substring(0, str.IndexOf(' '));
                    //args = str.Substring(str.IndexOf(' ') + 1).Split(' ');

                    // Split arguments on whitespace, except if quoted:
                    args = Regex.Matches(str.Substring(str.IndexOf(' ') + 1), @"[\""].+?[\""]|[^ ]+")
                        .Cast<Match>()
                        .Select(m => m.Value)
                        .ToList().ToArray<string>();

                    // Cleanup the quotes
                    for (int i = 0; i < args.Length; i++)
                    {
                        args[i] = args[i].Replace("\"", "");
                    }
                }



                // Check for help(?) flag
                if (args != null && (str.Contains("/?") || str.Contains("help")))
                {
                    //ConsolePipe cp = (ConsolePipe)_pipes[_pipes.IndexOf(new ConsolePipe(PipeID))];
                    if (_commands != null)
                    {
                        retval = "Unrecognized Command: '" + cmd + "', try again.";
                        foreach (ConsoleCommand cc in _commands)
                        {
                            if (cc.CommandName.ToLower() == cmd.ToLower())
                                retval = cc.UsageString;
                        }
                    }
                    else
                    {
                        retval = "Logger not yet configured\r\n";
                    }
                }
                else
                {
                    retval = "Logger not yet configured\r\n";
                    if (_commands != null && _commands.Count > 0)
                    {
                        retval = "Unrecognized Command: '" + cmd + "'";
                        foreach (ConsoleCommand cc in _commands)
                        {
                            if (cc.CommandName.ToLower() == cmd.ToLower())
                                if (cc.CallbackMethod != null)
                                    retval = cc.CallbackMethod(args);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                LogMessage("Logger - Error processing Cmd. Error: " + ex.Message, MessageType.Error);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(retval);
            sb.AppendLine(";"); // command terminating line
            return sb.ToString();

        }

        private string CmdHelp(string[] args)
        {
            StringBuilder sb = new StringBuilder();
            foreach (ConsoleCommand cc in _commands)
            {
                sb.AppendLine(cc.CommandName.PadRight(18) + " - " + cc.Description);
            }
            return sb.ToString();
        }
        private string CmdDisplayTracingLevels(string[] args)
        {
            StringBuilder sb = new StringBuilder();
            if (args == null)
            {
                sb.AppendLine("Console: 0x" + ConsoleTracing.ToString("X") + " (" + ConsoleTracing.ToString() + ")");
                sb.AppendLine("File:    0x" + LogFileTracing.ToString("X") + " (" + LogFileTracing.ToString() + ")");
                sb.AppendLine("Pipe:    0x" + PipeTracing.ToString("X") + " (" + PipeTracing.ToString() + ")");
            }
            else
            {
                switch (args[0].ToLower())
                {
                    case "console":
                        //foreach (long val in Enum.GetValues(typeof(LogMessageType)))
                        //{
                        //    if((ConsoleTracing & val) > 0)
                        //        sb.AppendLine(Enum.GetName(typeof(LogMessageType), val).PadRight(16) + GetLogMessageString((LogMessageType)val) + " - On");
                        //    else
                        //        sb.AppendLine(Enum.GetName(typeof(LogMessageType), val).PadRight(16) + GetLogMessageString((LogMessageType)val) + " - Off");
                        //}
                        foreach (MessageType mType in MessageType.GetValues())
                        {
                            if ((ConsoleTracing & mType) > 0)
                                sb.AppendLine(mType.Name.PadRight(16) + mType.Character + " - On");
                            else
                                sb.AppendLine(mType.Name.PadRight(16) + mType.Character + " - Off");
                        }
                        break;
                    case "file":
                        //foreach (long val in Enum.GetValues(typeof(LogMessageType)))
                        //{
                        //    if((LogFileTracing & val) > 0)
                        //        sb.AppendLine(Enum.GetName(typeof(LogMessageType), val).PadRight(16) + GetLogMessageString((LogMessageType)val) + " - On");
                        //    else
                        //        sb.AppendLine(Enum.GetName(typeof(LogMessageType), val).PadRight(16) + GetLogMessageString((LogMessageType)val) + " - Off");
                        //}
                        foreach (MessageType mType in MessageType.GetValues())
                        {
                            if ((LogFileTracing & mType) > 0)
                                sb.AppendLine(mType.Name.PadRight(16) + mType.Character + " - On");
                            else
                                sb.AppendLine(mType.Name.PadRight(16) + mType.Character + " - Off");
                        }

                        break;
                    case "pipe":
                        //foreach (long val in Enum.GetValues(typeof(LogMessageType)))
                        //{
                        //    if((PipeTracing & val) > 0)
                        //        sb.AppendLine(Enum.GetName(typeof(LogMessageType), val).PadRight(16) + GetLogMessageString((LogMessageType)val) + " - On");
                        //    else
                        //        sb.AppendLine(Enum.GetName(typeof(LogMessageType), val).PadRight(16) + GetLogMessageString((LogMessageType)val) + " - Off");
                        //}
                        foreach (MessageType mType in MessageType.GetValues())
                        {
                            if ((PipeTracing & mType) > 0)
                                sb.AppendLine(mType.Name.PadRight(16) + mType.Character + " - On");
                            else
                                sb.AppendLine(mType.Name.PadRight(16) + mType.Character + " - Off");
                        }

                        break;
                    default:
                        break;
                }
            }

            sb.AppendLine("");
            return sb.ToString();
        }
        private string CmdSetTracingLevels(string[] args)
        {
            // trace file|Console|Pipe [TraceName|*] [\on|\off] 
            StringBuilder sb = new StringBuilder();

            string type = "";
            bool onoff = false;
            long traceBits = 0;
            if (args != null)
            {
                if (args.Length > 2)
                {
                    type = args[0];
                    if (args[1] == "*")
                    {
                        traceBits = 0xFFFFFFFFFFFFFFF;
                    }
                    else
                    {
                        //traceBits = (long)GetLogMessageTypeFromName(args[1]);
                        traceBits = MessageType.FromName( args[1] );
                    }
                    if (args[2].ToLower() == "\\on")
                        onoff = true;
                }
                //// In case the parameters came in a different order.
                //for(int i = 0; i < args.Length; i++)
                //{
                //    if (args[i].ToLower() == "file" || args[i].ToLower() == "console" || args[i].ToLower() == "pipe")
                //    {
                //        type = args[i];
                //    }
                //    else if (args[i].ToLower() == "\\on" || args[i].ToLower() == "\\off")
                //    {
                //        if( args[i] == "\\on" )
                //            onoff = true;
                //    }
                //    else
                //    {
                //        if (args[i] == "*")
                //        {
                //            traceBits = 0xFFFFFFFFFFFFFFF;
                //        }
                //        else
                //        {
                //            traceBits = (long)GetLogMessageTypeFromName(args[i]);
                //        }
                //    }
                //}


                switch (type)
                {
                    case "console":
                        if (onoff)
                            ConsoleTracing |= traceBits;
                        else
                        {
                            //foreach (long val in Enum.GetValues(typeof(LogMessageType)))
                            //{
                            //    ConsoleTracing -= traceBits&ConsoleTracing;
                            //}
//                            foreach ( long val in NewMessageType.GetValues() )
                            //{
                                ConsoleTracing -= traceBits & ConsoleTracing;
                            //}
                        }
                        break;

                    case "file":
                        if (onoff)
                            LogFileTracing |= traceBits;
                        else
                        {
                            //foreach (long val in Enum.GetValues(typeof(LogMessageType)))
                            //{
                            //    LogFileTracing -= traceBits & LogFileTracing;
                            //}
                            //foreach (long val in NewMessageType.GetValues())
                            //{
                                LogFileTracing -= traceBits & LogFileTracing;
                            //}
                        }
                        break;
                    
                    case "pipe":
                        if (onoff)
                            PipeTracing |= traceBits;
                        else
                        {
                            //foreach (long val in Enum.GetValues(typeof(LogMessageType)))
                            //{
                            //    PipeTracing -= traceBits & PipeTracing;
                            //}
                            //foreach (long val in NewMessageType.GetValues())
                            //{
                                PipeTracing -= traceBits & PipeTracing;
                            //}
                        }
                        break;
                    
                    default:
                        sb.AppendLine("Unrecognized Log Type '" + type + "'");
                        break;
                }
               
            }

            return sb.ToString();
        }
        private string CmdSaveConfigFile(string[] args)
        {
            //SaveConfigFile();
            return "";
        }
        private string CmdFlushLogs(string[] args)
        {
            StringBuilder sb = new StringBuilder();

            LogMessage("Writing log file...", MessageType.Cmd, true);   // Forces log file to be written
            sb.AppendLine("Logfile saved.");

            return sb.ToString();
        }
        private string CmdReloadLogConfig(string[] args)
        {
            UpdateConfiguration();
            return "Reload Configuration complete.";
        }
        private string CmdKillAllPipes(string[] args)
        {
//            ArrayList completedPipes;
//            completedPipes = new ArrayList();

            lock (_pipes)
            {
                foreach (ConsolePipe cp in _pipes)
                {
                    if (cp.State != PipeState.Closed)
                    {
                        LogMessage("Logger - Closing Pipe with ID:" + cp.ID.ToString(), MessageType.LogFile);
                        cp.WriteLine("Logger - BANG! You're dead");
                        cp.Close();
                        cp.Dispose();
//                        completedPipes.Add(cp);
                    }
                }

//                foreach (ConsolePipe cp in completedPipes)
                //{
                //    LogMessage("Removing Pipe from memory ID:" + cp.ID.ToString(), LogMessageType.LogFile);
                //    _pipes.Remove(cp);
                //    _pipeCount--;
                //}
            }

            return "";
        }
        private string CmdClosePipe(string[] args)
        {
            lock (_pipes)
            {
                foreach (ConsolePipe cp in _pipes)
                {
                    if (cp.ID == Convert.ToInt32(args[0]))
                    {
                        LogMessage("Logger - Closing Pipe with ID:" + cp.ID.ToString(), MessageType.LogFile);
                        cp.WriteLine("Logger - BANG! You're dead");
                        cp.Close();
                    }
                }

            }
            return "";
        }
        private string CmdPipeStatusSummary(string[] args)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("TODO: Implementation needed.");
            return sb.ToString();
        }
        private string CmdLogDuration(string[] args)
        {
            string retval = "Keeping logs for " + HoursToKeepLogFiles.ToString() + " Hours";
            //StringBuilder sb = new StringBuilder();

            if (args != null)
            {
                if (args.Length >= 1)
                {
                    try
                    {
                        LogMessage("Logger - Setting HoursToHoldLogFiles to " + args[0] + " hours", MessageType.Status);

                        //LoggerConfiguration.TracingRow tr = _logConfig.Tracing[0];
                        //tr.HoursToHoldLogFiles = 
                        HoursToKeepLogFiles = Convert.ToInt32(args[0]);
                        //_logConfig.AcceptChanges();

                        //LogMessage("Logger - Saving Logger confuration...", MessageType.Status);
                        //_logConfig.WriteXml(RootFolderName + @"\Logger.config", System.Data.XmlWriteMode.IgnoreSchema);

                        retval = "Configuration updated.\r\nKeeping logs for " + HoursToKeepLogFiles.ToString() + " Hours";
                    }
                    catch (Exception ex)
                    {
                        retval = "Error saving Logger configuration. Error:" + ex.Message;
                        LogMessage("Logger - " + retval, MessageType.Error );
                    }

                }
            }
            
            
            return retval;
        }
        public string CmdTraceTypes(string[] args)
        {
            StringBuilder sb = new StringBuilder();

            IList<FieldInfo> members = new List<FieldInfo>(_messageType.GetFields());
            IList<FieldInfo> baseMembers = new List<FieldInfo>(_messageType.BaseType.GetFields());

            foreach (FieldInfo member in baseMembers)
            {
                //object propValue = prop.GetType(_messageType, null);
                //MessageType mt = (MessageType)propValue;
                if (member.FieldType == typeof(MessageType))
                {
                    MessageType mt = (MessageType)member.GetValue(null);
                    sb.AppendLine("0x" + mt.Value.ToString("X10") + " " + mt.Character + " " + mt.Name);
                }
                // Do something with propValue
            } 

            foreach (FieldInfo member in members)
            {
                //object propValue = prop.GetType(_messageType, null);
                //MessageType mt = (MessageType)propValue;
                if (member.FieldType == _messageType)
                {
                    MessageType mt = (MessageType)member.GetValue(null);
                    sb.AppendLine("0x" + mt.Value.ToString("X10") + " " + mt.Character + " " + mt.Name);
                }
                // Do something with propValue
            } 
            
            return sb.ToString();
        }

        #endregion
    }
}

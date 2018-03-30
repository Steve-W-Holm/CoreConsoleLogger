using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Pipes;
using System.Threading;
//using System.Threading.Tasks;

namespace Itea.Logger
{

    public delegate void MessageReceivedHandler(string Message);
    public delegate void TitleUpdateHandler(string Message);
    public class LoggerClient
    {
        public event MessageReceivedHandler MessageReceived;
        public event TitleUpdateHandler TitleUpdate;

        private NamedPipeClientStream _pipeMessages;
        private NamedPipeClientStream _pipeCommands;
        private StreamReader _srPipe;
        private StreamWriter _swPipe;
        private string _appName = "Debug";
        private int _id;
        private PipeState _state;
        public bool executingCmd = false;
//        private bool _outputEnabled;
        //        private bool _isConsole;

        #region Constructors
        public LoggerClient() : this ("Debug")
        { }
        public LoggerClient(string AppName) //: this (true)
        { _appName = AppName;  }

        #endregion

        #region Properties
        public PipeState State
        {
            get 
            { 
                if(_pipeCommands == null || !_pipeCommands.IsConnected)
                    _state = PipeState.Closed;

                return _state; 
            }
            set
            {
                _state = value;
            }
        }
        //public bool OutputEnabled
        //{
        //    get { return _outputEnabled; }
        //    set { _outputEnabled = value; }
        //}
        //public bool IsConsole
        //{
        //    get { return _isConsole; }
        //    set { _isConsole = value; }
        //}
        
        #endregion

        public void Close()
        {
            _state = PipeState.Closed;
            try
            {
                _swPipe.WriteLine("close " + _id.ToString());
            }
            catch (Exception ex)
            {
                RaiseMessage("Error: " + ex.Message);
            }
            finally
            {
                if (_pipeCommands != null)
                {
                    if (_pipeCommands.IsConnected)
                        _pipeCommands.Close();
                }
                if (_pipeMessages != null)
                {
                    if (_pipeMessages.IsConnected)
                        _pipeMessages.Close();
                }
            }

        }
        public void RunOperations( object o )
        {
            PipeState StartState = (PipeState)o;
            try
            {
                ConnectToApp( StartState );
                
                if(StartState == PipeState.OutputMode)
                    _swPipe.WriteLine("start");

                UpdateTitle("Logger Client - " + _appName  + " - Pipe ID:" + _id.ToString() + " - connection:" + _pipeMessages.NumberOfServerInstances.ToString() + " " + _pipeMessages.SafePipeHandle.IsClosed.ToString());

                string temp = "";
                while (_pipeMessages.IsConnected && (temp = _srPipe.ReadLine()) != null)
                {
                    if (_state == PipeState.CommandMode && temp == ";")    // Command terminating line character
                        executingCmd = false;

                    else if (temp.StartsWith("BANG!"))
                    {
                        RaiseMessage(temp);
                        Thread.Sleep(1000);
                        Close();
                    }

                    else if (_state != PipeState.Closed)
                    {
                        if (_state == PipeState.OutputMode || _state == PipeState.CommandMode)
                        {
                            RaiseMessage(temp);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                RaiseMessage("ERROR: " + ex.Message);
            }
            finally
            {
                Thread.Sleep(8001);
            }

        }

        private void RaiseMessage(string message)
        {
            if (MessageReceived != null)
                MessageReceived(message);
        }
        private void UpdateTitle(string message)
        {
            if (TitleUpdate != null)
                TitleUpdate(message);
        }

        public void ConnectToApp(PipeState StartState)
        {
            _pipeMessages = new NamedPipeClientStream(".", _appName, PipeDirection.In);
            _srPipe = new StreamReader(_pipeMessages);
            RaiseMessage("Attempting to connect...");
            _pipeMessages.Connect(3000);
            RaiseMessage("Connected");
            _state = PipeState.Configuring;

            _id = Convert.ToInt32(_srPipe.ReadLine());  // Read PipeID


            RaiseMessage("Setting up command pipe....");
            _pipeCommands = new NamedPipeClientStream(".", _appName + _id.ToString(), PipeDirection.Out);
            _pipeCommands.Connect(3000);
            RaiseMessage("Command Pipe is connected!!!");
            _swPipe = new StreamWriter(_pipeCommands);
            _swPipe.AutoFlush = true;
            _state = StartState; // PipeState.OutputMode;

        }

        public void SendCommand(string command)
        {
            try
            {
                if (_pipeCommands != null || _pipeCommands.IsConnected)
                {
                    _swPipe.WriteLine(command);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error sending commad: " + ex.Message);
            }
        }

    }
}

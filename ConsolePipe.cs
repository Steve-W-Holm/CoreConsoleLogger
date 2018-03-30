using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.IO.Pipes;

namespace Itea.Logger
{
    public delegate void NewLogPipeMessageHandler(string strCommand, MessageType type);
    public delegate string CmdReceivedHandler(string strCommand);

    public class ConsolePipe
    {
        public event NewLogPipeMessageHandler LogMessageNew;
        public event CmdReceivedHandler CmdReceived;

        private NamedPipeServerStream _pipeOutput;
        private StreamWriter _pipeWriter;
        private NamedPipeServerStream _pipeInput;
//        private NamedPipeClientStream _pipeClient;
        private StreamReader _pipeReader;

        private static int _maxId = 1;
        private int _id;
        private string _pipeName;
        private PipeState _state;

        #region Constructors

        public ConsolePipe(int ID)
        {
            _id = ID;
        }
        public ConsolePipe() : this("noname") { }
        public ConsolePipe(string Name)
        {
            _id = _maxId++;
            _pipeName = Name;
            _state = PipeState.New;
        }

        ~ConsolePipe()
        {
            _pipeWriter = null;
            _pipeReader = null;
            _pipeOutput = null;
        }
        #endregion

        #region Properties

        public int ID
        {
            get { return _id; }
        }

        public PipeState State
        {
            get { return _state; }
        }
        
        #endregion

        public void Dispose()
        {
            //if(_pipeWriter != null)
            //    _pipeWriter.Dispose();

            //if (_pipeReader != null)
            //    _pipeReader.Dispose();

            if (_pipeOutput != null)
            {
                if (_pipeOutput.IsConnected)
                    _pipeOutput.Close();

//                _pipeServer = null;
            }
            if (_pipeInput != null)
            {
                if (_pipeInput.IsConnected)
                    _pipeInput.Close();

                _pipeInput = null;
            }
            //if(_pipeServer != null)
            //_pipeServer.Dispose();
        }
        public void Close()
        {
            _state = PipeState.Closed;

            if (_pipeOutput != null)
            {
                if (_pipeOutput.IsConnected)
                    _pipeOutput.Close();

            }
            if (_pipeInput != null)
            {
                if (_pipeInput.IsConnected)
                    _pipeInput.Close();
            }
        }
        public void Start()
        {
            try
            {
                _pipeOutput = new NamedPipeServerStream(_pipeName, PipeDirection.Out, 5, PipeTransmissionMode.Message);
                _pipeOutput.ReadMode = PipeTransmissionMode.Message;
                
                _state = PipeState.Waiting;
                LogMyMessage("ConsolePipe - New Pipe waiting for connection ID:" + _id.ToString(), MessageType.Pipe);
                _pipeOutput.WaitForConnection();    // Blocking method.
                
                LogMyMessage("ConsolePipe - Received Pipe Connection on ThreadID:" + Thread.CurrentThread.ManagedThreadId + " PipeID:" + _id.ToString(), MessageType.Threading);
                LogMyMessage("ConsolePipe - Received Connection on Pipe ID:" + _id.ToString(), MessageType.Pipe);

                _state = PipeState.Configuring;
                _pipeWriter = new StreamWriter(_pipeOutput);
                _pipeWriter.AutoFlush = true;


                if (_pipeOutput.IsConnected)
                {
                    ListenForInput();

                    //// No need for a new thread - Commented out 11/3/2016
                    //Thread pipeInputThread = new Thread(ListenForInput);    
                    //pipeInputThread.Start();
                    //LogMyMessage("ThreadID:" + Thread.CurrentThread.ManagedThreadId + " started a new ThreadID:" + pipeInputThread.ManagedThreadId + " Method:InputThread", MessageType.Threading);
                }
            }
            catch (IOException ex)
            {
                _state = PipeState.Closed;
                LogMyMessage("ConsolePipe - Pipe ID:" + _id.ToString() + " ERROR: " + ex.Message, MessageType.Pipe);
            }

            LogMyMessage("Logger - Thread complete - ThreadID:" + Thread.CurrentThread.ManagedThreadId + " Method:WriteLogFile", MessageType.Threading);
        }

        public void WriteLine(string Message)
        {
            try
            {
                if (_state == PipeState.OutputMode || _state == PipeState.CommandMode)
                {
                    if(_pipeOutput.IsConnected)
                        _pipeWriter.WriteLine(Message);
                }
            }
            catch (IOException ex)
            {
                _state = PipeState.Closed;
                _pipeOutput.Close();
                LogMyMessage("ConsolePipe - Error on Pipe ID:" + _id.ToString() + " Message:" + ex.Message, MessageType.Pipe);
                LogMyMessage("ConsolePipe - Closing Pipe ID:" + _id.ToString(), MessageType.Pipe);
            }
        }

        private void ListenForInput()
        {
            try
            {
                // Setup InputPipe - Send my PipeID to client to negotiate an inbound pipe.
                _pipeWriter.WriteLine(_id.ToString());  
                _pipeInput = new NamedPipeServerStream(_pipeName + _id.ToString(), PipeDirection.In, 1, PipeTransmissionMode.Message);

                LogMyMessage("ConsolePipe - Waiting for inbound pipe connection from client. ID:" + _id.ToString(), MessageType.Pipe);
                _pipeInput.WaitForConnection();    // Blocking method.
                LogMyMessage("ConsolePipe - Received Connection on Pipe ID:" + _id.ToString(), MessageType.Pipe);
                _pipeReader = new StreamReader(_pipeInput);
                _state = PipeState.CommandMode;

                string input, response;
                while (_pipeInput != null && _pipeInput.IsConnected)
                {
                    if ((input = _pipeReader.ReadLine()) != null)
                    {
                        _state = PipeState.CommandMode;
                        if (CmdReceived != null)
                        {
                            LogMyMessage("ConsolePipe - Recieved message on Pipe ID:" + _id.ToString() + " Message:'" + input + "'", MessageType.Pipe);

                            response = "";
                            if (input.Trim().ToLower() == "stop")
                            {
                                _state = PipeState.CommandMode;
                            }
                            else if (input.Trim().ToLower() == "start")
                            {
                                _state = PipeState.OutputMode;
                            }
                            else
                            {
                                response = CmdReceived(input.Trim()); // lowercase everything and trim spaces
                                WriteLine(response);
                            }
                        }
                    }

                    Thread.Sleep(11);
                }
            }
            catch (IOException ex)
            {
                LogMyMessage("ConsolePipe - ERROR: " + ex.Message, MessageType.Pipe);
            }

        }

        private void LogMyMessage(string message, MessageType type)
        {
            if (LogMessageNew != null)
            {
                LogMessageNew(message, type);
            }
        }

        public override bool Equals(object obj)
        {
            //FIX: Type check 
            if (obj.GetType() != typeof(ConsolePipe)) return false;

            ConsolePipe o = (ConsolePipe)obj;

            if (o.ID == this.ID)
                return true;
            else
                return false;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public enum PipeState
    {
        New = 0,
        Waiting = 1,
        Configuring = 2,
        OutputMode = 3,
        CommandMode = 4,
        Closed = 5
    }
}

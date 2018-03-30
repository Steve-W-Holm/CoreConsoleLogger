using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
//using Itea.ConsoleLogger;

namespace Itea.Logger
{
    class LoggerClientApp
    {
        static void Main(string[] args)
        {
            Thread.Sleep(1000);

            string appname = "GoRec"; // Generics.MyName();

//#if DEBUG
//            appname = "Debug";
//#endif
            Console.WriteLine("My name is " + appname);

            LoggerClient pipe = new LoggerClient(appname);
            pipe.MessageReceived += new MessageReceivedHandler(LogMessage);
            pipe.TitleUpdate += new TitleUpdateHandler(UpdateTitle);
            Thread threadPipe = new Thread( pipe.RunOperations );
            threadPipe.Start(PipeState.OutputMode);
            


            // Wait for connection, up to 10 seconds
            int i = 0;
            while (pipe.State != PipeState.CommandMode && pipe.State != PipeState.OutputMode && i < 100)
            {
                Thread.Sleep(100);
                i++;
            }

            string temp;

            pipe.SendCommand("start");

            try
            {
                while (pipe.State != PipeState.Closed) //&& (temp = Console.ReadLine()) != null)
                {
                    if (Console.KeyAvailable)
                    {
                        temp = Console.ReadLine();

                        if (pipe.State == PipeState.Closed)
                        {
                            break;
                        }

                        pipe.State = PipeState.CommandMode;

                        //bool executingCmd = false;
                        switch (temp.ToLower().Trim())
                        {
                            case "start":
                                pipe.State = PipeState.OutputMode;
                                Console.Clear();
                                pipe.SendCommand("start");

                                //_swPipe.WriteLine("start");
                                break;
                            case "close":
                                pipe.Close();
                                break;
                            case "exit":
                                pipe.Close();
                                break;
                            case "connect":
                                Console.WriteLine("Not yet implemented");
                                break;
                            case "":
                                pipe.State = PipeState.CommandMode;
                                pipe.SendCommand("stop");
                                //_swPipe.WriteLine("stop");
                                break;
                            default:
                                pipe.State = PipeState.CommandMode;
                                pipe.executingCmd = true;
                                pipe.SendCommand(temp);
                                //_swPipe.WriteLine(temp);
                                break;
                        }

                        if (pipe.State == PipeState.CommandMode)
                        {
                            int seconds = 0;
                            Thread.Sleep(500);
                            while (pipe.executingCmd && seconds < 29)    // Wait for any remaining messages before displaying command prompt
                            {
                                Console.Write('.');
                                Thread.Sleep(500); 
                                seconds++;
                            }

                            Thread.Sleep(500);
                            //RaiseMessage("Cmd:>");
                            //Console.WriteLine("");
                            Console.Write("Cmd:>");  // Display Command prompt
                            pipe.executingCmd = false;
                        }

                    }

                    Thread.Sleep(11);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error Received. Closing applications. Error:" + ex.Message);
                Thread.Sleep(2000);
                
                pipe.State = PipeState.Closed;
            }
        }
        

        static private void LogMessage(string message)
        {
            Console.WriteLine(message);
        }
        static private void UpdateTitle(string message)
        {
            Console.Title = message;
        }
    }
}

using System;
using System.Text;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;


namespace iCUE_HTTP_Server
{
    class PipeServer
    {
        // Used to keep track of operations and stop race-conditions
        private static string nextMessageToSend;
        private static bool needToResetPipeClient;

        // Used for returning values from the PipeClient, nullable to be able to detect when a value is returned (ie. not null anymore)
        private static string returnMessage;

        // The current PipeClient program, used to be able to shut it down at will
        private static Process pipeClient;

        private static string pre { get { return string.Format("{0} PipeServer -- ", DateTime.Now.ToString("[HH:mm:ss]")); } }

        public static void Run ()
        {
            while (Program.isRunning)
            {
                // Setting up for a new PipeClient program
                pipeClient = new Process();
                pipeClient.StartInfo.FileName = Program.CgSDKHandlerDir;
                pipeClient.StartInfo.UseShellExecute = false;

                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("CgPipe", PipeDirection.InOut))
                {
                    // Start the new PipeClient program
                    pipeClient.Start();
                    needToResetPipeClient = false;
                    Console.WriteLine(pre + "Started Pipe Client");

                    // Wait until the new PipeClient has connected to the server
                    pipeServer.WaitForConnection();
                    Console.WriteLine(pre + "Pipe Client connected");

                    try
                    {
                        while (!needToResetPipeClient)
                        {                          
                            // Send request to the client
                            using (StreamWriter sw = new StreamWriter(pipeServer, Encoding.UTF8, 512, true))
                            {
                                // Wait until there is a message to send
                                while (string.IsNullOrWhiteSpace(nextMessageToSend) && !needToResetPipeClient)
                                {
                                    Thread.Sleep(1);
                                }

                                // Escape if need to restart the client
                                if (needToResetPipeClient)
                                {
                                    break;
                                }

                                sw.WriteLine(nextMessageToSend);
                                nextMessageToSend = null;
                            }

                            // Wait for response from client
                            using (StreamReader sr = new StreamReader(pipeServer, Encoding.UTF8, false, 512, true))
                            {
                                string msg;
                                // Wait until the PipeClient returns a response
                                while ((msg = sr.ReadLine()) == null && !needToResetPipeClient)
                                {
                                    Thread.Sleep(1);
                                }

                                // Escape if need to restart the client
                                if (needToResetPipeClient)
                                {
                                    returnMessage = "resetting";
                                    break;
                                }

                                // Print the response we received -- for debug
                                Console.WriteLine(pre + "Received response: {0}", msg);

                                returnMessage = msg;
                            }
                        }
                    }
                    // Catch the error thrown when the pipe is broken or disconnected
                    catch (IOException e)
                    {
                        Console.WriteLine(pre + e.ToString());
                    }
                    catch (ObjectDisposedException e)
                    {
                        Console.WriteLine(pre + e.ToString());
                    }
                }

                // Close the old client
                CleanUpClients();
                Console.WriteLine(pre + "Closed Pipe Client");
            }
        }

        // Called to initiate the safe relaunching of the client -- used to reset access to the CgSDK to allow SetGame() to be called twice
        public static void ResetClient ()
        {
            needToResetPipeClient = true;
        }

        private static readonly object syncLock = new object();

        private static string CallFunction(string function)
        {
            lock(syncLock)
            {
                nextMessageToSend = function;
                while (returnMessage == null)
                {
                    Thread.Sleep(1);
                }
                string result = returnMessage;
                returnMessage = null;
                return result;
            }
        }

        // Defines a function on the PipeClient that returns an integer
        public static int IntFunction (string function)
        {
            return int.Parse(CallFunction(function));
        }

        // Defines a function on the PipeClient that returns a boolean
        public static bool BoolFunction(string function)
        {
            return StringToBool(CallFunction(function));
        }

        // Close the active client, called when we need to reset the client and when we close the server
        public static void CleanUpClients ()
        {
            if (pipeClient != null && !pipeClient.HasExited)
            {
                pipeClient.Kill();
                pipeClient = null;
            }
        }

        // Converts a string to a boolean -- used in interpretting data from the PipeClient
        private static bool StringToBool (string state)
        {
            if (state == "true")
            {
                return true;
            }

            return false;
        }
    }
}

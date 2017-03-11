using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Morpheus
{
    
    class LocalServer
    {
        [System.Runtime.InteropServices.DllImport("Powrprof.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, ExactSpelling = true)]
        private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);
        /// <summary>
        /// Starts a local UDP server, in order to receive commands from the user's application.
        /// Creates and runs the corresponding process.
        /// </summary>
        public void Start()
        {
            // Containers for the command and the argument
            string processArgs = String.Empty;
            string userCommand = String.Empty;
            // There is no exit mechanism, but it looks better this way
            bool shouldExit = false;
            // UDP object for receiving messages.
            UdpClient udpReceiver = null;

            try
            {
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 12765);
                udpReceiver = new UdpClient(endpoint);
              
                while (!shouldExit)
                {
                    Console.WriteLine("Waiting...");
                    byte[] rcvBytes = udpReceiver.Receive(ref endpoint);
                    userCommand = Encoding.ASCII.GetString(rcvBytes);
                    if (!String.IsNullOrEmpty(userCommand))
                    {
                        Console.WriteLine("Received: " + userCommand);
                        // Assign the process arguments according to the command type
                        switch (userCommand)
                        {
                            case "SLEEP":
                                // We could use "shutdown -h" but the hibernate feature may be disabled. So put it in stand-by mode.
                                SetSuspendState(false, true, true);
                                continue;                                
                            case "RESTART":
                                processArgs = "/r /t 1";
                                break;
                            case "SHUTDOWN":
                                processArgs = "/s /t 1";
                                break;
                            default:
                                Console.WriteLine("Unknown command");
                                continue;
                        }
                        ProcessStartInfo processCommand = new ProcessStartInfo("shutdown", processArgs);
                        processCommand.CreateNoWindow = true;
                        processCommand.UseShellExecute = false;
                        Process.Start(processCommand);
                    }
                    else
                    {
                        Console.WriteLine("Message empty");
                    }
                }
            }
            // The nature of this application doesn't leave us with many options. Crash silently.
            catch (SocketException s)
            { 
                // For debugging
                Console.WriteLine("SocketException: " + s.Message);
            }
            catch (ArgumentException a)
            {
                // For debugging
                Console.WriteLine("ArgumentException: " + a.Message);
            }
            finally
            {
                if (udpReceiver != null)
                    udpReceiver.Close();
            }
}
    }
}

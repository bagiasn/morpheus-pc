using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace Morpheus
{
    class Program
    {
        const string REG_KEY_NAME = "Morpheus";

        static void Main(string[] args)
        {
            CheckForDuplicateProcess();

            CheckRegistry();

            CheckNetworkStatus();

            LocalServer server = new LocalServer();
            server.Start();
        }

        /// <summary>
        /// Ensures this is the only app instance alive.
        /// It is also useful when changing the app's location
        /// </summary>
        private static void CheckForDuplicateProcess()
        {
            Process currentProcess = Process.GetCurrentProcess();
            var listProcesses = Process.GetProcessesByName(currentProcess.ProcessName);
            // If the list contains more than one, we have a duplicate.
            if (listProcesses.Length > 1)
            {
                foreach (var proc in listProcesses)
                {
                    // Compare the IDs to kill the previous one.
                    if (proc.Id != currentProcess.Id)
                        proc.Kill();
                }
            }
        }

        /// <summary>
        /// Ensures that the application is included in the startup process.
        /// It, also, renews the app's path in registry, if necessary.
        /// (to provide the option to change its location)
        /// </summary>
        private static void CheckRegistry()
        {
            string procPath = Process.GetCurrentProcess().MainModule.FileName;
            using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                string currentEntry = (string) regKey.GetValue(REG_KEY_NAME);
                if (currentEntry == null)
                {
                    regKey.SetValue(REG_KEY_NAME, procPath, RegistryValueKind.String);
                    Console.WriteLine("New path: " + procPath);
                }
                else if (!currentEntry.Equals(procPath))
                {
                    Console.WriteLine("Previous path: " + currentEntry);
                    // First, delete the previous value.
                    regKey.DeleteValue(REG_KEY_NAME, false);
                    // Then, insert the new path.
                    regKey.SetValue(REG_KEY_NAME, procPath, RegistryValueKind.String);
                    Console.WriteLine("New path: " + procPath);
                }
                regKey.Close();
            }
        }

        /// <summary>
        /// Checks whether there is a valid network connection
        /// and, if not, waits until there is one.
        /// </summary>
        private static void CheckNetworkStatus()
        {
            do
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if ((ni.OperationalStatus != OperationalStatus.Up) || (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) ||
                            (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel))
                            continue;
                        if (ni.Description.Equals("Microsoft Loopback Adapter", StringComparison.OrdinalIgnoreCase))
                            continue;
                        Console.WriteLine("Network connected.");
                        return;
                    }
                }
                // Sleep is definitely not the best approach, but we need to keep the main process busy.
                Console.WriteLine("Network not available");
                System.Threading.Thread.Sleep(2000);
              
            } while (true);
        }

    }
    

}

using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using DivaHook.Injection;
using DivaHook.Emulator;
using DivaHook.Emulator.Config;

namespace TotallyLegitArcadeController
{
    class Program
    {
        private static readonly bool injectHook = true;

        private static DivaConfig divaConfig;

        private static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;

            if (injectHook)
            {
                Console.Title = "DivaHook Host";
                TryInjectHook();
            }
            else
            {
                InputEmulator.KeyConfig.TryLoadConfig();
                InitializeActivePollEmulator();
            }

            Console.ResetColor();
        }

        private static void InitializeActivePollEmulator()
        {
            Console.WriteLine("MikuComfy");

            var emulator = new InputEmulator();
            emulator.LoopUpdateEmulateInput();
        }

        private static void TryInjectHook()
        {
            divaConfig = new DivaConfig();
            divaConfig.TryLoadConfig();

            string directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string injectionLibrary = Path.Combine(directory, "DivaHook.dll");

            Process[] processes = Process.GetProcessesByName("diva");
            Process divaProcess = processes.FirstOrDefault();

            string channelName = null;

            if (divaProcess == null)
            {
                if (!File.Exists(divaConfig.ExePath))
                {
                    Console.Write(
                        $"The specified path to diva.exe \"{divaConfig.ExePath}\" does not exist and no diva process could be found.\n" +
                        $"Please correct your {divaConfig.ConfigFileName} or start project diva, then run this application again...");
                    Console.ReadKey();
                    return;
                }
                else
                {
                    Console.WriteLine($"Launching: {divaConfig.ExePath} {divaConfig.ExeArguments}");
                    CreateAndInjectHook(injectionLibrary, ref divaProcess, ref channelName);
                }
            }
            else
            {
                Console.WriteLine($"Diva process found: {divaProcess.ProcessName} - {divaProcess.Id}, injecting hook...");
                InjectHook(injectionLibrary, ref divaProcess, ref channelName);
            }

            divaProcess.WaitForExit();
        }

        private static void CreateAndInjectHook(string injectionLibrary, ref Process divaProcess, ref string channelName)
        {
            EasyHook.RemoteHooking.IpcCreateServer<ServerInterface>(ref channelName, System.Runtime.Remoting.WellKnownObjectMode.Singleton);
            EasyHook.RemoteHooking.CreateAndInject(
                divaConfig.ExePath,         // executable to run
                divaConfig.ExeArguments,    // command line arguments for target
                0,                          // additional process creation flags to pass to CreateProcess
                EasyHook.InjectionOptions.DoNotRequireStrongName, // allow injectionLibrary to be unsigned
                injectionLibrary,           // 32-bit library to inject (if target is 32-bit)
                injectionLibrary,           // 64-bit library to inject (if target is 64-bit)
                out int processId,          // retrieve the newly created process ID
                channelName                 // the parameters to pass into injected library
            );

            divaProcess = Process.GetProcessById(processId);
        }

        private static void InjectHook(string injectionLibrary, ref Process divaProcess, ref string channelName)
        {
            EasyHook.RemoteHooking.IpcCreateServer<ServerInterface>(ref channelName, System.Runtime.Remoting.WellKnownObjectMode.Singleton);
            EasyHook.RemoteHooking.Inject(
                divaProcess.Id,     // ID of process to inject into
                injectionLibrary,   // 32-bit library to inject (if target is 32-bit)
                injectionLibrary,   // 64-bit library to inject (if target is 64-bit)
                channelName         // the parameters to pass into injected library
            );
        }
    }
}
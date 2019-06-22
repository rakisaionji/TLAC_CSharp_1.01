using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivaHook.Injection
{
    /// <summary>
    /// Provides an interface for communicating from the client (target) to the server (injector)
    /// </summary>
    public class ServerInterface : MarshalByRefObject
    {
        public void IsInstalled(int clientPID)
        {
            return;
        }

        public void ReportMessages(params string[] messages)
        {
            for (int i = 0; i < messages.Length; i++)
                Console.WriteLine(messages[i]);
        }

        public void ReportMessage(string message)
        {
            Console.WriteLine(message);
        }

        public void ReportString(string message)
        {
            Console.Write(message);
        }

        public void ReportException(Exception e)
        {
            Console.WriteLine("The target process has reported an error:\r\n" + e.ToString());
        }
        
        public void Ping()
        {
            return;
        }

        public void Execute(Action action)
        {
            action.Invoke();
        }
    }
}

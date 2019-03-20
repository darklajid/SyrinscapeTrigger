using Microsoft.Win32;
using System;
using System.Configuration;
using System.Net.Http;
using System.Reflection;
using System.Security.Principal;

namespace SyrinscapeTrigger
{
    class Program
    {
        private static string ProtocolName = "Syrinscape";

        static void Main(string[] args)
        {
            try
            { 
                Run(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Caught an exception: {0}", ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        static void Run(string[] args)
        {
            CheckProtocolHandlerExists();

            var baseApiUrl = ConfigurationManager.AppSettings["baseApiUrl"] ?? "https://www.syrinscape.com/online/frontend-api/";
            var authToken = ConfigurationManager.AppSettings["auth_token"];
            if (string.IsNullOrEmpty(baseApiUrl) || string.IsNullOrEmpty(authToken))
            {
                Console.WriteLine("Please configure a Syrinscape Online 'auth_token' in the app settings");
                return;
            }

            if (args.Length < 1 || string.IsNullOrEmpty(args[0]) || args[0].Length < ProtocolName.Length+1)
            {
                Console.WriteLine("No arguments provided, nothing to do");
                return;
            }
            var urlFragment = args[0].Substring(ProtocolName.Length+1);

            CallSyrinscape(baseApiUrl, authToken, urlFragment);
        }

        static bool IsAdmin()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private static void CheckProtocolHandlerExists()
        {            
            using (var key = Registry.ClassesRoot.OpenSubKey(ProtocolName, false))
            {
                if (key != null) return;

                if (!IsAdmin())
                {
                    Console.WriteLine("Protocol not registered, but not running elevated/as an admin. Can't fix it.");
                    return;
                }

                // Create protocol handler
                using (var pk = Registry.ClassesRoot.CreateSubKey(ProtocolName))
                using (var sok = pk.CreateSubKey(@"shell\open\command"))
                {
                    pk.SetValue(string.Empty, "URL:Syrinscape Online sound trigger protocol");
                    pk.SetValue("URL Protocol", string.Empty);
                    
                    sok.SetValue(string.Empty, $"\"{Assembly.GetExecutingAssembly().Location}\" \"%1\"");
                }
            }
        }

        private static void CallSyrinscape(string baseApiUrl, string authToken, string urlFragment)
        {
            using (var client = new HttpClient())
            {
                var url = $"{baseApiUrl}{urlFragment.TrimStart('/')}?auth_token={authToken}";
                var response = client.GetStringAsync(url).Result;
            }            
        }
    }
}

using Newtonsoft.Json;
using Reddit;
using Reddit.AuthTokenRetriever;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace RedditBerner
{
    public class Workflow
    {
        private string ConfigDir { get; set; }
        private string ConfigPath { get; set; }
        private string SubredditsPath { get; set; }
        private string ScriptsDir { get; set; }

        private Config Config { get; set; }
        private IList<string> Scripts { get; set; }
        private IList<string> Subreddits { get; set; }

        private RedditAPI Reddit { get; set; }
        public bool Active { get; set; }

        private string AppId { get; set; } = "z8huXvY0aph0PQ";

        public Workflow()
        {
            ConfigDir = Path.Combine(Environment.CurrentDirectory, "config");
            if (!Directory.Exists(ConfigDir))
            {
                Directory.CreateDirectory(ConfigDir);
            }

            ConfigPath = Path.Combine(ConfigDir, "RedditBerner.config.json");
            if (!File.Exists(ConfigPath))
            {
                // Create new config file and prompt user for token retrieval process.  --Kris
                Config = new Config(AppId);

                Console.WriteLine("****************************");
                Console.WriteLine("* Welcome to RedditBerner! *");
                Console.WriteLine("****************************");

                Console.WriteLine();

                Console.WriteLine("Before the bot can run, we'll need to link it to your Reddit account.");
                Console.WriteLine("This is very easy:  Whenever you're ready, press any key and a browser window will open and take you to the Reddit authentication page.");
                Console.WriteLine("Enter your username/password if you're not already logged in, then scroll down and click on the 'Allow' button to authorize this app to use your Reddit account.");

                Console.WriteLine();

                Console.WriteLine("Press any key to continue....");

                Console.ReadKey();

                AuthTokenRetrieverLib authTokenRetrieverLib = new AuthTokenRetrieverLib(AppId);
                authTokenRetrieverLib.AwaitCallback();

                Console.Clear();

                Console.WriteLine("Opening web browser....");

                OpenBrowser(authTokenRetrieverLib.AuthURL());

                DateTime start = DateTime.Now;
                while (string.IsNullOrWhiteSpace(authTokenRetrieverLib.RefreshToken)
                    && start.AddMinutes(5) > DateTime.Now)
                {
                    Thread.Sleep(1000);
                }

                if (string.IsNullOrWhiteSpace(authTokenRetrieverLib.RefreshToken))
                {
                    throw new Exception("Unable to authorize Reddit; timeout waiting for Refresh Token.");
                }

                Config.AccessToken = authTokenRetrieverLib.AccessToken;
                Config.RefreshToken = authTokenRetrieverLib.RefreshToken;

                SaveConfig();

                Console.WriteLine("Reddit authentication successful!  Press any key to continue....");

                Console.ReadKey();
            }
            else
            {
                LoadConfig();
            }

            SubredditsPath = Path.Combine(ConfigDir, "subreddits.json");
            if (!File.Exists(SubredditsPath))
            {
                Subreddits = new List<string>
                {
                    "StillSandersForPres",
                    "WayOfTheBern",
                    "SandersForPresident",
                    "BernieSanders"
                };
                SaveSubreddits();
            }
            else
            {
                LoadSubreddits();
            }

            ScriptsDir = Path.Combine(Environment.CurrentDirectory, "scripts");
            if (!Directory.Exists(ScriptsDir))
            {
                Directory.CreateDirectory(ScriptsDir);
            }

            if (!Directory.EnumerateFileSystemEntries(ScriptsDir).Any())
            {
                throw new Exception("Scripts directory cannot be empty!  Please add at least 1 text file to serve as a comment template so the app knows what content to post.");
            }

            LoadScripts();
            if (Scripts == null
                || Scripts.Count.Equals(0))
            {
                throw new Exception("No suitable scripts found!  Please add at least 1 text file under 10 K to serve as a comment template so the app knows what content to post.");
            }

            Reddit = new RedditAPI(appId: AppId, refreshToken: Config.RefreshToken, accessToken: Config.AccessToken);
        }

        public void Start()
        {
            Console.WriteLine("Commencing bot workflow....");

            // TODO - Register callback.  --Kris

            Active = true;
            while (Active)
            {
                // TODO - Main workflow.  --Kris
            }

            // TODO - Unregister callback.  --Kris

            Console.WriteLine("Bot workflow terminated.");
        }

        private void LoadConfig()
        {
            Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigPath));
        }

        private void SaveConfig()
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(Config));
        }

        private void LoadSubreddits()
        {
            Subreddits = JsonConvert.DeserializeObject<IList<string>>(File.ReadAllText(SubredditsPath));
        }

        private void SaveSubreddits()
        {
            File.WriteAllText(SubredditsPath, JsonConvert.SerializeObject(Subreddits));
        }

        // Note - Script files must end in a .txt extension and not exceed 10,000 characters in length in order to be recognized.  --Kris
        private void LoadScripts()
        {
            Scripts = new List<string>();
            foreach (FileInfo file in (new DirectoryInfo(ScriptsDir)).GetFiles("*.txt", SearchOption.AllDirectories))
            {
                if (file.Length <= 10000)
                {
                    Scripts.Add(File.ReadAllText(file.FullName));
                }
            }
        }

        public void OpenBrowser(string authUrl, string browserPath = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe")
        {
            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo(authUrl);
                Process.Start(processStartInfo);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // This typically occurs if the runtime doesn't know where your browser is.  Use BrowserPath for when this happens.  --Kris
                ProcessStartInfo processStartInfo = new ProcessStartInfo(browserPath)
                {
                    Arguments = authUrl
                };
                Process.Start(processStartInfo);
            }
        }
    }
}

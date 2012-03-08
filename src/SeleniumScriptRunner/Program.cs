using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using ConsoleApplication;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;

namespace SeleniumScriptRunner {
    public class Program : CommandLineProgram<Program, Arguments> {
        static void Main(string[] args) {
            new Program().RunProgram(args);
        }

        protected override void Run(Arguments arguments) {
            Out(arguments.ToString());

            Result.NUnitResultAccumulator log = new Result.NUnitResultAccumulator(arguments.TestName ?? @"Selenium Script Test");

            Out(@"Finding files matching {0}", arguments.ScriptFile);
            string basePath = Path.GetDirectoryName(arguments.ScriptFile);
            string fileMask = Path.GetFileName(arguments.ScriptFile);
            if (string.IsNullOrEmpty(fileMask)) fileMask = @"*.*";

            SeleniumScript script = null;
            foreach (string file in Directory.GetFiles(basePath, fileMask)) {
                try {
                    Out(@"Opening file {0}", file);
                    script = SeleniumHtmlScriptParser.Load(file);
                } catch (Exception ex) {
                    Out(@"Error loading script {0}: {1}, skipping this file", file, ex.Message);
                    continue;
                }

                try {
                    Out(@"Running script file {0}", file);
                    for (int i = 0; i < arguments.TestCombinations.Length; i++) {
                        //Out(@"Starting RemoteWebDriver with capabilities {0} for file {1}", arguments.TestCombinations[i], file);
                        //IWebDriver driver = CreateDriver(arguments, i);
                    }
                } catch (Exception ex) {
                    Out(@"Error running script {0}: {1}", file, ex.Message);
                    continue;
                }
            }
            
        }

        private IWebDriver CreateDriver(Arguments arguments, int index) {

            string[] pieces = arguments.TestCombinations[index].Split(';');
            if (pieces.Length != 3) throw new ArgumentException(@"TestCombination");
            DesiredCapabilities capabilities = new DesiredCapabilities(pieces[0], pieces[1], GetPlatform(pieces[2]));
            capabilities.SetCapability("name", arguments.TestName);
            capabilities.SetCapability("build", arguments.BuildVersion);
            capabilities.SetCapability("username", arguments.UserID);
            capabilities.SetCapability("accessKey", arguments.Token);

            var driver = new RemoteWebDriver(
                new Uri(arguments.RemoteUrl), 
                capabilities);

            return driver;
        }

        private Platform GetPlatform(string p) {
            return new Platform((PlatformType)Enum.Parse(typeof(PlatformType), p, true));
        }

        protected override void Validate(Arguments arguments) {
            return;
        }

        protected override void Exit(Arguments arguments) {
            if (arguments.WaitForExit) WaitForExit();
        }
    }

    public class Arguments {

        [Option(@"n", @"name", HelpText = @"An optional name for the entire test run.  This value may be sent to remote web driver provider for organizational purposes.", Required = false)]
        public string TestName = null;

        [Option(@"v", @"buildversion", HelpText = @"An optional identifier for the build being tested.  This value may be sent to remote web driver provider for cross-referencing purposes.", Required = false)]
        public string BuildVersion = null;

        [Option(@"r", @"remote", HelpText = @"The RemoteWebDriver URL", Required = true)]
        public string RemoteUrl = null;

        [Option(@"u", @"userid", HelpText = @"The specified user's identifier (if required by remote provider)", Required = false)]
        public string UserID = null;

        [Option(@"t", @"token", HelpText = @"The specified user's API token (if required by remote provider)", Required = false)]
        public string Token = null;

        [OptionArray(@"c", @"combo", HelpText = @"One or more combinations of browser;level;os strings to be used in testing. Each combination should be wrapped in quotes", Required = true)]
        public string[] TestCombinations = { };
        // {android|chrome|firefox|htmlunit|internet explorer|iPhone|iPad|opera}
        // 6, 7, 8, 9, 10, etc.
        // {WINDOWS|XP|VISTA|MAC|LINUX|UNIX}

        [Option(@"b", @"baseurl", HelpText = @"Base URL to be used for testing (i.e., your test or production site)", Required = true)]
        public string BaseUrl = null;

        [Option(@"s", @"script", HelpText = @"Path and file name to the script file to be run.  May include wildcards for the file name to run multiple files.", Required = true)]
        public string ScriptFile = null;

        [Option(@"o", @"output", HelpText = @"Path and file name of the results file to be written, using NUnit XML format.  If null or empty, output is written to stdout.", Required = false)]
        public string OutputFile = null;

        [Option(@"w", @"wait", HelpText = @"If TRUE, then wait for <Enter> before ending the program")]
        public bool WaitForExit = false;

        public override string ToString() {
            return string.Format(@"Remote URL = {0}, User ID = {1}, Token = {2}, Combinations = [{3}], Base URL = {4}, Script File = {5}, Output File = {6}, Wait for Exit = {7}, Build Version = {8}",
                this.RemoteUrl, this.UserID, this.Token, string.Join(", ", this.TestCombinations), BaseUrl, ScriptFile, OutputFile, WaitForExit, BuildVersion);
        }
    }
}

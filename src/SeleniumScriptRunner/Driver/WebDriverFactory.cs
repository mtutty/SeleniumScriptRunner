using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System.Collections.Specialized;

namespace SeleniumScriptRunner.Driver {
    /// <summary>
    /// Factory class for connecting to Remote Selenium Web Driver providers
    /// </summary>
    public class WebDriverFactory {
        private WebDriverFactory() { /* Not creatable */ }

        /// <summary>
        /// Constructs a set of name/value pairs suitable for communicating with a Sauce Labs RemoteWebDriver.
        /// </summary>
        /// <param name="testName">Custom name for the test being run</param>
        /// <param name="version">Version indicator for the code under test</param>
        /// <param name="userID">Unique identifier for the Sauce Labs account being used</param>
        /// <param name="accessKey">Secret key / API token for the Sace Labs account being used</param>
        /// <returns>A StringDictionary with keys and values set according to the provided parameter values</returns>
        public static NameValueCollection SauceLabsCapabilities(string testName, string version, string userID, string accessKey) {
            NameValueCollection ret = new NameValueCollection();
            ret.Add("name", testName);
            ret.Add("build", version);
            ret.Add("username", userID);
            ret.Add("accessKey", accessKey);
            return ret;
        }

        /// <summary>
        /// Creates a RemoteWebDriver using the provided parameter values.
        /// </summary>
        /// <param name="remoteUrl">The source of the RemoteWebDriver instance</param>
        /// <param name="testCombo">A combination of browser, version number and OS/Platform, delimited by semi-colons.</param>
        /// <param name="customCapabilities">Additional name/value pairs to be sent to the remote provider</param>
        /// <returns>A RemoteWebDriver, initialized and connected to the provider according to the parameter values</returns>
        public static IWebDriver CreateRemoteDriver(string remoteUrl, string testCombo, NameValueCollection customCapabilities) {
            string[] pieces = testCombo.Split(';');
            if (pieces.Length != 3) throw new ArgumentException(@"TestCombination");
            return CreateRemoteDriver(remoteUrl, pieces[0], pieces[1], pieces[2], customCapabilities);
        }

        /// <summary>
        /// Creates a RemoteWebDriver using the provided parameter values.
        /// </summary>
        /// <param name="remoteUrl">The source of the RemoteWebDriver instance</param>
        /// <param name="browser">Name of the browser program to be used for testing.  See SeleniumHQ.org for more information</param>
        /// <param name="version">Version number of the browser program to be used for testing.  See SeleniumHQ.org for more information</param>
        /// <param name="platform">Name of the O/S to be used for testing.  See SeleniumHQ.org for more information</param>
        /// <param name="customCapabilities">Additional name/value pairs to be sent to the remote provider</param>
        /// <returns>A RemoteWebDriver, initialized and connected to the provider according to the parameter values</returns>
        public static IWebDriver CreateRemoteDriver(string remoteUrl, string browser, string version, string platform, NameValueCollection customCapabilities) {
            DesiredCapabilities capabilities = new DesiredCapabilities(browser, version, GetPlatform(platform));
            if (customCapabilities != null && customCapabilities.Count > 0) {
                foreach (string key in customCapabilities.Keys) {
                    capabilities.SetCapability(key, customCapabilities[key]);
                }
            }

            var driver = new RemoteWebDriver(
                new Uri(remoteUrl),
                capabilities);

            return driver;
        }

        /// <summary>
        /// Converts a string name of a platform to the matching Platform constant as defined by SeleniumHQ.
        /// </summary>
        /// <param name="p">The abbreviated name or acronym for the browser</param>
        /// <returns>The Platform constant whose name matches the submitted value</returns>
        public static Platform GetPlatform(string p) {
            return new Platform((PlatformType)Enum.Parse(typeof(PlatformType), p, true));
        }

    }
}

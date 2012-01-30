using System;
using OpenQA.Selenium;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.Specialized;
using System.Threading;
using System.Text.RegularExpressions;

namespace SeleniumScriptRunner {
    public class SeleniumWebDriverCommands {
        private string baseURL;
        private IWebDriver driver;
        private StringCollection verificationErrors;
        private StringCollection executedElements;
        private Exception runtimeException;
        private NameValueCollection scriptVars;

        #region Public usage methods
        public SeleniumWebDriverCommands(IWebDriver driver) {
            this.driver = driver;
        }

        public void RunScript(SeleniumScript script) {
            Assert.IsInstanceOf<IWebDriver>(driver, @"The WebDriver instance for this test class has not been created.  It must be created and configured by the test class during fixture setup or test setup.");
            this.verificationErrors = new StringCollection();
            this.executedElements = new StringCollection();
            this.scriptVars = new NameValueCollection();
            this.runtimeException = null;
            this.baseURL = script.BaseURL;
            foreach (SeleniumScriptLine line in script.Lines) {
                this.DoCommand(line, driver);
            }
        }

        public void DoCommand(SeleniumScriptLine line, IWebDriver driver) {
            string key = line.Command.ToLower();
            if (Commands.ContainsKey(key) == false) throw new NotSupportedException(string.Format(@"Selenium command '{0}' is not supported by this script runner", key));
            try {
                Commands[key].Invoke(this, new object[] { line, driver });
            } catch (AssertionException e) {
                if (isAssertion(line)) throw;
                verificationErrors.Add(e.Message);
            }
        }

        public StringCollection VerificationErrors { get { return this.verificationErrors; } }
        #endregion

        #region Selenium script commands
        [SeleniumWDCommand(@"open")]
        public void OpenUrl(SeleniumScriptLine line, IWebDriver driver) {
            try {
                driver.Navigate().GoToUrl(this.baseURL + line.Target);
            } catch (Exception ex) {
                this.runtimeException = ex;
                throw;
            }
        }

        [SeleniumWDCommand(@"verifyTitle")]
        [SeleniumWDCommand(@"assertTitle")]
        public void checkTitle(SeleniumScriptLine line, IWebDriver driver) {
            try {
                Assert.AreEqual(line.Target, driver.Title);
            } catch (Exception ex) {
                this.runtimeException = ex;
                throw;
            }
        }

        [SeleniumWDCommand(@"verifyValue")]
        [SeleniumWDCommand(@"assertValue")]
        [SeleniumWDCommand(@"verifyText")]
        [SeleniumWDCommand(@"assertText")]
        public void checkValueOrText(SeleniumScriptLine line, IWebDriver driver) {
            IWebElement elem = driver.FindElement(FindBy(line));
            Assert.IsNotNull(elem);
            string actual = elem.TagName.Equals(@"input") ? elem.GetAttribute(@"value") : elem.Text;
            matchText(actual, expandVars(line.Value, this.scriptVars));
        }

        [SeleniumWDCommand(@"verifyTextPresent")]
        [SeleniumWDCommand(@"assertTextPresent")]
        [SeleniumWDCommand(@"storeTextPresent")]
        public void checkTextPresent(SeleniumScriptLine line, IWebDriver driver) {
            string textToFind = line.Target;
            if (line.Command.StartsWith(@"store")) textToFind = expandVars(line.Target, this.scriptVars);
            string xpath = string.Format(@"//*[contains(text(),'{0}')]", textToFind);
            Assert.IsNotNull(driver.FindElement(By.XPath(xpath)));
        }

        [SeleniumWDCommand(@"verifyElementPresent")]
        [SeleniumWDCommand(@"assertElementPresent")]
        public void checkElementPresent(SeleniumScriptLine line, IWebDriver driver) {
            Assert.IsNotNull(driver.FindElement(FindBy(line)));
        }

        [SeleniumWDCommand(@"click")]
        [SeleniumWDCommand(@"clickAndWait")]
        public void click(SeleniumScriptLine line, IWebDriver driver) {
            driver.FindElement(FindBy(line)).Click();
            Thread.Sleep(1000);

            SeleniumScriptLine temp = new SeleniumScriptLine(@"waitForElementPresent", "css=body", string.Empty);
            waitForElementPresence(temp, driver);
        }

        [SeleniumWDCommand(@"clear")]
        public void clear(SeleniumScriptLine line, IWebDriver driver) {
            driver.FindElement(FindBy(line)).Clear();
        }

        [SeleniumWDCommand(@"type")]
        public void type(SeleniumScriptLine line, IWebDriver driver) {
            driver.FindElement(FindBy(line)).SendKeys(line.Value);
        }

        [SeleniumWDCommand(@"waitForElementPresent")]
        [SeleniumWDCommand(@"waitForElementNotPresent")]
        public void waitForElementPresence(SeleniumScriptLine line, IWebDriver driver) {
            bool waitingForPresent = (line.Command.Contains(@"Not") == false);
            for (int second = 0; ; second++) {
                if (second >= 60) Assert.Fail("timeout");
                try {
                    if (IsElementPresent(driver, FindBy(line)) == waitingForPresent) return;
                } catch (Exception) { }
                Thread.Sleep(1000);
            }
        }

        [SeleniumWDCommand(@"storeText")]
        public void storeText(SeleniumScriptLine line, IWebDriver driver) {
            this.scriptVars[line.Value] = driver.FindElement(FindBy(line)).Text;
        }
        #endregion

        #region internal utility methods - you don't care about this stuff
        internal static void matchText(string actual, string expectedWithDiscriminant) {
            string expected = string.Empty;
            if (string.IsNullOrEmpty(expectedWithDiscriminant) == false) {
                if (expectedWithDiscriminant.StartsWith(@"exact:")) {
                    Assert.AreEqual(actual, expectedWithDiscriminant.Substring(6));
                    return;
                } else if (expectedWithDiscriminant.StartsWith(@"glob:")) {
                    Assert.IsTrue(globToRegex(expectedWithDiscriminant.Substring(5)).IsMatch(actual));
                } else if (expectedWithDiscriminant.StartsWith(@"regex:")) {
                    Assert.IsTrue(new Regex(expectedWithDiscriminant.Substring(6)).IsMatch(actual));
                }
            }
            Assert.AreEqual(actual, expected);
        }
        internal static Regex globToRegex(string glob) {
            return new Regex(
                "^" + Regex.Escape(glob).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            );
        }
        internal static string expandVars(string raw, NameValueCollection vars) {
            Regex regex = new Regex(@"\$\{(.+?)\}");
            foreach (Match m in regex.Matches(raw)) {
                raw = raw.Replace(m.Value, vars[m.Groups[1].Value]);
            }
            return raw;
        }
        internal static bool isAssertion(SeleniumScriptLine line) {
            return line.Command.StartsWith(@"assert", StringComparison.CurrentCultureIgnoreCase);
        }
        private By FindBy(SeleniumScriptLine line) {
            if (line.Target.StartsWith(@"css=")) {
                return By.CssSelector(line.Target.Replace(@"css=", @""));
            } else if (line.Target.StartsWith(@"id=")) {
                return By.Id(line.Target.Replace(@"id=", @""));
            } else if (line.Target.StartsWith(@"identifier=")) {
                return By.Id(line.Target.Replace(@"identifier=", @""));
            } else if (line.Target.StartsWith(@"name=")) {
                return By.Name(line.Target.Replace(@"name=", @""));
            } else if (line.Target.StartsWith(@"link=")) {
                return By.LinkText(line.Target.Replace(@"link=", @""));
            } else /* XPath */ {
                return By.XPath(line.Target);
            }
        }
        private bool IsElementPresent(IWebDriver driver, By finder) {
            return (driver.FindElement(finder) != null);
        }
        #endregion

        #region Command to method mapping - you don't care about this stuff
        private static IDictionary<string, MethodInfo> Commands;

        static SeleniumWebDriverCommands() {
            Commands = new Dictionary<string, MethodInfo>();
            foreach (MethodInfo mi in typeof(SeleniumWebDriverCommands).GetMethods(BindingFlags.Instance | BindingFlags.Public)) {
                foreach (SeleniumWDCommandAttribute attr in mi.GetCustomAttributes(typeof(SeleniumWDCommandAttribute), false)) {
                    Commands.Add(attr.CommandName.ToLower(), mi);
                }
            }
        }

        #endregion
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    sealed class SeleniumWDCommandAttribute : Attribute {
        readonly string commandName;

        // This is a positional argument
        public SeleniumWDCommandAttribute(string commandName) {
            this.commandName = commandName;
        }

        public string CommandName {
            get { return commandName; }
        }
    }
}

using System;
using OpenQA.Selenium;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.Specialized;
using System.Threading;
using System.Text.RegularExpressions;

using SeleniumScriptRunner.Result;

namespace SeleniumScriptRunner.Script {

    public delegate void BeforeExecuteDelegate(SeleniumScriptLine line, TestRunDescriptor desc);

    public class SeleniumWebDriverCommands {

        public delegate void OnAssertion();

        private string baseURL;
        private IWebDriver driver = null;
        private TestRunDescriptor desc = null;
        private NUnitResultAccumulator log = null;
        private StringCollection verificationErrors;
        private StringCollection executedElements;
        private Exception runtimeException;
        private NameValueCollection scriptVars;
        private BeforeExecuteDelegate beforeExecute = null;

        #region Public usage methods
        public SeleniumWebDriverCommands(IWebDriver driver, TestRunDescriptor desc) : this(driver, desc, null) { }

        public SeleniumWebDriverCommands(IWebDriver driver, TestRunDescriptor desc, Result.NUnitResultAccumulator log) : this(driver, desc, log, null) { }

        public SeleniumWebDriverCommands(IWebDriver driver, TestRunDescriptor desc, Result.NUnitResultAccumulator log, BeforeExecuteDelegate before) {
            this.driver = driver;
            this.desc = desc;
            this.log = log;
            this.beforeExecute = before;
        }

        public void RunScript(SeleniumScript script) {
            Assert.IsInstanceOf<IWebDriver>(driver, @"The WebDriver instance for this test class has not been created.  It must be created and configured by the test class during fixture setup or test setup.");
            this.verificationErrors = new StringCollection();
            this.executedElements = new StringCollection();
            this.scriptVars = new NameValueCollection();
            this.runtimeException = null;
            this.baseURL = script.BaseURL;
            foreach (SeleniumScriptLine line in script.Lines) {
                if (this.beforeExecute != null)
                    beforeExecute.Invoke(line, desc);
                this.DoCommand(line, driver);
            }
        }

        public void DoCommand(SeleniumScriptLine line, IWebDriver driver) {
            string key = line.Command.ToLower();
            if (Commands.ContainsKey(key) == false)
                throw new NotSupportedException(string.Format(@"Selenium command '{0}' is not supported by this script runner", key));
            try {
                Commands[key].Invoke(this, new object[] { line, driver });
                if (this.log != null && isAssertion(line)) {
                    this.log.AssertionPassed(desc.SuiteName, desc.FixtureName, desc.TestName);
                }
            } catch (Exception ex) {
                if (ex.GetType().Equals(typeof(AssertionException))) {
                    if (FailAssertion(desc, line, ex))
                        throw;
                    verificationErrors.Add(ex.Message);
                } else if (ex.GetType().Equals(typeof(NoSuchElementException))) {
                    if (FailAssertion(desc, line, ex))
                        throw;
                    verificationErrors.Add(ex.Message);
                } else if (ex.InnerException != null &&
                           ex.InnerException.GetType().Equals(typeof(AssertionException))) {
                    if (FailAssertion(desc, line, ex.InnerException))
                        throw;
                    verificationErrors.Add(ex.InnerException.Message);
                } else if (ex.InnerException != null &&
                           ex.InnerException.GetType().Equals(typeof(NoSuchElementException))) {
                    if (FailAssertion(desc, line, ex.InnerException))
                        throw;
                    verificationErrors.Add(ex.InnerException.Message);
                } else {
                    if (this.log != null) {
                        this.log.Exception(desc.SuiteName, desc.FixtureName, desc.TestName, ex);
                    }
                    throw;
                }
            }
        }

        private bool FailAssertion(TestRunDescriptor desc, SeleniumScriptLine line, Exception ex) {
            if (isAssertion(line)) {
                if (this.log != null) {
                    string location = string.Join(@", ", line.Command, decodeString(line.Target, this.scriptVars), decodeString(line.Value, this.scriptVars));
                    this.log.AssertionFailed(desc.SuiteName, desc.FixtureName, desc.TestName, ex.Message, location);
                }
                return true;
            }
            return false;
        }

        public StringCollection VerificationErrors { get { return this.verificationErrors; } }
        #endregion

        #region Selenium script commands
        [SeleniumWDCommand(@"open")]
        public void OpenUrl(SeleniumScriptLine line, IWebDriver driver) {
            try {
                driver.Navigate().GoToUrl(this.baseURL + decodeString(line.Target, this.scriptVars));
            } catch (Exception ex) {
                this.runtimeException = ex;
                throw;
            }
        }

        [SeleniumWDCommand(@"verifyTitle")]
        [SeleniumWDCommand(@"assertTitle")]
        public void checkTitle(SeleniumScriptLine line, IWebDriver driver) {
            try {
                Assert.AreEqual(decodeString(line.Target, this.scriptVars), driver.Title);
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
            string actual = elem.TagName.Equals(@"input") ? elem.GetAttribute(@"value") : elem.Text;
            matchText(actual, decodeString(line.Value, this.scriptVars));
        }

        [SeleniumWDCommand(@"verifyTextPresent")]
        [SeleniumWDCommand(@"assertTextPresent")]
        [SeleniumWDCommand(@"storeTextPresent")]
        public void checkTextPresent(SeleniumScriptLine line, IWebDriver driver) {
            string textToFind = decodeString(line.Target, this.scriptVars);
            string xpath = string.Format(@"//*[contains(text(),'{0}')]", textToFind);
            driver.FindElement(By.XPath(xpath));
        }

        [SeleniumWDCommand(@"verifyTextNotPresent")]
        [SeleniumWDCommand(@"assertTextNotPresent")]
        public void checkTextNotPresent(SeleniumScriptLine line, IWebDriver driver) {
            try {
                string textToFind = decodeString(line.Target, this.scriptVars);
                string xpath = string.Format(@"//*[contains(text(),'{0}')]", textToFind);
                var tmp = driver.FindElement(By.XPath(xpath));
                Assert.Fail(@"Text {0} was expected not present but was found in the document.", line.Target);
            } catch (OpenQA.Selenium.NotFoundException nfe) {
                nfe = null;
                // Success
                Assert.True(true);
            }
        }

        [SeleniumWDCommand(@"verifyElementVisible")]
        [SeleniumWDCommand(@"assertElementVisible")]
        public void checkElementVisible(SeleniumScriptLine line, IWebDriver driver) {
            Assert.IsTrue(driver.FindElement(FindBy(line)).Displayed);
        }

        [SeleniumWDCommand(@"verifyElementPresent")]
        [SeleniumWDCommand(@"assertElementPresent")]
        public void checkElementPresent(SeleniumScriptLine line, IWebDriver driver) {
            driver.FindElement(FindBy(line));
        }

        [SeleniumWDCommand(@"verifyElementNotPresent")]
        [SeleniumWDCommand(@"assertElementNotPresent")]
        public void checkElementNotPresent(SeleniumScriptLine line, IWebDriver driver) {
            try {
                var tmp = driver.FindElement(FindBy(line));
                Assert.Fail(@"Element {0} was expected not present but was found in the document.", line.Target);
            } catch (OpenQA.Selenium.NotFoundException nfe) {
                nfe = null;
                // Success
                Assert.True(true);
            }
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
            var lineValue = decodeString(line.Value, this.scriptVars);
            driver.FindElement(FindBy(line)).SendKeys(lineValue);
        }

        [SeleniumWDCommand(@"waitForElementPresent")]
        [SeleniumWDCommand(@"waitForElementNotPresent")]
        public void waitForElementPresence(SeleniumScriptLine line, IWebDriver driver) {
            bool waitingForPresent = (line.Command.Contains(@"Not") == false);
            for (int second = 0; ; second++) {
                if (second >= 60)
                    Assert.Fail("timeout");
                try {
                    if (IsElementPresent(driver, FindBy(line)) == waitingForPresent)
                        return;
                } catch (Exception) { }
                Thread.Sleep(1000);
            }
        }

        [SeleniumWDCommand(@"waitForText")]
        [SeleniumWDCommand(@"waitForTextPresent")]
        [SeleniumWDCommand(@"waitForTextNotPresent")]
        public void waitForTextPresence(SeleniumScriptLine line, IWebDriver driver) {
            bool waitingForPresent = (line.Command.Contains(@"Not") == false);
            for (int second = 0; ; second++) {
                if (second >= 60)
                    Assert.Fail("timeout");
                try {
                    var rawElement = driver.FindElement(FindBy(line));
                    if (rawElement == null) {
                        if (!waitingForPresent)
                            break;
                    } else {
                        if (waitingForPresent && !string.IsNullOrEmpty(rawElement.Text))
                            return;
                    }
                } catch (Exception) { }
                Thread.Sleep(1000);
            }
        }

        [SeleniumWDCommand(@"waitForElementVisible")]
        [SeleniumWDCommand(@"waitForElementNotVisible")]
        public void waitForElementVisibility(SeleniumScriptLine line, IWebDriver driver) {
            bool waitingForVisible = (line.Command.Contains(@"Not") == false);
            for (int second = 0; ; second++) {
                if (second >= 60)
                    Assert.Fail("timeout");
                try {
                    if (IsElementVisible(driver, FindBy(line)) == waitingForVisible)
                        return;
                } catch (Exception) { }
                Thread.Sleep(1000);
            }
        }


        [SeleniumWDCommand(@"select")]
        [SeleniumWDCommand(@"selectAndWait")]
        public void select(SeleniumScriptLine line, IWebDriver driver) {
            var rawElement = driver.FindElement(FindBy(line));  // FindBy includes the decodeString function below
            var lineValue = decodeString(line.Value, this.scriptVars);
            var select = new OpenQA.Selenium.Support.UI.SelectElement(rawElement);
            if (lineValue != null) {
                if (lineValue.StartsWith(@"label=")) {
                    select.SelectByText(lineValue.Substring(6));
                } else if (lineValue.StartsWith(@"value=")) {
                    select.SelectByValue(lineValue.Substring(6));
                } else if (lineValue.StartsWith(@"index=")) {
                    select.SelectByIndex(int.Parse(lineValue.Substring(6)));
                } else {
                    select.SelectByText(lineValue);
                }
                if (line.Command.EndsWith(@"Wait", StringComparison.CurrentCultureIgnoreCase)) {
                    SeleniumScriptLine temp = new SeleniumScriptLine(@"waitForElementPresent", "css=body", string.Empty);
                    waitForElementPresence(temp, driver);
                }
            }
        }

        [SeleniumWDCommand(@"storeText")]
        public void storeText(SeleniumScriptLine line, IWebDriver driver) {
            this.scriptVars[line.Value] = driver.FindElement(FindBy(line)).Text;
        }

        [SeleniumWDCommand(@"storeEval")]
        public void storeEval(SeleniumScriptLine line, IWebDriver driver) {
            var jsEngine = driver as OpenQA.Selenium.IJavaScriptExecutor;
            if (jsEngine == null)
                throw new NotSupportedException(@"The current driver does not support remote execution of Javascript");
            var result = jsEngine.ExecuteScript(line.Target);
            if (result == null)
                this.scriptVars[line.Value] = null;
            else
                this.scriptVars[line.Value] = result.ToString();
        }

        [SeleniumWDCommand(@"deleteCookie")]
        public void deleteCookie(SeleniumScriptLine line, IWebDriver driver) {
            // RMT Bug - this method fails on Sauce.  Since they're using a new browser for each test anyway, just keep going.
            var c = driver.Manage().Cookies.GetCookieNamed(decodeString(line.Target, this.scriptVars));
            if (c != null)
                driver.Manage().Cookies.DeleteCookie(c);
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
                    return;
                } else if (expectedWithDiscriminant.StartsWith(@"regex:")) {
                    Assert.IsTrue(new Regex(expectedWithDiscriminant.Substring(6)).IsMatch(actual));
                    return;
                } else {
                    expected = expectedWithDiscriminant;
                }
            }
            Assert.AreEqual(expected, actual);
        }
        internal static Regex globToRegex(string glob) {
            return new Regex(
                "^" + Regex.Escape(glob).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            );
        }
        internal static string decodeString(string raw, NameValueCollection vars) {
            return htmlDecode(expandVars(raw, vars));
        }
        internal static string htmlDecode(string raw) {
            return System.Net.WebUtility.HtmlDecode(raw);
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
            string lineTarget = decodeString(line.Target, this.scriptVars);
            if (lineTarget.StartsWith(@"css=")) {
                return By.CssSelector(lineTarget.Replace(@"css=", @""));
            } else if (lineTarget.StartsWith(@"id=")) {
                return By.Id(lineTarget.Replace(@"id=", @""));
            } else if (lineTarget.StartsWith(@"identifier=")) {
                return By.Id(lineTarget.Replace(@"identifier=", @""));
            } else if (lineTarget.StartsWith(@"name=")) {
                return By.Name(lineTarget.Replace(@"name=", @""));
            } else if (lineTarget.StartsWith(@"link=")) {
                return By.LinkText(lineTarget.Replace(@"link=", @""));
            } else if (lineTarget.StartsWith(@"xpath=")) {
                return By.XPath(lineTarget.Replace(@"xpath=", @""));
            } else /* XPath */ {
                return By.XPath(lineTarget);
            }
        }

        private bool IsElementPresent(IWebDriver driver, By finder) {
            return (driver.FindElement(finder) != null);
        }

        private bool IsElementVisible(IWebDriver driver, By finder) {
            IWebElement elem = driver.FindElement(finder);
            if (elem != null)
                return elem.Displayed;
            return false;
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

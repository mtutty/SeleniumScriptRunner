using OpenQA.Selenium;

namespace SeleniumScriptRunner.Tests {
    public class FakeWebDriver : IWebDriver {
        void IWebDriver.Close() {
            return;
        }

        string IWebDriver.CurrentWindowHandle {
            get { return @"mock"; }
        }

        IOptions IWebDriver.Manage() {
            throw new System.NotImplementedException();
        }

        INavigation IWebDriver.Navigate() {
            return null;
        }

        string IWebDriver.PageSource {
            get { return string.Empty; }
        }

        void IWebDriver.Quit() {
            return;
        }

        ITargetLocator IWebDriver.SwitchTo() {
            return null;
        }

        string IWebDriver.Title {
            get { return @"Page Title"; }
        }

        private string url = string.Empty;
        string IWebDriver.Url {
            get {
                return url;
            }
            set {
                url = value;
            }
        }

        System.Collections.ObjectModel.ReadOnlyCollection<string> IWebDriver.WindowHandles {
            get { return null; }
        }

        IWebElement ISearchContext.FindElement(By by) {
            return null;
        }

        System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> ISearchContext.FindElements(By by) {
            return null;
        }

        void System.IDisposable.Dispose() {
            return;
        }
    }
}

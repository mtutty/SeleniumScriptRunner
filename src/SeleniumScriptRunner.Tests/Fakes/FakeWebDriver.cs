using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using System.Collections.Specialized;

namespace SeleniumScriptRunner.Tests.Fakes {
    public class FakeWebDriver : IWebDriver {

        private FakeNavigation nav = new FakeNavigation();
        private FakeWebElement docRoot = new FakeWebElement();

        private string url = string.Empty;
        private string title;

        public FakeWebElement DocumentElement {
            get { return docRoot; }
            set { docRoot = value; }
        }

        #region IWebDriver members
        public void Close() {
            return;
        }

        public string CurrentWindowHandle {
            get { return @"win"; }
        }

        public IOptions Manage() {
            return null;
        }

        public INavigation Navigate() {
            return this.nav;
        }

        public string PageSource {
            get { return @"FakeWebDriver"; }
        }

        public void Quit() {
            return;
        }

        public ITargetLocator SwitchTo() {
            throw new NotImplementedException();
        }

        public string Title {
            get { return this.title; }
        }

        public string Url {
            get {
                return this.url;
            }
            set {
                this.url = value;
            }
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<string> WindowHandles {
            get { return new System.Collections.ObjectModel.ReadOnlyCollection<string>(new List<string>()); }
        }

        public IWebElement FindElement(By by) {
            return this.docRoot.FindElement(by);
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindElements(By by) {
            return this.docRoot.FindElements(by);
        }

        public void Dispose() {
            return;
        }
        #endregion

        public class FakeNavigation : INavigation {

            private int pointer = -1;
            private List<string> history = new List<string>();

            public void Back() {
                if (pointer > 0) pointer--;
            }

            public void Forward() {
                if (pointer < history.Count - 1) pointer++;
            }

            public void GoToUrl(Uri url) {
                history.Add(url.ToString());
                pointer = history.Count - 1;
            }

            public void GoToUrl(string url) {
                GoToUrl(new Uri(url));
            }

            public void Refresh() {
                return;
            }
        }

        public class FakeWebElement : IWebElement {

            private string text = string.Empty;
            private string tagName = string.Empty;
            private bool enabled = true;
            private bool displayed = true;
            private StringDictionary cssProperties = new StringDictionary();
            private StringDictionary attributes = new StringDictionary();
            private System.Drawing.Point location = new System.Drawing.Point(0, 0);
            private System.Drawing.Size size = new System.Drawing.Size(0, 0);
            private bool selected = false;
            private IList<IWebElement> elements = new List<IWebElement>();

            public void SetCssProperty(string propertyName, string propertyValue) {
                this.cssProperties[propertyName] = propertyValue;
            }

            public void SetAttribute(string attributeName, string attributeValue) {
                this.attributes[attributeName] = attributeValue;
            }

            public void AddElement(IWebElement elem) {
                this.elements.Add(elem);
            }

            #region IWebElement members
            public void Clear() {
                this.text = string.Empty;
            }

            public void Click() {
                return;
            }

            public bool Displayed {
                get { return this.displayed; }
                set { this.displayed = value; }
            }

            public bool Enabled {
                get { return this.enabled; }
                set { this.enabled = value; }
            }

            public string GetAttribute(string attributeName) {
                return this.attributes[attributeName];
            }

            public string GetCssValue(string propertyName) {
                return cssProperties[propertyName];
            }

            public System.Drawing.Point Location {
                get { return this.location; }
                set { this.location = value; }
            }

            public bool Selected {
                get { return this.selected; }
                set { this.selected = value; }
            }

            public void SendKeys(string text) {
                this.text = text;
            }

            public System.Drawing.Size Size {
                get { return this.size; }
                set { this.size = value; }
            }

            public void Submit() {
                return;
            }

            public string TagName {
                get { return this.tagName; }
                set { this.tagName = value; }
            }

            public string Text {
                get { return this.text; }
                set { this.text = value; }
            }

            public IWebElement FindElement(By by) {
                return null;
            }

            public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> FindElements(By by) {
                return new System.Collections.ObjectModel.ReadOnlyCollection<IWebElement>(new List<IWebElement>());
            }
            #endregion
        }
    }
}

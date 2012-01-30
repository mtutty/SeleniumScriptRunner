using System;
using NUnit.Framework;
using System.IO;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace SeleniumScriptRunner.Tests {

    [TestFixture(Category=@"Integration")]
    public class FirefoxScriptTest : SeleniumScriptTest {

        public FirefoxScriptTest()
            : base(new StringReader(Properties.Resources.Simple_Selenium_Site_Check)) {
                this.driver = new FirefoxDriver(new FirefoxProfile());
        }

        [TestFixtureTearDown()]
        public void Shutdown() {
            this.driver.Quit();
            this.driver = null;
        }
    }
}
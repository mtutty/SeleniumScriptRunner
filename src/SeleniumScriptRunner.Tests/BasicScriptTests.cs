using System;
using NUnit.Framework;
using System.IO;

namespace SeleniumScriptRunner.Tests {

    [TestFixture]
    public class BasicScriptTests {

        [TestFixtureSetUp]
        public void SetupMethods() {
        }

        [TestFixtureTearDown]
        public void TearDownMethods() {
        }

        [SetUp]
        public void SetupTest() {
        }

        [TearDown]
        public void TearDownTest() {
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void LoadNullStream() {
            Assert.IsNotNull(SeleniumScriptTest.Load(string.Empty));
        }

        [Test]
        public void LoadBasic() {
            string content = SeleniumScriptRunner.Tests.Properties.Resources.Basic;
            SeleniumScript target = SeleniumScriptTest.LoadContent(content);
            Assert.IsNotNull(target);
            Assert.AreEqual(@"MVC-Basic", target.Title);
            Assert.AreEqual(@"http://mvc.hireahelper.com/", target.BaseURL);

            Assert.AreEqual(94, target.Lines.Count);

            CheckScriptLine(target.Lines[0], @"open", @"/");
            CheckScriptLine(target.Lines[1], @"verifyTitle", @"Local Movers &amp; Day Labor | HireAHelper.com");
            CheckScriptLine(target.Lines[3], @"verifyTextPresent", @"Day Laborers");
            CheckScriptLine(target.Lines[92], @"verifyTextPresent", "Johnny &quot;B&quot; Movers");
            CheckScriptLine(target.Lines[93], @"clickAndWait", @"link=HireAHelper.com");

        }

        private void CheckScriptLine(SeleniumScriptLine line, string cmd, string tgt, string val) {
            Assert.AreEqual(cmd, line.Command);
            Assert.AreEqual(tgt, line.Target);
            Assert.AreEqual(val, line.Value);
        }

        private void CheckScriptLine(SeleniumScriptLine line, string cmd, string tgt) { CheckScriptLine(line, cmd, tgt, String.Empty); }
    }
}
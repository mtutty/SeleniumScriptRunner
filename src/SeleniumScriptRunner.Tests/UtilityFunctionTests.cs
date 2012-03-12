using System;
using NUnit.Framework;
using System.Collections.Specialized;

using SeleniumScriptRunner.Script;

namespace SeleniumScriptRunner.Tests {

    [TestFixture]
    public class UtilityFunctionTests {

        private NameValueCollection vars;

        [TestFixtureSetUp]
        public void SetupMethods() {
            vars = new NameValueCollection();
            vars.Add(@"a", @"letter a");
            vars.Add(@"movingvan", @"Moving Van");
            vars.Add(@"whoa-a-dash", @"Dashes!");
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

        [TestCase(@"b", @"b")]
        [TestCase(@"${a}", @"letter a")]
        [TestCase(@"${a} and b", @"letter a and b")]
        [TestCase(@"${whoa-a-dash} and b", @"Dashes! and b")]
        public void ExpandVarsTest(string raw, string expected) {
            Assert.AreEqual(expected, SeleniumWebDriverCommands.expandVars(raw, vars));
        }
    }
}
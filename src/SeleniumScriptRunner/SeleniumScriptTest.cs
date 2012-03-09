using NUnit.Framework;
using System.Collections.Generic;
using OpenQA.Selenium;
using System.IO;
using System;

namespace SeleniumScriptRunner {

    public abstract class SeleniumScriptTest {

        protected SeleniumScript Script;
        protected IWebDriver driver;

        [Test()]
        public void RunScriptTest() {
            SeleniumWebDriverCommands runner = new SeleniumWebDriverCommands(this.driver);
            runner.RunScript(this.Script);
        }

        public SeleniumScriptTest(string filename) {
            this.Script = SeleniumHtmlScriptParser.LoadScript(filename);
        }

        public SeleniumScriptTest(SeleniumScript script) {
            this.Script = script;
        }

        public SeleniumScriptTest(TextReader rdr) {
            this.Script = SeleniumHtmlScriptParser.LoadScript(rdr);
        }

    }

    public class SeleniumScript {
        public string Title;
        public string BaseURL;
        public IList<SeleniumScriptLine> Lines;

        public SeleniumScript(string title, string baseURL, IList<SeleniumScriptLine> lines) {
            this.Title = title;
            this.BaseURL = baseURL;
            this.Lines = lines;
        }

        public SeleniumScript(string title, string baseURL) : this(title, baseURL, new List<SeleniumScriptLine>()) { }

        public SeleniumScript() : this(@"Untitled Script", string.Empty) { }
    }

    public class SeleniumScriptLine {
        public string Command;
        public string Target;
        public string Value;

        public SeleniumScriptLine(string cmd, string tgt, string val) {
            this.Command = cmd;
            this.Target = tgt;
            this.Value = val;
        }

        public SeleniumScriptLine() : this(string.Empty, string.Empty, string.Empty) { }
    }
}

using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using HtmlAgilityPack;
using OpenQA.Selenium;

namespace SeleniumScriptRunner {

    public abstract class SeleniumScriptTest {

        protected SeleniumScript Script;
        protected IWebDriver driver;

        [Test()]
        public void RunScriptTest() {
            // TODO Run the Selenium Script
        }

        public SeleniumScriptTest(string filename) {
            this.Script = SeleniumScriptTest.Load(filename);
        }

        #region Base Class File Loading Methods

        public static SeleniumScript Load(string filename) {
            return Load(File.OpenText(filename));
        }

        public static SeleniumScript Load(TextReader rdr) {
            return LoadContent(rdr.ReadToEnd());
        }

        public static SeleniumScript LoadContent(string content) {

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(content);

            SeleniumScript ret = new SeleniumScript(ScriptTitle(doc), ScriptBaseURL(doc));

            foreach (HtmlNode scriptNode in doc.DocumentNode.SelectNodes(@"//table/tbody/tr")) {
                if (scriptNode.SelectNodes("td").Count == 3) {
                    ret.Lines.Add(new SeleniumScriptLine(
                        ValueOrDefault(scriptNode.SelectSingleNode("td[1]")),
                        ValueOrDefault(scriptNode.SelectSingleNode("td[2]")),
                        ValueOrDefault(scriptNode.SelectSingleNode("td[3]"))
                    ));
                }
            }
            return ret;
        }

        internal static string ScriptTitle(HtmlDocument doc) {
            HtmlNode node = doc.DocumentNode;
            HtmlNode title = node.SelectSingleNode("//head/title");
            return ValueOrDefault(title, @"Untitled Script");
        }

        internal static string ScriptBaseURL(HtmlDocument doc) {
            HtmlNode node = doc.DocumentNode;
            HtmlNode link = node.SelectSingleNode("//link[@rel=\"selenium.base\"]");
            return AttributeValueOrDefault(link, @"href", string.Empty);
        }

        internal static string AttributeValueOrDefault(HtmlNode node, string attributeName, string defaultValue) {
            if (node == null) return defaultValue;
            if (node.Attributes.Contains(attributeName) == false) return defaultValue;
            return node.Attributes[attributeName].Value;
        }

        internal static string ValueOrDefault(HtmlNode node) {
            return ValueOrDefault(node, string.Empty);
        }

        internal static string ValueOrDefault(HtmlNode node, string defaultValue) {
            if (node == null) return defaultValue;
            if (!string.IsNullOrEmpty(node.InnerText)) return node.InnerText;
            return node.InnerText;
        }

        #endregion
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

using System.Collections.Generic;
using System.IO;
using HtmlAgilityPack;

namespace SeleniumScriptRunner.Script {
    public class SeleniumHtmlScriptParser {
        private SeleniumHtmlScriptParser() { /* Not creatable */ }

        public static IDictionary<string, string> LoadSuite(string filename) {
            return LoadSuite(File.OpenText(filename));
        }

        public static IDictionary<string, string> LoadSuite(TextReader rdr) {
            return LoadSuiteContent(rdr.ReadToEnd());
        }

        public static IDictionary<string, string> LoadSuiteContent(string content) {

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(content);

            var ret = new Dictionary<string, string>();
            foreach (HtmlNode linkNode in doc.DocumentNode.SelectNodes(@"//table[@class='selenium']//a")) {
                ret.Add(linkNode.InnerText, linkNode.GetAttributeValue(@"href", string.Empty));
            }
            return ret;
        }

        public static SeleniumScript LoadScript(string filename) {
            var rdr = File.OpenText(filename);
            var ret = LoadScript(rdr);
            rdr.Close();
            return ret;
        }

        public static SeleniumScript LoadScript(TextReader rdr) {
            return LoadScriptContent(rdr.ReadToEnd());
        }

        public static SeleniumScript LoadScriptContent(string content) {

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
            return System.Net.WebUtility.HtmlDecode(node.Attributes[attributeName].Value);
        }

        internal static string ValueOrDefault(HtmlNode node) {
            return ValueOrDefault(node, string.Empty);
        }

        internal static string ValueOrDefault(HtmlNode node, string defaultValue) {
            if (node == null || node.InnerText == null) return defaultValue;
            return System.Net.WebUtility.HtmlDecode(node.InnerText);
        }

    }
}

using System.IO;
using HtmlAgilityPack;

namespace SeleniumScriptRunner {
    public class SeleniumHtmlScriptParser {
        private SeleniumHtmlScriptParser() { /* Not creatable */ }

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

    }
}

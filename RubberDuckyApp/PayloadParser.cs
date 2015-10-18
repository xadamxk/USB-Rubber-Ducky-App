using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI.WebControls;

namespace RubberDuckyApp
{
    public struct Payload
    {
        public string Link { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
    }

    static class PayloadParser
    {
        public static List<Payload> Parse(string source)
        {
            var payloads = new List<Payload>();

            // 1.
            // Find all matches in file.
            var payloadMatches = Regex.Matches(source, @"(<a.*?>.*?</a>)",  RegexOptions.Singleline);

            // 2.
            // Loop over each match.
            foreach (Match payloadMatch in payloadMatches)
            {
                var payloadMatchValue = payloadMatch.Groups[1].Value;

                // 3. Declare new object to contain parsed link name title.
                var payload = new Payload();
                 
                // 4.
                // Get href attribute.
                var linkMatch = Regex.Match(payloadMatchValue, @"href=\""(.*?)\""", RegexOptions.Singleline);
                if (!linkMatch.Success)
                    continue;

                var path = HttpUtility.HtmlDecode(linkMatch.Groups[1].Value);
                payload.Link = "https://github.com" + path;
                
                // 5.
                // Remove inner tags from text.
                var name = Regex.Replace(payloadMatchValue, @"\s*<.*?>\s*", "", RegexOptions.Singleline);
                payload.Name = name;
                payloads.Add(payload);
            }

            return payloads;
        }
    }
}
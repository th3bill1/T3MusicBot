using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Genius;
using HtmlAgilityPack;
using static System.Net.Mime.MediaTypeNames;

namespace KebabBot.Services
{
    public class GeniusService
    {
        private GeniusClient _client;
        private string _token;
        private readonly string token_location = "C:\\Program Files\\KebabBot\\genius_token.txt";

        public GeniusService()
        {
            using (StreamReader sr = new(token_location)) _token = sr.ReadToEnd();
            _client = new GeniusClient(_token);
        }

        public async Task<string> GetSongLyrics(string name)
        {
            var search = await _client.SearchClient.Search(name);
            foreach (var hit in search.Response.Hits)
            {
                if(hit.Type == "song")
                {
                    var lyrics_class = "Lyrics__Container-sc-1ynbvzw-5 Dzxov";
                    var path = hit.Result.Path;
                    var url = "https://genius.com" + path;
                    Console.WriteLine(url);
                    var web = new HtmlWeb();
                    var doc = web.Load(url);
                    var lyricsNode = doc.DocumentNode.Descendants("div").Where(x => x.Attributes.Any(c => c.Value == "lyrics-root")).FirstOrDefault();
                    var sb = new StringBuilder();
                    Console.WriteLine(lyricsNode.InnerText.Length);
                    foreach (var node in lyricsNode.DescendantsAndSelf())
                    {
                        if (!node.HasChildNodes)
                        {
                            string text = node.InnerText;
                            if (!string.IsNullOrEmpty(text))
                            { 
                                
                                sb.AppendLine(text.Trim()); 
                            }
                                
                        }
                    }
                    var txt = sb.ToString();
                    txt = txt[(txt.IndexOf("Lyrics") + 6)..];
                    txt = txt[..^7];
                    var regex = new Regex("&#x[0-9]*;");
                    txt = regex.Replace(txt, "");
                    return txt;
                }
            }
            return null;
        }

    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Genius;

namespace KebabBot.Services
{
    public class GeniusService
    {
        private GeniusClient _client;
        private string _token;
        private readonly string token_location = "C:\\Program Files\\KebabBot\\openweather_token.txt";

        public GeniusService()
        {
            using (StreamReader sr = new(token_location)) _token = sr.ReadToEnd();
            _client = new GeniusClient(_token);
        }

        private Genius.Models.Song.Song GetSongByName(string name)
        {
            var response = _client.SearchClient.Search(name);
            return response.Result.Response.Hits.FirstOrDefault().Result;
            
        }

        public string GetLyricsByName(string name)
        {
            var song = GetSongByName(name);
            return song.EmbedContent;
        }

    }
}

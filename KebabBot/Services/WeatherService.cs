using OpenWeatherMap;
using OpenWeatherMap.Entities;
using OpenWeatherMap.Util;
using System.IO;

namespace KebabBot.Services
{
    public class WeatherService
    {
        private readonly string token_location = "C:\\Program Files\\KebabBot\\openweather_token.txt";
        private string _token;
        private OpenWeatherMapService _service;

        public WeatherService()
        {
            using(StreamReader sr = new(token_location)) _token = sr.ReadToEnd();
            _service = new OpenWeatherMapService(new OpenWeatherMapOptions { ApiKey = _token });
            RequestOptions.Default.Unit = UnitType.Metric;
        }
        public async Task<Weather> GetWeatherAsync(string city)
        {
            return await _service.GetCurrentWeatherAsync(city);
        }

    }
}

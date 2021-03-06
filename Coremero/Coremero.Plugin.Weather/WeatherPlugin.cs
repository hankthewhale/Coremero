﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Coremero.Attachments;
using Coremero.Commands;
using Coremero.Context;
using Coremero.Messages;
using Coremero.Storage;
using Coremero.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace Coremero.Plugin.Weather
{
    public class WeatherPlugin : IPlugin
    {
        private readonly string DARK_SKIES_APIKEY = "";
        private readonly string BING_MAPS_APIKEY = "";
        private readonly string USER_LOCATION_DIR = Path.Combine(PathExtensions.ResourceDir, "weather_locations.json");
        private Dictionary<string, string> _userPostcodes = new Dictionary<string, string>();
        private IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

        public WeatherPlugin(ICredentialStorage credentialStorage)
        {
            DARK_SKIES_APIKEY = credentialStorage.GetKey("darkskies", "");
            BING_MAPS_APIKEY = credentialStorage.GetKey("bing", "");
            try
            {
                _userPostcodes = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(USER_LOCATION_DIR));
            }
            catch
            {
                // ignore
            }
        }

        [Command("weather")]
        public async Task<IMessage> GetWeather(IInvocationContext context, string message)
        {
            Weather weather = new Weather(DARK_SKIES_APIKEY, BING_MAPS_APIKEY, PathExtensions.ResourceDir);
            string location = message.TrimCommand();
            if (location.Length == 0)
            {
                if (!_userPostcodes.TryGetValue(context.User.Mention, out location))
                {
                    throw new Exception("No location for user.");
                }
            }
            else
            {
                _userPostcodes[context.User.Mention] = location;
                File.WriteAllText(USER_LOCATION_DIR, JsonConvert.SerializeObject(_userPostcodes));
            }
            if (!memoryCache.TryGetValue(location, out WeatherRendererInfo forecast))
            {
                forecast = await weather.GetForecastAsync(location);
                memoryCache.Set(location, forecast, TimeSpan.FromMinutes(2));
            }
            return Message.Create("", new StreamAttachment(weather.RenderWeatherImage(forecast), "weather.png"));
        }
    }
}
using Orleans;
using Pryaniky.Orleans.BlazorApp.Shared;
using Pryaniky.Orleans.GrainInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pryaniky.Orleans.Grains
{
    public class WeatherGrain : Grain, IWeatherGrain
    {
        public async Task<IEnumerable<WeatherForecast>> GetForecastAsync()
        {
            return new List<WeatherForecast>()
            {
                new WeatherForecast(DateTime.Today.AddDays(1), 1, "Freezing"),
                new WeatherForecast(DateTime.Today.AddDays(2), 14, "Bracing"),
                new WeatherForecast(DateTime.Today.AddDays(3), -13, "Freezing"),
                new WeatherForecast(DateTime.Today.AddDays(4), -16, "Balmy"),
                new WeatherForecast(DateTime.Today.AddDays(5), -2, "Chilly")
            };
        }
    }
}

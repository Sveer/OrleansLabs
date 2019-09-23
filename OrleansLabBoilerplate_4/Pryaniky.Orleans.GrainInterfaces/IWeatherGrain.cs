using Orleans;
using Pryaniky.Orleans.BlazorApp.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Pryaniky.Orleans.GrainInterfaces
{
    public interface IWeatherGrain : IGrainWithGuidKey
    {
        Task<IEnumerable<WeatherForecast>> GetForecastAsync();
    }
}

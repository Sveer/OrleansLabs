using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using Pryaniky.Orleans.GrainInterfaces;

namespace Pryaniky.Orleans.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private IClusterClient _client;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IClusterClient client)
        {
            _logger = logger;
            _client = client;
       //     var count1= testStateGrain.Avadekedavra().GetAwaiter().GetResult();
         //   count1 = testStateGrain.AccessCount().GetAwaiter().GetResult();
        }



        [HttpGet]
        [Route("LockUser")]
        public async Task<bool> LockUser()
        {
            var testStateGrain = _client.GetGrain<IUserGrain>(Guid.Empty); //increment.GrainId
            return await testStateGrain.Avadekedavra();
        }


        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}

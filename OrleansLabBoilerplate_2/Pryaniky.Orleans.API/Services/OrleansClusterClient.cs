using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneBoxDeployment.OrleansUtilities;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Pryaniky.Orleans.GrainInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Pryaniky.Orleans.API.Services
{
    /// <summary>
    /// Испольузем IHostedSerivce - инстурмент дял запуска и DI 
    /// Сервисов в asp.net core 2.2+
    /// </summary>
    public class ClusterService : IHostedService
    {
        private readonly ILogger<ClusterService> logger;

        public ClusterService(ILogger<ClusterService> logger, IOptions<ClusterConfig> cc)
        {
            this.logger = logger;

            ClusterConfig clusterConfig = cc.Value;

            Client = new ClientBuilder()
                .ConfigureApplicationParts(manager => manager.AddApplicationPart(typeof(IUserGrain).Assembly).WithReferences())
                  .UseLocalhostClustering()
                  .Configure<ClusterOptions>(options =>
                  {
                      options.ClusterId = clusterConfig.ClusterOptions.ClusterId;
                      options.ServiceId = clusterConfig.ClusterOptions.ServiceId;
                  })
                   .UseAdoNetClustering(options =>
                   {
                       options.Invariant = clusterConfig.ConnectionConfig.AdoNetConstant;
                       options.ConnectionString = clusterConfig.ConnectionConfig.ConnectionString;
                   })
                  .Configure<EndpointOptions>(options =>
                  {
                      //Если IP кластера не задан, то исполььузем LoopBack
                      options.AdvertisedIPAddress = clusterConfig.EndPointOptions.AdvertisedIPAddress ?? IPAddress.Loopback;
                      options.GatewayListeningEndpoint = clusterConfig.EndPointOptions.GatewayListeningEndpoint;
                      options.GatewayPort = clusterConfig.EndPointOptions.GatewayPort;
                      options.SiloListeningEndpoint = clusterConfig.EndPointOptions.SiloListeningEndpoint;
                      options.SiloPort = clusterConfig.EndPointOptions.SiloPort;
                  })
                   .Configure<SiloMessagingOptions>(options =>
                   {
                       options.ResponseTimeout = TimeSpan.FromSeconds(5);
                   })
              
                .Build();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Client.Connect(async error =>
            {
                logger.LogError(error, error.Message);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                return true;
            });
        }

        public Task StopAsync(CancellationToken cancellationToken) => Client.Close();

        public IClusterClient Client { get; }
    }

    public static class ClusterServiceBuilderExtensions
    {
        public static IServiceCollection AddClusterService(this IServiceCollection services)
        {
            services.AddSingleton<ClusterService>();
            services.AddSingleton<IHostedService>(_ => _.GetService<ClusterService>());
            services.AddTransient(_ => _.GetService<ClusterService>().Client);
            return services;
        }
    }
}

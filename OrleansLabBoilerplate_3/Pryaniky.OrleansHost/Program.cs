//Настройки конфигуреации
//Microsoft.Extensions.Configuration.Json позволяет  считывать настройки из json-файлов
using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using OneBoxDeployment.OrleansUtilities;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Statistics;
using Orleans.ApplicationParts;
using Pryaniky.Orleans.Grains;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Pryaniky.OrleansHost
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var siloHost = BuildOrleansHost();
            await siloHost.StartAsync().ConfigureAwait(false);
            Console.WriteLine("Поехали!");
            Console.ReadLine();
            await siloHost.StopAsync().ConfigureAwait(false);

        }

        /// <summary>
        /// Создаем Silo для Orleans. 
        /// 
        /// </summary>
        /// <returns></returns>
        public static ISiloHost BuildOrleansHost()
        {
            //Стандартный сопсоб считывать настройки из разных файлов конфигшуроации
            //в зависимости от переменной окружания (Development, production и пр.)
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.orleanshost.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.orleanshost.{environmentName}.json", optional: false, reloadOnChange: true)
                .AddInMemoryCollection()
                .Build();

            //Читаем настройки из файла
            var clusterConfig = configuration.GetSection("ClusterConfig").Get<ClusterConfig>();

            var siloBuilder = new SiloHostBuilder()
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConsole();
                    logging.AddDebug();
                })
                .UsePerfCounterEnvironmentStatistics() //Используем системные счетчики для статистики работы кластера
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = clusterConfig.ClusterOptions.ClusterId;
                    options.ServiceId = clusterConfig.ClusterOptions.ServiceId;
                })
                //Альтернативно можем использовать Azure Tables, например.
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
                    //options.ResendOnTimeout = true;  Убрано в 3.0
                    //options.MaxResendCount = 5;
                })
                .UseAdoNetReminderService(options =>
                {
                    options.Invariant = clusterConfig.ReminderConfigs[0].AdoNetConstant;
                    options.ConnectionString = clusterConfig.ReminderConfigs[0].ConnectionString;
                })
                .AddAdoNetGrainStorage(clusterConfig.StorageConfigs[0].Name, options =>
                {
                    options.Invariant = clusterConfig.StorageConfigs[0].AdoNetConstant;
                    options.ConnectionString = clusterConfig.StorageConfigs[0].ConnectionString;
                })

                //Тут добавялем источники загрузки наших Grain'ов. Не заюываем подключить пространство имен "Orleans" :)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(UserGrain).Assembly).WithReferences())
                 .UseDashboard(options => { });
                

            return siloBuilder.Build();
        }
    }
}

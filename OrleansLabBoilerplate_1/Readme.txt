#Lab1. ������������ ������ �� ������� Microsopft Orleans

��� ����� ��� ����������:
1. Visual Studio 2019 Preview 4 - ����������� ������� (https://docs.microsoft.com/en-us/visualstudio/releases/2019/release-notes-preview)
2. .NET Core 3.0 RC1 (https://dotnet.microsoft.com/download/dotnet-core/3.0)
3. ���������� ������� Blazor ��� Visual Studio 2019:
dotnet new -i Microsoft.AspNetCore.Blazor.Templates::3.0.0-preview9.19457.4



## ��������� Boilerplate'a:
 - OneBoxDeployment.OrleansUtilities - ������ �� �������� Orleans - �������� ��������� ������� ��� ������ ������������ Silo �� json-����� ������������
 - Pryaniky.OrleansHost - ���������� ������ � �������� Silo
 - Pryaniky.Orleans.GrainInterfaces - ������ ������ ��� ����������� �������
 - Pryaniky.Orleans.Grains  - ������ ������ ��� ���������� �������

## ��������� Orleans � ����������� � SQL
### ���������������� Silo ��� ������� �������
��� ������ ������� ��������� ����� ��� ���������������� Orleans Silo
� ������� Pryaniky.OrleansHost
``` csharp
 /// <summary>
/// ������� Silo ��� Orleans. 
/// 
/// </summary>
/// <param name="args"></param>
/// <returns></returns>
public static ISiloHost BuildOrleansHost(string[] args)
{

}
```

����� ����� ���������� �� ����� ������������
��� ����� ���������� ������ OneBoxDeployment.OrleansUtilities �� ��������� ������� ������������� Orleans (������ ������� �� .NET Core 3.0). � ����� ������������ �������� ��������� Orleans:
* ClusterOptions - ��������� �������� (������������� ��������� � �������)
* EndpointOptions - ������� ���������
* ConnectionConfig - ������ ���������� � �� ��� �������� ���������� � �������� silo � ��������
* StorageConfigs - ������ ���������� ��� Storage-�����������
* ReminderConfigs - ������ ����������� ��� ������� ������������ �������

``` csharp
  //����������� ������ ��������� ��������� �� ������ ������ ��������������
            //� ����������� �� ���������� ��������� (Development, production � ��.)
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.orleanshost.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.orleanshost.{environmentName}.json", optional: false, reloadOnChange: true)
                .AddInMemoryCollection()
                .Build();
                
           //������ ��������� �� �����
            var clusterConfig = configuration.GetSection("ClusterConfig").Get<ClusterConfig>();

```

������ ������������ ���������������� �������, � ������� SiloHostBuilder.

�������� ��� ����� ������� - ������ ������������� �������� � �������, ������ ������� ��������� � ������ ���������� � �����������.
� ����� ������� �������� ����� ������� ��������� �������:

```
������ ������� ��������� Orleans (� ������������ ������ �� ������������):
.UseOrleans(builder =>
{
    builder.UseLocalhostClustering();
    builder.AddMemoryGrainStorageAsDefault();
    builder.AddMemoryGrainStorage("PubSubStore");
})
```
� �������� ������ �� ������ � ������ �������� - ������������ �������� � �������� ������-������� ����������:
```
������ ����������� ������� (� ������������ ������ �� ������������):
 builder.ConfigureApplicationParts(manager =>
    {
        manager.AddApplicationPart(typeof(HzGrain).Assembly).WithReferences();
    });
```

�� �� ������� ��������� ����� ������� ���������.
��� ������ �� ����� ������������� Azure SQL ��� ��������� SQL Server � �������� �������� �������� ��� Orleans. ����� ������� �������������� � ������� ��������� �� ����� ������������:
```
  var siloBuilder = new SiloHostBuilder()
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConsole();
                    logging.AddDebug();
                })
                .UsePerfCounterEnvironmentStatistics() //���������� ��������� �������� ��� ���������� ������ ��������
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = clusterConfig.ClusterOptions.ClusterId;
                    options.ServiceId = clusterConfig.ClusterOptions.ServiceId;
                })
                //������������� ����� ������������ Azure Tables, ��������.
                .UseAdoNetClustering(options =>
                {
                    options.Invariant = clusterConfig.ConnectionConfig.AdoNetConstant;
                    options.ConnectionString = clusterConfig.ConnectionConfig.ConnectionString;
                })
                .Configure<EndpointOptions>(options =>
                {
                    //���� IP �������� �� �����, �� ����������� LoopBack
                    options.AdvertisedIPAddress = clusterConfig.EndPointOptions.AdvertisedIPAddress ?? IPAddress.Loopback;
                    options.GatewayListeningEndpoint = clusterConfig.EndPointOptions.GatewayListeningEndpoint;
                    options.GatewayPort = clusterConfig.EndPointOptions.GatewayPort;
                    options.SiloListeningEndpoint = clusterConfig.EndPointOptions.SiloListeningEndpoint;
                    options.SiloPort = clusterConfig.EndPointOptions.SiloPort;
                })
                .Configure<SiloMessagingOptions>(options =>
                {
                    options.ResponseTimeout = TimeSpan.FromSeconds(5);
                    //options.ResendOnTimeout = true;  ������ � 3.0
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

                //��� ��������� ��������� �������� ����� Grain'��. �� �������� ���������� ������������ ���� "Orleans" :)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(UserGrain).Assembly).WithReferences());
```

� �������� ������� � ������� ��������� Silo Host �� ������ BuildOrleansHost
```
 return siloBuilder.Build();
```

### � ������ ��������� �� ���� ���� ��������
��� ������� SiloHost, ������ Main �� ����������� ����� � ��������:
```
 static async Task Main(string[] args)
{
    var siloHost = BuildOrleansHost();
    await siloHost.StartAsync().ConfigureAwait(false);
    Console.WriteLine("�������!");
    Console.ReadLine();
    await siloHost.StopAsync().ConfigureAwait(false);

}
```


### ������� ������ � ������� �������
��� �������� ������� ���������� ������� ��� ����:
- ������� �������� ��� ������ � �������� API
- ������� ��� �����, �������� �� ����� Grain, � ����������� ��� �������� � ���������� � ��������.�
 
�������� ��������� � ������� Pryaniky.Orleans.GrainInterfaces
�� ����� ����� ���������� ��� ������ � �������� Orleans ��� ������ �������

```
public interface IUserGrain : IGrainWithGuidKey
{
    Task<bool> Avadekedavra();  // ������� �������������
    Task<bool> SendMessage(string msg);   // ���������� ���������
}
```

����� �������� ��� �����, ������ �� ���������� �������� ��������� ������. ������� ����� �������� ������ - ������ �� ��� ���

```
  [StorageProvider(ProviderName = "TestStorage")]
    ///State - ������ ������ �� ������������ ��� ���
    public class UserGrain : Grain<bool>, IUserGrain
    {
       
        public async Task<bool> Avadekedavra()
        {
            //TODO: ������� �����
            State = true;
            //��������� ���������
            await base.WriteStateAsync();

            return true;

        }

        public async Task<bool> Voskresing()
        {
            //TODO: ������� �����
            State = false;
            //��������� ���������
            await base.WriteStateAsync();

            return true;

        }

        public async Task<bool> SendMessage(string msg)
        {
            //TODO: ������������ ��������� ��� ������������
            return true;
        }
    }
```

������ �������� ��������� ���������, ���������� ��� ������ �������.
��� ����� ����� DI ������� � ���� ������ ������:

```
        private readonly ILogger<UserGrain> _logger;
        
        public UserGrain(ILogger<UserGrain> logger)
        {
            this._logger = logger;
        }

```

�����  ����� ����� � ������:
```

public async Task<bool> SendMessage(string msg)
{
    logger.LogInformation(
        "{@GrainType} {@GrainKey} receive message {@msg}",
        GrainType, GrainKey, msg);
    return true;
}
 _
```

### ������� �������� ������� ��� ������

��� �������� �������� �������� ������� ������ ASP.NET CORE Web Application -> API (��� HTTPS, ��� ��������)

���������� ������� �� ������� ����� DI. ��� ����� �� ������������� ������ IHostedService � asp.net core
IHostedService ��������� � .NET ���������� ������� ������.
������� ������� ����� ������ �� �������� Silo, ������� ������� ��� �� 1 ������:


```
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
    /// ���������� IHostedSerivce - ���������� ��� ������� � DI 
    /// �������� � asp.net core 2.2+
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
                 // .UseLocalhostClustering()
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
                      //���� IP �������� �� �����, �� ����������� LoopBack
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
public static class ClusterServiceBuilderExtensions{
    //TODO: ������������� � ASP.NET CORE ������ �������...
}
    
}

```

����� ��� ������� � �������� ������� ���������� ����� ClusterServiceBuilderExtensions � �������-����������� ����������� IOrleansClient. ����� ����� ��� ����� ����� ����� ������������ � ������������:

```
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
```

�, ����������, �������� ������� � ����� ConfigureServices. 
���� ������� AddClusterService ������ ��������� �� �����. => ��� ���� ��������� ��������� �� ��������� ����� � ����������, � ���� appSettins.json

```
  services.Configure<ClusterConfig>(Configuration.GetSection("ClusterConfig"));
            services.AddClusterService();
```

�������� ������������ ���������� � ������. ��� ����� ������� � ���������� WeatherForecastController (������ � �������� �������) IOrleansCLient:
```
  private IClusterClient _client;

public WeatherForecastController(ILogger<WeatherForecastController> logger, IClusterClient client)
{
    _logger = logger;
    _client = client;
    
}
```
� ����� ������ ������:
```
[HttpGet]
[Route("LockUser")]
public async Task<bool> LockUser()
{
    var testStateGrain = _client.GetGrain<IUserGrain>(Guid.Empty); 
    return await testStateGrain.Avadekedavra();
}
```
����� �� ������� ������������ �� ��� ID (����� Guid.Empty) � �������� ������ ��� � �������� ���������� �������.

��� ����� - ��������� �� ������� (Multi) - OrleansHost � Pryaniky.Orleans.API � ������� ������� URL: /api/WeatherForecast/LockUser


##Lab2. ����������� Orleans DashBoard.
� Orleans ���� ������� ���������� ��� ����������� ������������������.
��� ����� ����������� ������������ ����� Orleans.Telementy
� ��� ��� ������������ ���� ������ Orleans.Dashboard.

��� ��� ����������� ���������� ��������� �����
```
Microsoft.Orleans.Dashboard
```

� ����� � Startup.cs �������� ��� ������:
```

```


##Lab3. Blazor � �������� ������� � �������� ������� ��������� ������

�� ������ ��������� ������� ������ �� Orleans, ������ ��������� ��������� ����������.
� �������� ������� ����� ������������ ����� Framework - Microsoft Blazor.
�� ��������� ��������� ���������� ����������� �� ������ C#, � ���� ������-���������� ������� (����� SignalR) ��� ����������� ���������� ����� � WebAssembly.

�� ��������� ������ �������.

�����. ��������� � ������ ���������� Blazor. 
���� � ������ � ��� ��� ������� Blazor, �� ���������� ��������� �������:
```
dotnet add template blazor
```

������ ������� Pryanky.Orleans.Blazor
��� ���� ����� ������� ��� �������:
* Pryaniky.Orleans.BlazorApp.Server - ��������� BackEnd
* Pryaniky.Orleans.BlazorApp.Client - ���������� WebAssembly

Pryaniky.Orleans.BlazorApp.Server - ����������� ������ ������ ������ ��������� ASP.NET Core ����������, ��� ���������� ����������� ����� Services � ������ ����������� ������� Orleans + ���� �������.
� Startup.cs ���������
```
   public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }
        
```
� � ConfigureServices

```
 services.Configure<ClusterConfig>(Configuration.GetSection("ClusterConfig"));

services.AddClusterService();
           
```
�� � ��������� ������������ ������ (������ ���� ���������):
* Microsoft.Orleans.Clustering.AdoNet
* Microsoft.Orleans.Core
* 
+ �� ������� ��� Swagger, ����� �������� ����������� ������������ � ������ API:
* Swashbuckle.AspNetCore.Swagger
* Swashbuckle.AspNetCore.SwaggerGen
* Swashbuckle.AspNetCore.SwaggerUI
P.S> ����� ����� Swashbuckle.AspNetCore.SwaggerUI ���� �� ��������� � ASP.NET CORE 3.0 RC1.

������ �� Blazor ��������� ��������� ������� ����������, � ������� ���� ���������� ����������� �������� ������ � ���������
������� ��������� ������� ������ � Orleans, � ����� �������� ��� �������� :)

��� ������ ����� ��� ������� ������ ��������� � DTO-��� WeatherForecast.
��������� ��������� ������, ������� �����  ��� ������������ � ������ Pryanky.Orleans.GrainInterfaces:

```
public interface IWeatherGrain : IGrainWithGuidKey
{
    Task<IEnumerable<WeatherForecast>> GetForecastAsync();
}
```

����� ������� � ����� WeatherForecast �����������, ��� �������� �������������:
```
 public WeatherForecast(DateTime date, int temperatureC, string summary)
{
    Date = date;
    TemperatureC = temperatureC;
    Summary = summary;
}
```

� ������� Grain:
```
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
```

�������� ��� �������. ��� ����� ������ ���������� WeatherForecastController:
```
public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> logger;
        private IClusterClient _client;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IClusterClient client)
        {
            this.logger = logger;
            this._client = client;
        }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            var rng = new Random();
            var testStateGrain = _client.GetGrain<IWeatherGrain>(Guid.Empty); //increment.GrainId

            return await testStateGrain.GetForecastAsync();
            //return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            //{
            //    Date = DateTime.Now.AddDays(index),
            //    TemperatureC = rng.Next(-20, 55),
            //    Summary = Summaries[rng.Next(Summaries.Length)]
            //})
            //.ToArray();
        }
    }
```

�� ��������  ���������� ����� Syste,Data.SqlClient, ��� �������� �� �� ������ ������������� � Silo.
���������, � ��������� �� ������� �������� ������. �������� ��������� :)


## Lab4. ��������� ������� - �������� � ��������

� Orleans �������� ����� ���������� ��� ���������� � ����������� ������� �� ����������� ������.
�������� ������ ������������ ������� Register.
� ��� �� ������� "��������" ��� �������, ����������� �������� ���������. � ����� �������� ��������� � ��������� ��������-���������. ��� ��������� ����� ���������  ������� � �������� ������ ����������� � ������� ����������� ��������.

������� ������� ��������� TODO-���� � ���� ���������� Blazor, ��� ���������� ���� �������.

��� ������ �������� ������ ������ ��� ������ ������ (ToDoItem � ������ ToDoList). ��������� �� � ������ GrainInterfaces

```
using Orleans.Concurrency;
using System;

namespace Sample.Grains.Models
{
    [Immutable]
    public class TodoItem : IEquatable<TodoItem>
    {
        public TodoItem(Guid key, string title, bool isDone, Guid ownerKey)
            : this(key, title, isDone, ownerKey, DateTime.UtcNow)
        {
        }

        protected TodoItem(Guid key, string title, bool isDone, Guid ownerKey, DateTime timestamp)
        {
            Key = key;
            Title = title;
            IsDone = isDone;
            OwnerKey = ownerKey;
            Timestamp = timestamp;
        }

        public Guid Key { get; }
        public string Title { get; }
        public bool IsDone { get; }
        public Guid OwnerKey { get; }
        public DateTime Timestamp { get; }

        public bool Equals(TodoItem other)
        {
            if (other == null) return false;
            return Key == other.Key
                && Title == other.Title
                && IsDone == other.IsDone
                && OwnerKey == other.OwnerKey
                && Timestamp == other.Timestamp;
        }

        public TodoItem WithIsDone(bool isDone) =>
            new TodoItem(Key, Title, isDone, OwnerKey, DateTime.UtcNow);

        public TodoItem WithTitle(string title) =>
            new TodoItem(Key, title, IsDone, OwnerKey, DateTime.UtcNow);
    }
}
```

����� ������� �����-�������
```
  public interface ITodoManagerGrain : IGrainWithGuidKey
    {
        Task RegisterAsync(Guid itemKey);
        Task UnregisterAsync(Guid itemKey);

        Task<ImmutableArray<Guid>> GetAllAsync();
    }
```

```
 public interface ITodoGrain : IGrainWithGuidKey
    {
        Task SetAsync(TodoItem item);

        Task ClearAsync();

        Task<TodoItem> GetAsync();
    }
```


� ���� ������:

```
 public class TodoGrain : Grain, ITodoGrain
    {
        private readonly ILogger<TodoGrain> logger;
        private readonly IPersistentState<State> state;

        private string GrainType => nameof(TodoGrain);
        private Guid GrainKey => this.GetPrimaryKey();

        public TodoGrain(ILogger<TodoGrain> logger, [PersistentState("State")] IPersistentState<State> state)
        {
            this.logger = logger;
            this.state = state;
        }

        public Task<TodoItem> GetAsync() => Task.FromResult(state.State.Item);

        public async Task SetAsync(TodoItem item)
        {
            // ensure the key is consistent
            if (item.Key != GrainKey)
            {
                throw new InvalidOperationException();
            }

            // save the item
            state.State.Item = item;
            await state.WriteStateAsync();

            // register the item with its owner list
            await GrainFactory.GetGrain<ITodoManagerGrain>(item.OwnerKey)
                .RegisterAsync(item.Key);

            // for sample debugging
            logger.LogInformation(
                "{@GrainType} {@GrainKey} now contains {@Todo}",
                GrainType, GrainKey, item);

            // notify listeners - best effort only
            GetStreamProvider("SMS").GetStream<TodoNotification>(item.OwnerKey, nameof(ITodoGrain))
                .OnNextAsync(new TodoNotification(item.Key, item))
                .Ignore();
        }

        public async Task ClearAsync()
        {
            // fast path for already cleared state
            if (state.State.Item == null) return;

            // hold on to the keys
            var itemKey = state.State.Item.Key;
            var ownerKey = state.State.Item.OwnerKey;

            // unregister from the registry
            await GrainFactory.GetGrain<ITodoManagerGrain>(ownerKey)
                .UnregisterAsync(itemKey);

            // clear the state
            await state.ClearStateAsync();

            // for sample debugging
            logger.LogInformation(
                "{@GrainType} {@GrainKey} is now cleared",
                GrainType, GrainKey);

            // notify listeners - best effort only
            GetStreamProvider("SMS").GetStream<TodoNotification>(ownerKey, nameof(ITodoGrain))
                .OnNextAsync(new TodoNotification(itemKey, null))
                .Ignore();

            // no need to stay alive anymore
            DeactivateOnIdle();
        }

        public class State
        {
            public TodoItem Item { get; set; }
        }
    }
```


```
public class TodoManagerGrain : Grain, ITodoManagerGrain
    {
        private readonly IPersistentState<State> state;

        private Guid GrainKey => this.GetPrimaryKey();

        public TodoManagerGrain([PersistentState("State")] IPersistentState<State> state)
        {
            this.state = state;
        }

        public override Task OnActivateAsync()
        {
            if (state.State.Items == null)
            {
                state.State.Items = new HashSet<Guid>();
            }

            return base.OnActivateAsync();
        }

        public async Task RegisterAsync(Guid itemKey)
        {
            state.State.Items.Add(itemKey);
            await state.WriteStateAsync();
        }

        public async Task UnregisterAsync(Guid itemKey)
        {
            state.State.Items.Remove(itemKey);
            await state.WriteStateAsync();
        }

        public Task<ImmutableArray<Guid>> GetAllAsync() =>
            Task.FromResult(ImmutableArray.CreateRange(state.State.Items));

        public class State
        {
            public HashSet<Guid> Items { get; set; }
        }
    }
```
![ead5e59106533cd3201c8d489235ab54.png](:/6aea71eb8419498ca8d6d8b7c7d1ec32)



1. �������� � ������ �������� ������ (User)
� Orleans ���� ��� �������� ��������:
Grain � ����������� ����� (��������� ��������� IGrain, IGrainWithXXXKey)
Silo � ����, ������������� ������ Grain

������  - ������� ������� Grain �������������� (User) � ��������� ��� � Silo
��� ����� � BoilerPlate ������� ����� ��������� ��� �������-����������:
- GrainInterfaces
- Grains

� GrainInterfaces ��������� ��� Nuget-������:
Orleans.Abstractions � Orleans.CodeGeneration


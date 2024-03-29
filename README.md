Обновленно до версии Orleas 3.6  и .NET 6
#Lab1. Практическая работа по основам Microsopft Orleans

Что нужно для подготовки:
1. Visual Studio 2022 - бесплатный вариант (https://docs.microsoft.com/en-us/visualstudio/releases/2022/release-notes-preview)
2. .NET 6.0 
3. Установить шаблоны Blazor для Visual Studio 2022:
dotnet new -i Microsoft.AspNetCore.Blazor.Templates

4. http://github.com/sveer/



## Структура Boilerplate'a:
 - OneBoxDeployment.OrleansUtilities - проект из примеров Orleans - содержит несколько классов для чтения конфигурации Silo из json-файла конфигурации
 - Pryaniky.OrleansHost - Консольный проект с запуском Silo
 - Pryaniky.Orleans.GrainInterfaces - пустой проект для интерфейсов грейнов
 - Pryaniky.Orleans.Grains  - пустой проект для размещения грейнов

## Запускаем Orleans с хранилищами в SQL
### Конфигурирование Silo для запуска проекта
Для начала сделаем отдельный метод для конфигурирования Orleans Silo
в проекте Pryaniky.OrleansHost
``` csharp
 /// <summary>
/// Создаем Silo для Orleans. 
/// 
/// </summary>
/// <param name="args"></param>
/// <returns></returns>
public static ISiloHost BuildOrleansHost(string[] args)
{

}
```

Будем брать настройки из файла конфигурации
Для этого подключим проект OneBoxDeployment.OrleansUtilities из классного примера использования Orleans (Только обновим до .NET 6.0). В класс десериализуем основные настройки Orleans:
* ClusterOptions - Настройки кластера (идентифкаторы кластера и сервиса)
* EndpointOptions - сетевые настройки
* ConnectionConfig - строка соединения к БД для хранения информации о членстве silo в кластере
* StorageConfigs - строки соединения для Storage-провайдеров
* ReminderConfigs - строки соединения для сервиса планировщика заданий

```csharp
            //Стандартный способ считывать настройки из разных файлов конфигурации
            //в зависимости от переменной окружения (Development, production и пр.)
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.orleanshost.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.orleanshost.{environmentName}.json", optional: false, reloadOnChange: true)
                .AddInMemoryCollection()
                .Build();
                
           //Читаем настройки из файла
            var clusterConfig = configuration.GetSection("ClusterConfig").Get<ClusterConfig>();

```

Теперь необхолдимо сконфигурировать кластер, с помощью SiloHostBuilder.

Основное что нужно сделать - задать идентифкаторы кластера и сервиса, задать сетевые настройки и строки соединения с хранилищами.
В самом простом варианте можно создать локальный кластер:



```csharp Пример простой настройки Orleans (в практической работе не используется):
.UseOrleans(builder =>
{
    builder.UseLocalhostClustering();
    builder.AddMemoryGrainStorageAsDefault();
    builder.AddMemoryGrainStorage("PubSubStore");
})
```
И добавить ссылку на сборки с нашими грейнами - виртуальными акторами с основной бизнес-логикой приложения:
```csharp Пример регистрации грейнов (в практической работе не используется):
 builder.ConfigureApplicationParts(manager =>
    {
        manager.AddApplicationPart(typeof(HzGrain).Assembly).WithReferences();
    });
```

Но мы сделаем несколько более сложную настройку.
Для начала, мы будем использовать Azure SQL или локальный SQL Server в качестве основных хранилищ для Orleans. Также добавим журналирование и сетевые настройки из файла конфигурации:
```csharp
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

                //Тут добавляем источники загрузки наших Grain'ов. Не забываем подключить пространство имен "Orleans" :)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(UserGrain).Assembly).WithReferences());
```

И остается собрать и вернуть настройки Silo Host из метода BuildOrleansHost
```csharp
 return siloBuilder.Build();
```

### А теперь попробуем со всем этим взлететь
Для запуска SiloHost, меняем Main на асинхронный вызов и получаем:
```csharp
 static async Task Main(string[] args)
{
    var siloHost = BuildOrleansHost();
    await siloHost.StartAsync().ConfigureAwait(false);
    Console.WriteLine("Поехали!");
    Console.ReadLine();
    await siloHost.StopAsync().ConfigureAwait(false);

}
```


### Создаем грейны с простой логикой
Для создания грейнов достаточно сделать две вещи:
- Создать интерфейс для грейна с понятным API
- Создать сам грейн, наследуя от класса Grain, с реализацией нашего интерфейса с блекджеком и методами.
 
Интерфейс реализуем в проекта Pryaniky.Orleans.GrainInterfaces
Мы потом будем подключать этот проект к клиентам Orleans для вызова методов

```csharp
public interface IUserGrain : IGrainWithGuidKey
{
    Task<bool> Avadekedavra();  // Удаляем пользоваателя
    Task<bool> SendMessage(string msg);   // Отправляем сообщение
}
```

Далее релизуем сам грейн, вместе со структурой хранения состояния грейна. Хранить будем состояние грейна - удален он или нет

```csharp
  [StorageProvider(ProviderName = "TestStorage")]
    ///State - хранит удален ли пользователь или нет
    public class UserGrain : Grain<bool>, IUserGrain
    {
       
        public async Task<bool> Avadekedavra()
        {
            //TODO: Удалить юзера
            State = true;
            //Сохраняем состояние
            await base.WriteStateAsync();

            return true;

        }

        public async Task<bool> Voskresing()
        {
            //TODO: Удалить юзера
            State = false;
            //Сохраняем состояние
            await base.WriteStateAsync();

            return true;

        }

        public async Task<bool> SendMessage(string msg)
        {
            //TODO: Опубликовать сообщение для пользвоателя
            return true;
        }
    }
```

Теперь добавим сервисные сообщения, отображаемые при вызове грейнов.
Для этого через DI добавим в класс грейна логгер:

```csharp
        private readonly ILogger<UserGrain> _logger;
        
        public UserGrain(ILogger<UserGrain> logger)
        {
            this._logger = logger;
        }

```

Тепеь  можно писать в журнал:
```csharp

public async Task<bool> SendMessage(string msg)
{
    logger.LogInformation(
        "{@GrainType} {@GrainKey} receive message {@msg}",
        GrainType, GrainKey, msg);
    return true;
}
 _
```

### Создаем простого клиента для грейна

Для создания простого клиента создаем проект ASP.NET CORE Web Application -> API (без HTTPS, для простоты)

Подключать клиента мы будем модулем через DI. Для этого мы зарегистриуем сервис IHostedService в asp.net core
IHostedService позволяет в .NET создававать фоновые задачи.
Создание клиента очень похоже на создание Silo, поэтому сделаем это за 1 проход:


```csharp
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
    /// Испольузем IHostedSerivce - инструмент для запуска и DI 
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
                      //Если IP кластера не задан, то используем LoopBack
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
    //TODO: Зарегистрируем в ASP.NET CORE Клиент сервиса...
}
    
}

```

Далее для красоты и простоты сделаем статический класс ClusterServiceBuilderExtensions с методом-расширением регистрации IOrleansClient. После этого его можно будет легко использовать в контроллерах:

```csharp
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

И, собственно, добавляем клиента в метод ConfigureServices. 
Перед вызовом AddClusterService читаем настройки из файла. => Нам надо перенести настройки из серверной части в клиентскую, в файл appSettins.json

```csharp
  services.Configure<ClusterConfig>(Configuration.GetSection("ClusterConfig"));
            services.AddClusterService();
```

Остается попробовать обратиться к грейну. Для этого добавим в контроллер WeatherForecastController (Создан в качестве примера) IOrleansCLient:
```csharp
  private IClusterClient _client;

public WeatherForecastController(ILogger<WeatherForecastController> logger, IClusterClient client)
{
    _logger = logger;
    _client = client;
    
}
```
И метод вызова грейна:
```csharp
[HttpGet]
[Route("LockUser")]
public async Task<bool> LockUser()
{
    var testStateGrain = _client.GetGrain<IUserGrain>(Guid.Empty); 
    return await testStateGrain.Avadekedavra();
}
```
Здесь мы получаем пользователя по его ID (Берем Guid.Empty) и вызываем методы как у обычного локального объекта.

Для теста - запускаем оба проекта (Multi) - OrleansHost и Pryaniky.Orleans.API и пробуем открыть URL: /api/WeatherForecast/LockUser


##Lab2. Подключаем Orleans DashBoard.
у Orleans есть готовый инструмент для мониторинга работоспособности.
Для сбора статиститки используется пакет Orleans.Telemetry.
А для визуализации есть проект Orleans.Dashboard.

Для его подключения достаточно поставить пакет
```csharp
OrleansDashboard
```

И далее в Startup.cs добавить его запуск:
```csharp
 .UseDashboard(options => { })
```


##Lab3. Blazor в качестве клиента и хранение сложной структуры данных

Мы смогли запустить простой проект на Orleans, теперь попробуем расширить функционал.
В качестве клиента будем использовать новый Framework - Microsoft Blazor.
Он позволяет создавать браузерные приложения на чистом C#, в виде Клиент-Серверного решения (через SignalR) или размещением клиентской части в WebAssembly.

Мы попробуем второй вариант.

Итак. Добавляем в проект приложение Blazor. 
Если в студии у Вас нет шаблона Blazor, то необходимо выполнить команду:
```csharp
dotnet add template blazor
```

Проект назовем Pryanky.Orleans.Blazor
При этом будут созданы два проекта:
* Pryaniky.Orleans.BlazorApp.Server - Серверный BackEnd
* Pryaniky.Orleans.BlazorApp.Client - приложение WebAssembly

Pryaniky.Orleans.BlazorApp.Server - практически полный аналог нашего тестового ASP.NET Core приложения, нам достаточно скопировать папку Services с файлом регистрации клиента Orleans + файл настроек.
В Startup.cs добавляем
```csharp
   public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }
        
```
и в ConfigureServices

```csharp
 services.Configure<ClusterConfig>(Configuration.GetSection("ClusterConfig"));

services.AddClusterService();
           
```
Ну и добавляем необходиммые пакеты (студия сама предложит):
* Microsoft.Orleans.Clustering.AdoNet
* Microsoft.Orleans.Core
* 
+ на будущее еще Swagger, чтобы получить симпатичную документацию к нашему API:
* Swashbuckle.AspNetCore.Swagger
* Swashbuckle.AspNetCore.SwaggerGen
* Swashbuckle.AspNetCore.SwaggerUI
P.S. Общий пакет Swashbuckle.AspNetCore.SwaggerUI пока не совместим с ASP.NET 6.0.

Пример на Blazor позволяет запустить простое приложение, в котором есть функционал отображения прогноза погоды и счетчика.
Давайте перенесем прогноз погоды в Orleans, а также сделаем его реальным :)

Для начала видим что прогноз погоды хранится в DTO-шке WeatherForecast.
Добавляем интерфейс грейна, который будет  это обслуживать в сборке Pryanky.Orleans.GrainInterfaces:

```csharp
public interface IWeatherGrain : IGrainWithGuidKey
{
    Task<IEnumerable<WeatherForecast>> GetForecastAsync();
}
```

Далее добавим в класс WeatherForecast конструктор, для простоты инициализации:
```csharp
 public WeatherForecast(DateTime date, int temperatureC, string summary)
{
    Date = date;
    TemperatureC = temperatureC;
    Summary = summary;
}
```

И создаем Grain:
```csharp
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

Остается его вызвать. Для этого меняем контроллер WeatherForecastController:
```csharp
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

Не забываем  добавить пакет System.Data.SqlClient, без которого мы не сможем подключиться к Silo.
Запускаем, и переходим на вкладку прогноза погоды. Получаем результат :)


## Lab4. Структуры грейнов - регистры и элементы

В Orleans довольно много механизмов для построения и оптимизации работы со структурами данных.
Наиболее часто используется паттерн Register.
В нем мы создаем "Менеджер" для грейнов, управляющий списком элементов. А сами элементы находятся в отдельных грейнах-элементах. 
Это позволяет гибко работать с памятью и довольно просто управляться с большим количеством объектов.

Давайте добавим небольшой TODO-лист в наше приложение Blazor, для управления этим списком.

Для начала создадим модели данных для грейна Задача (ToDoItem и списка ToDoList). Добавим их в сборку GrainInterfaces в подкаталог Models

```csharp
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pryaniky.Orleans.GrainInterfaces.Models
{
    [Immutable] //Позволяет фреймворку оптимизировать работу с объектом. Ex: не делать DeepCopy
    public class TodoItem : IEquatable<TodoItem>
    {
        public TodoItem()
        {

        }
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
    
     [Immutable]
    ///Класс для отправки данных через стримы(очереди)
    public class TodoNotification
    {
        public TodoNotification(Guid itemKey, TodoItem item)
        {
            ItemKey = itemKey;
            Item = item;
        }

        public Guid ItemKey { get; }
        public TodoItem Item { get; }
    }
    
    
}

```

Далее добавим грейн-регистр
```csharp
  public interface ITodoManagerGrain : IGrainWithGuidKey
    {
        Task RegisterAsync(Guid itemKey);
        Task UnregisterAsync(Guid itemKey);

        Task<ImmutableArray<Guid>> GetAllAsync();
    }
```

```csharp
 public interface ITodoGrain : IGrainWithGuidKey
    {
        Task SetAsync(TodoItem item);

        Task ClearAsync();

        Task<TodoItem> GetAsync();
    }
```


И сами грейны:

```csharp
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


```csharp
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


Теперь добавляем вьюхи для ToDo. В NavMenu:
```csharp
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="todo">
                <span class="oi oi-list-rich" aria-hidden="true"></span> Todo
            </NavLink>
        </li>
```
        
И верстку Todo.razor:
```
        
```
        
   

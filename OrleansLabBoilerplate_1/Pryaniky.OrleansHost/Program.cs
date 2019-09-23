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
          
            Console.WriteLine("Hello World!");
            Console.ReadLine();

        }

    }
}

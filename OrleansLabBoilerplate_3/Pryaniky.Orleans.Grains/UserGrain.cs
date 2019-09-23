using Orleans;
using Orleans.Providers;
using Pryaniky.Orleans.GrainInterfaces;
using System;
using System.Threading.Tasks;

namespace Pryaniky.Orleans.Grains
{
    [StorageProvider(ProviderName = "TestStorage")]
    ///State - хранит удален ли пользователь или нет
    public class UserGrain : Grain<bool>, IUserGrain
    {

        //private readonly ILogger<UserGrain> _logger;

        //public UserGrain(ILogger<UserGrain> logger)
        //{
        //    this._logger = logger;
        //}

        public async Task<bool> Avadekedavra()
        {
            //TODO: Удалить юзера
            State = true;
            await base.WriteStateAsync();
            return true;

        }

        public async Task<bool> Voskresing()
        {
            //TODO: Воскресить юзера
            State = false;
            await base.WriteStateAsync();
            return true;

        }

        public async Task<bool> SendMessage(string msg)
        {
            //TODO: Сохранить сообщение для пользвоателя
            return true;
        }
    }
}

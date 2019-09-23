using System;
using System.Threading.Tasks;
using Orleans;

namespace Pryaniky.Orleans.GrainInterfaces
{
    public interface IUserGrain : IGrainWithGuidKey
    {
        Task<bool> Avadekedavra();  // Удаляем пользоваателя
        Task<bool> Voskresing();  // Восстанавливаем пользоваателя
        Task<bool> SendMessage(string msg);   // Отправляем сообщение
    }
}

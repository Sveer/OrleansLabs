using Orleans;
using Pryaniky.Orleans.GrainInterfaces;
using System;

namespace Pryaniky.Orleans.Grains
{
    public class UserGrain: Grain<int>, IUserGrain
    {
    }
}

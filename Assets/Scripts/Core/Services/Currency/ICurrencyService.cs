using System;

namespace JigsawPrototype.Core.Services.Currency
{
    public interface ICurrencyService
    {
        int Balance { get; }
        event Action<int> BalanceChanged;

        bool TrySpend(int amount);
        void Add(int amount);
    }
}


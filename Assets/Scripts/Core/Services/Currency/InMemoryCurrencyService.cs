using System;

namespace JigsawPrototype.Core.Services.Currency
{
    public sealed class InMemoryCurrencyService : ICurrencyService
    {
        public sealed class Config
        {
            public int InitialBalance;
        }

        private int _balance;

        public InMemoryCurrencyService(Config config)
        {
            _balance = config.InitialBalance;
        }

        public int Balance => _balance;
        public event Action<int> BalanceChanged;

        public bool TrySpend(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (_balance < amount) return false;

            _balance -= amount;
            BalanceChanged?.Invoke(_balance);
            return true;
        }

        public void Add(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            _balance += amount;
            BalanceChanged?.Invoke(_balance);
        }
    }
}


using System;

namespace Universes.Core
{
    public class O_StardustWallet
    {
        public double Balance { get; private set; }
        public double TotalProduced { get; private set; }

        public event Action<double> OnBalanceChanged;
        public event Action<double> OnTotalProducedChanged;

        public void Reset(double startingAmount)
        {
            Balance = startingAmount;
            TotalProduced = startingAmount;
            OnBalanceChanged?.Invoke(Balance);
            OnTotalProducedChanged?.Invoke(TotalProduced);
        }

        public bool CanAfford(double cost) => Balance >= cost;

        public bool TrySpend(double cost)
        {
            if (!CanAfford(cost))
                return false;

            Balance -= cost;
            OnBalanceChanged?.Invoke(Balance);
            return true;
        }

        public void Add(double amount, bool countAsProduction = true)
        {
            if (amount <= 0)
                return;

            Balance += amount;
            if (countAsProduction)
                TotalProduced += amount;

            OnBalanceChanged?.Invoke(Balance);
            if (countAsProduction)
                OnTotalProducedChanged?.Invoke(TotalProduced);
        }
    }
}

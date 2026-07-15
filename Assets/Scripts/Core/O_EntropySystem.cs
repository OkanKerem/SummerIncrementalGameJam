using System;
using Universes.Core;
using Universes.Planets;
using Universes.Prestige;
using Universes.Stars;

namespace Universes.Core
{
    public class O_EntropySystem
    {
        public float Entropy { get; private set; }
        public double EntropySlowdownPurchased { get; private set; }

        public event Action<float> OnEntropyChanged;

        public void Reset()
        {
            Entropy = 0;
            EntropySlowdownPurchased = 0;
            OnEntropyChanged?.Invoke(Entropy);
        }

        public bool TryPurchaseSlowdown(O_GameBalance balance, O_StardustWallet wallet)
        {
            if (!wallet.TrySpend(balance.entropySlowdownCost))
                return false;

            EntropySlowdownPurchased += balance.entropySlowdownAmount;
            return true;
        }

        public void Tick(float deltaTime, O_GameBalance balance, O_RunModifiers modifiers, O_StarManager stars, O_PlanetManager planets)
        {
            var rate = modifiers.GetEffectiveEntropyBase(balance.baseEntropyRatePerSecond);
            rate += stars.Stars.Count * balance.entropyPerStar;
            rate += planets.CountPlanets() * balance.entropyPerPlanet;
            rate += planets.LifePlanets * balance.entropyPerLifePlanet;
            rate -= EntropySlowdownPurchased * 0.01;

            Entropy = Math.Min(100f, Entropy + (float)(rate * deltaTime));
            OnEntropyChanged?.Invoke(Entropy);
        }

        public bool IsCollapsed => Entropy >= 100f;
    }
}

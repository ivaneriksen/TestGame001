using System;
using System.Collections.Generic;

namespace TestGame001
{
    public class Economy
    {
        public int Gold { get; private set; }

        private readonly Dictionary<Type, int> purchaseCounts = new Dictionary<Type, int>();

        public Economy(int startingGold)
        {
            Gold = startingGold;
        }

        // Price rises by TowerPriceIncreaseRate for every prior purchase of this specific
        // tower type - BasicTower and SniperTower scale independently even though they
        // share the same starting price.
        public int GetCurrentPrice(Type towerType)
        {
            int count = purchaseCounts.TryGetValue(towerType, out int c) ? c : 0;
            double multiplier = Math.Pow(1 + GameConstants.TowerPriceIncreaseRate, count);
            return (int)Math.Round(GameConstants.BasicTowerBaseCost * multiplier);
        }

        public bool CanAfford(Type towerType)
        {
            return Gold >= GetCurrentPrice(towerType);
        }

        public void PurchaseTower(Type towerType)
        {
            int price = GetCurrentPrice(towerType);
            Gold -= price;
            purchaseCounts[towerType] = purchaseCounts.TryGetValue(towerType, out int c) ? c + 1 : 1;
        }

        public void AddGold(int amount)
        {
            Gold += amount;
        }
    }
}
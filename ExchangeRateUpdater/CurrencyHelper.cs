using System.Collections.Generic;

namespace ExchangeRateUpdater
{
    public static class CurrencyHelper
    {
        public static CurrencyCrossPair[] GetCurrencyCrossPairs(IList<string> collection)
        {
            var crossPairs = new CurrencyCrossPair[(collection.Count * collection.Count - collection.Count) / 2];
            var crossPairIndex = 0;
            for (int i = 0; i < collection.Count - 1; i++)
            {
                for (int j = i + 1; j < collection.Count; j++)
                {
                    crossPairs[crossPairIndex] = new CurrencyCrossPair()
                    {
                        seriesid1 = collection[i],
                        seriesid2 = collection[j]
                    };
                    crossPairIndex++;
                }
            }

            return crossPairs;
        }
    }
}
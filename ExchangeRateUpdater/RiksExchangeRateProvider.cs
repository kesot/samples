using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;

namespace ExchangeRateUpdater
{
    public class RiksExchangeRateProvider : IExchangeRateProvider, IDisposable
    {
        private const LanguageType language = LanguageType.en;
        private readonly SweaWebService webService = new SweaWebService();
        private bool disposed;
        
        /// <summary>
        ///     Should return exchange rates among the specified currencies that are defined by the source. But only those defined
        ///     by the source, do not return calculated exchange rates. E.g. if the source contains "EUR/USD" but not "USD/EUR",
        ///     do not return exchange rate "USD/EUR" with value calculated as 1 / "EUR/USD". If the source does not provide
        ///     some of the currencies, ignore them.
        /// </summary>
        public IEnumerable<ExchangeRate> GetExchangeRates(IEnumerable<Currency> currencies)
        {
            if (currencies == null)
                throw new ArgumentNullException(nameof(currencies));

            var availableSeries = webService.getAllCrossNames(language);

            var seriesToCurrenciesMap = MapSeriesOnCurrencies(availableSeries, currencies);

            var lastDate = GetLastBankDayWithinLastMonth();
            var previousDate = GetLastBankDayWithinLastMonth(1);

            var crossRates = GetCrossRatesByDates(
                previousDate,
                lastDate,
                CurrencyHelper.GetCurrencyCrossPairs(seriesToCurrenciesMap.Keys.ToList()));

            return CrossResultToExchangeRates(crossRates, seriesToCurrenciesMap);
        }

        private IEnumerable<ExchangeRate> CrossResultToExchangeRates(CrossResult crossResult,
            Dictionary<string, Currency> seriesToCurrenciesMap)
        {
            return crossResult.groups[0].series.Select(
                s =>
                    new ExchangeRate(seriesToCurrenciesMap[s.seriesid1.Trim()],
                        seriesToCurrenciesMap[s.seriesid2.Trim()],
                        Convert.ToDecimal(s.resultrows.MaxBy(r => r.date).value)));
        }

        private DateTime GetLastBankDayWithinLastMonth(int skipDays = 0)
        {
            var lastDay = webService.getCalendarDays(DateTime.Now.AddDays(-30 - skipDays), DateTime.Now)
                .Where(cd => cd.bankday == "Y")
                .OrderByDescending(cd => cd.caldate)
                .Skip(skipDays)
                .FirstOrDefault();

            if (lastDay?.caldate == null)
                throw new Exception("No bank days was found for last month");

            return lastDay.caldate.Value;
        }

        private Dictionary<string, Currency> MapSeriesOnCurrencies(IEnumerable<Series> series,
            IEnumerable<Currency> currencies)
        {
            return series.Join(
                    currencies,
                    s => s.seriesname,
                    c => c.Code, (s, cn) => new
                    {
                        SeriesId = s.seriesid,
                        Currency = cn
                    })
                .ToDictionary(x => x.SeriesId, x => x.Currency);
        }

        private CrossResult GetCrossRatesByDates(DateTime dateFrom, DateTime dateTo, CurrencyCrossPair[] crossPairs)
        {
            var crossRates = webService.getCrossRates(new CrossRequestParameters
            {
                aggregateMethod = AggregateMethodType.D,
                crossPair = crossPairs,
                datefrom = dateFrom,
                dateto = dateTo,
                languageid = LanguageType.en
            });
            return crossRates;
        }

        ~RiksExchangeRateProvider()
        {
            Dispose(false);
        }

        private void Dispose(bool manual)
        {
            if (!disposed)
            {
                if (manual)
                {
                    webService?.Dispose();
                }
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
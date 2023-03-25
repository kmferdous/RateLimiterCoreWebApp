
using CoreWebApp.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace CoreWebApp.Classes
{

    public class SlidingWindowRateLimiter : IRateLimiter
    {
        private readonly IMemoryCache _memoryCache;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public SlidingWindowRateLimiter(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        
        public async Task<bool> ConformRequestAsync(int windowSize, int maxLimit, string clientKey) 
        {
            var isRequestConforms = false;

            await _semaphore.WaitAsync();
            try
            {
                RequestWindowStatistics? clientStatistics = await GetClientStatistics(clientKey);
                var currentTime = DateTime.UtcNow.Ticks;
                var windowSizeInTicks = windowSize * TimeSpan.TicksPerSecond;
                long elapsedTimeInticks = 0;

                // new comer
                if (clientStatistics == null)
                {
                    clientStatistics = new RequestWindowStatistics()
                    {
                        CurrentWindowStartTime = currentTime,
                        CurrentWindowRequestCount = 1,
                        LastWindowRequestCount = 0
                    };
                }
                else
                {
                    elapsedTimeInticks = currentTime - clientStatistics.CurrentWindowStartTime;

                    // if we exceeded the current window by atleast two window lengths
                    if (elapsedTimeInticks >= windowSizeInTicks * 2)
                    {
                        clientStatistics.CurrentWindowStartTime = currentTime;
                        clientStatistics.LastWindowRequestCount = 0;
                        clientStatistics.CurrentWindowRequestCount = 1;
                        elapsedTimeInticks = 0;
                    }
                    // else if we exceeded or met the current window's end time since our last request
                    else if (elapsedTimeInticks >= windowSizeInTicks)
                    {
                        clientStatistics.LastWindowRequestCount = clientStatistics.CurrentWindowRequestCount;
                        clientStatistics.CurrentWindowStartTime += windowSizeInTicks;
                        clientStatistics.CurrentWindowRequestCount = 1;
                        elapsedTimeInticks -= windowSizeInTicks;
                    }
                    // with in current time window
                    else
                    {
                        clientStatistics.CurrentWindowRequestCount++;
                    }

                }
                
                var weightedRequestCount = clientStatistics.LastWindowRequestCount * Math.Floor((decimal)(windowSizeInTicks - elapsedTimeInticks) / windowSizeInTicks)
                                            + clientStatistics.CurrentWindowRequestCount;

                if (weightedRequestCount <= maxLimit)
                {
                    isRequestConforms = true;
                    await UpdateClientStatistics(clientKey, clientStatistics, TimeSpan.FromSeconds(windowSize * 2));

                    Console.WriteLine("key {0}; CurrentWindowRequestCount# {1}; LastWindowRequestCount#{2}; weightedRequestCount# {3}", clientKey, clientStatistics.CurrentWindowRequestCount, clientStatistics.LastWindowRequestCount, weightedRequestCount);
                }

            }
            catch (Exception ex) 
            {
                //log
            }
            finally
            {
                _semaphore.Release();
            }
            

            return isRequestConforms;
        }


        private Task<RequestWindowStatistics?> GetClientStatistics(string key)
        {
            return Task.Run(() => _memoryCache.TryGetValue(key, out RequestWindowStatistics? item) ? item : null);
        }

        private Task UpdateClientStatistics(string key, RequestWindowStatistics item, TimeSpan ts)
        {
            return Task.Run(() => _memoryCache.Set(key, item, ts));
        }
    }
}

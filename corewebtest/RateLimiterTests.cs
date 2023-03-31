using CoreWebApp.Classes;
using Microsoft.Extensions.Caching.Memory;

namespace corewebtest;

public class RateLimiterTests
{
    private IMemoryCache _cache; 

    [SetUp]
    public void Setup()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }


    [TestCase(5, 10, "1")]
    public async Task RateLimit_ShouldPassWhenInLimit(int windowSize, int maxLimit, string clientKey)
    {
        //arrange
        var rateLimiter = new SlidingWindowRateLimiter(_cache);
        bool isConformed = false;
        int cycleTime = (int)windowSize*1000,
            requestTime = cycleTime/maxLimit,
            cycleCount = 3;

        //act
        for(int cycle = 0; cycle < cycleCount; cycle++)
        {
            for (int req = 0; req < maxLimit; req++)
            {
                isConformed = await rateLimiter.ConformRequestAsync(windowSize, maxLimit, clientKey);
                //assert
                Assert.That(isConformed, Is.EqualTo(true));
                await Task.Delay(requestTime);
            }

        }
    }

    [TestCase(5, 10, "1")]
    public async Task RateLimit_ShouldThrow429WhenExceedLimit(int windowSize, int maxLimit, string clientKey)
    {
        //arrange
        var rateLimiter = new SlidingWindowRateLimiter(_cache);
        bool isConformed = false;
        int cycleTime = (int)(windowSize)*1000,
            requestTime = cycleTime/maxLimit - 100,
            cycleCount = 3;

        //act
        for(int cycle = 0; cycle < cycleCount; cycle++)
        {
            for (int req = 0; req < maxLimit + 1; req++)
            {
                isConformed = await rateLimiter.ConformRequestAsync(windowSize, maxLimit, clientKey);
                
                //assert
                if (req > maxLimit)     //intensional
                    Assert.That(isConformed, Is.EqualTo(false));

                await Task.Delay(requestTime);
            }

        }
    }
}
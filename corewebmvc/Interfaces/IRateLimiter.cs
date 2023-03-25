
namespace CoreWebApp.Interfaces
{

    public interface IRateLimiter
    {

        /// <summary>
        /// also update counter if request limit not exceeded
        /// </summary>
        /// <param name="windowSize">in second</param>
        /// <param name="maxLimit"></param>
        /// <param name="clientKey"></param>-
        /// <returns></returns>
        Task<bool> ConformRequestAsync(int windowSize, int maxLimit, string clientKey);

    }
}

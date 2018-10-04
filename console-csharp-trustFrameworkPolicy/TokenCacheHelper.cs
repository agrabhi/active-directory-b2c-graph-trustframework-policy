using System.IO;
using System.Security.Cryptography;
using Microsoft.Identity.Client;

namespace active_directory_wpf_msgraph_v2
{
    static class TokenCacheHelper
    {
        public static byte[] CacheContent;

        /// <summary>
        /// Get the user token cache
        /// </summary>
        /// <returns></returns>
        public static TokenCache GetUserCache()
        {
            if (usertokenCache == null)
            {
                usertokenCache = new TokenCache();
                usertokenCache.SetBeforeAccess(BeforeAccessNotification);
                usertokenCache.SetAfterAccess(AfterAccessNotification);
            }
            return usertokenCache;
        }

        static TokenCache usertokenCache;

        public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            args.TokenCache.Deserialize(CacheContent);
        }

        public static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.TokenCache.HasStateChanged)
            {
                CacheContent = args.TokenCache.Serialize();
                
                // once the write operation takes place restore the HasStateChanged bit to false
                args.TokenCache.HasStateChanged = false;
            }
        }
    }
}
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using active_directory_wpf_msgraph_v2;

namespace console_csharp_trustframeworkpolicy
{
    class AuthenticationHelper
    {
        // The test endpoint currently does not require a specific scope.
        // By public preview, this API will require Policy.ReadWrite.All permission as an admin-only scope,
        // so authorization will fail if you sign in with a non-admin account.
        // For now, this API is only accessible on tenants that have been whitelisted
        public static string[] Scopes = { "User.Read" };

        public static PublicClientApplication IdentityClientApp = new PublicClientApplication(
            Inputs.ClientId,
            string.Format(Constants.AuthorityUriFormat, Inputs.TenantId),
            TokenCacheHelper.GetUserCache());

        public static string AccessTokenForUser = null;
        public static DateTimeOffset Expiration;

        private static GraphServiceClient graphClient = null;

        // Get an access token for the given context and resourceId. An attempt is first made to 
        // acquire the token silently. If that fails, then we try to acquire the token by prompting the user.
        public static GraphServiceClient GetAuthenticatedClientForUser()
        {
            // Create Microsoft Graph client.
            try
            {
                graphClient = new GraphServiceClient(
                    "https://graph.microsoft.com/v1.0",
                    new DelegateAuthenticationProvider(
                        async (requestMessage) =>
                        {
                            var token = await GetAccessTokenForUserAsync();
                            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
                            // This header has been added to identify usage of this sample in the Microsoft Graph service.  You are free to remove it without impacting functionlity.
                            requestMessage.Headers.Add("SampleID", "console-csharp-trustframeworkpolicy");
                        }));
                return graphClient;
            }

            catch (Exception ex)
            {
                Debug.WriteLine("Could not create a graph client: " + ex.Message);
            }

            return graphClient;
        }

        public static void AddHeaders(HttpRequestMessage requestMessage)
        {
            if(AccessTokenForUser == null)
            {
                Debug.WriteLine("Call GetAuthenticatedClientForUser first");
            }

            try
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", AccessTokenForUser);
                requestMessage.Headers.Add("SampleID", "console-csharp-trustframeworkpolicy");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Could not add headers to HttpRequestMessage: " + ex.Message);
            }
        }

        /// <summary>
        /// Get Token for User.
        /// </summary>
        /// <returns>Token for user.</returns>
        public static async Task<string> GetAccessTokenForUserAsync()
        {
            AuthenticationResult authResult;
            try
            {
                authResult = await IdentityClientApp.AcquireTokenSilentAsync(Scopes, IdentityClientApp.Users.First());
                AccessTokenForUser = authResult.AccessToken;                
            }

            catch (Exception)
            {
                if (AccessTokenForUser == null || Expiration <= DateTimeOffset.UtcNow.AddMinutes(5))
                {
                    authResult = await IdentityClientApp.AcquireTokenAsync(Scopes);

                    AccessTokenForUser = authResult.AccessToken;
                    Expiration = authResult.ExpiresOn;
                }
            }

            return AccessTokenForUser;
        }

        /// <summary>
        /// Signs the user out of the service.
        /// </summary>
        public static void SignOut()
        {
            foreach (var user in IdentityClientApp.Users)
            {
                IdentityClientApp.Remove(user);
            }
            graphClient = null;
            AccessTokenForUser = null;
        }

    }
}

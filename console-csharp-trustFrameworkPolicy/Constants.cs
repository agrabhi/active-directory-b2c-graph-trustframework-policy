namespace console_csharp_trustframeworkpolicy
{
    /// <summary>
    /// Constants
    /// </summary>
    internal class Constants
    {
        /// <summary>
        /// The authority URI format/ reply Url format
        /// </summary>
        public const string AuthorityUriFormat = "https://login.microsoftonline.com/{0}/oauth2/v2.0/token";
        
        // leave these as-is - Private Preview Graph URIs for custom trust framework policy
        public const string TrustFrameworkPolicesUri = "https://graph.microsoft.com/testcpimtf/trustFrameworkPolicies";
        public const string TrustFrameworkPolicyByIDUri = "https://graph.microsoft.com/testcpimtf/trustFrameworkPolicies/{0}/$value";
    }
}

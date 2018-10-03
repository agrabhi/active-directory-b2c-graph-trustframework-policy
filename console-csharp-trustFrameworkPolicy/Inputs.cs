using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace console_csharp_trustframeworkpolicy
{
    /// <summary>
    /// Inputs by user
    /// </summary>
    internal static class Inputs
    {
        /// <summary>
        /// Gets or sets the tenant identifier.
        /// </summary>
        public static string TenantId{ get; set; }

        /// <summary>
        /// Gets or sets the client identifier.
        /// </summary>
        /// <value>
        /// The client identifier.
        /// </value>
        public static string ClientId{ get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier.
        /// </summary>
        public static string RefreshToken { get; set; }                
    }
}

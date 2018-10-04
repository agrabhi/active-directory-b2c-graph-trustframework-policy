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
        public static Commands Command { get; set; }


        /// <summary>
        /// Gets or sets the tenant identifier.
        /// </summary>
        public static string TenantId{ get; set; }

        /// <summary>
        /// Gets or sets the policy identifier.
        /// </summary>
        /// <value>
        /// The policy identifier.
        /// </value>
        public static string PolicyId{ get; set; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>
        /// The path.
        /// </value>
        public static string Path{ get; set; }

        /// <summary>
        /// Gets or sets the client identifier.
        /// </summary>
        /// <value>
        /// The client identifier.
        /// </value>
        public static string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier.
        /// </summary>
        public static string Tokens { get; set; }     
    }
}

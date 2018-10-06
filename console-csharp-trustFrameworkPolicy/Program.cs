using active_directory_wpf_msgraph_v2;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace console_csharp_trustframeworkpolicy
{
    /// <summary>
    /// Program entry point class
    /// </summary>
    class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        static void Main(string[] args)
        {
            //Console.ReadKey();

            if (!ParseCommandLine(args)
                || !ValidInputs())
            {
                PrintHelp(args);
                return;
            }

            PerformCommand();
        }

        /// <summary>
        /// Valids the inputs.
        /// </summary>
        /// <returns>if valid inputs</returns>
        private static bool ValidInputs()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            if (string.IsNullOrWhiteSpace(Inputs.TenantId))
            {
                Console.WriteLine("TenantID is a mandatory parameter for the application.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Inputs.ClientId))
            {
                Console.WriteLine("ApplicationId is a mandatory parameter for the application.");
                return false;
            }

            switch (Inputs.Command)
            {
                case Commands.LIST:
                    break;
                case Commands.UPDATE:
                    {
                        if (string.IsNullOrWhiteSpace(Inputs.PolicyId))
                        {
                            Console.WriteLine("Please specify a policy id which can be used to update or create the policy.");
                            return false;
                        }
                        if (string.IsNullOrWhiteSpace(Inputs.Path))
                        {
                            Console.WriteLine("Please specify a relative path to policy xml which can be used to upload policy content.");
                            return false;
                        }
                    }
                    break;
                case Commands.CREATE:
                    {
                        if (string.IsNullOrWhiteSpace(Inputs.Path))
                        {
                            Console.WriteLine("Please specify a relative path to policy xml which can be used to upload policy content.");
                            return false;
                        }
                    }
                    break;
                case Commands.GET:
                    if (string.IsNullOrWhiteSpace(Inputs.PolicyId))
                    {
                        Console.WriteLine("Please specify a policyId of the policy you want to download.");
                        return false;
                    }
                    if (string.IsNullOrWhiteSpace(Inputs.Path))
                    {
                        Console.WriteLine("Please specify a relative path to policy xml which can be used to download policy content.");
                        return false;
                    }
                    break;
                case Commands.DELETE:
                    {
                        if (string.IsNullOrWhiteSpace(Inputs.PolicyId))
                        {
                            Console.WriteLine("Please specify a policyId of the policy you want to delete.");
                            return false;
                        }
                    }
                    break;
                case Commands.GETTOKENS:
                    break;
                default:
                    throw new ArgumentException("Unexpected value of command.");
            }

            Console.ForegroundColor = ConsoleColor.White;
            return true; 
        }

        /// <summary>
        /// Parses the command line.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>If valid command line arguments were passed and execution should continue</returns>
        private static bool ParseCommandLine(string[] args)
        {
            if (args.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string inputArg = args[i];
                switch (inputArg.ToUpper())
                {
                    // Main operations
                    case "-LIST":
                    case "-CREATE":
                    case "-GET":
                    case "-UPDATE":
                    case "-DELETE":
                    case "-GETTOKENS":
                        Inputs.Command = (Commands)Enum.Parse(typeof(Commands), inputArg.Remove(0, 1).ToUpper());
                        break;

                    case "-T":
                    case "-TENANTID":
                        if (!OneMoreElementPresent(args.Length, i, "TenantId"))
                        {
                            return false;
                        }
                        i++;                        
                        Inputs.TenantId = args[i];
                        break;

                    case "-USETOKENS":
                        if (!OneMoreElementPresent(args.Length, i, "Tokens"))
                        {
                            return false;
                        }
                        i++;
                        Inputs.Tokens = args[i];
                        break;

                    case "-P":
                    case "-POLICY":
                        if (!OneMoreElementPresent(args.Length, i, "PolicyId"))
                        {
                            return false;
                        }
                        i++;
                        Inputs.PolicyId = args[i];
                        break;

                    case "-PATH":
                        if (!OneMoreElementPresent(args.Length, i, "Path"))
                        {
                            return false;
                        }
                        i++;
                        Inputs.Path = args[i];
                        break;

                    case "-APPID":
                        if (!OneMoreElementPresent(args.Length, i, "ApplicationId"))
                        {
                            return false;
                        }
                        i++;
                        Inputs.ClientId = args[i];
                        break;

                    default:
                        return false;                 
                }
            }

            return true;
        }

        /// <summary>
        /// Checks the one more element present.
        /// </summary>
        /// <param name="argName">corresponding argument</param>
        private static bool OneMoreElementPresent(int arrayLength, int i, string argName)
        {
            if ((i + 1) >= arrayLength)
            {
                Console.WriteLine($"{argName} is missing.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Performs the command.
        /// </summary>
        private static void PerformCommand()
        {
            HttpRequestMessage request = null;
            // ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            try
            {
                if (Inputs.Tokens != null)
                {
                    TokenCacheHelper.CacheContent = Convert.FromBase64String(Inputs.Tokens);
                }

                // Login as global admin of the Azure AD B2C tenant
                UserMode.LoginAsAdmin();

                // Graph client does not yet support trustFrameworkPolicy, so using HttpClient to make rest calls
                switch (Inputs.Command)
                {
                    case Commands.LIST:
                        {
                            // List all policies using "GET /trustFrameworkPolicies"
                            request = UserMode.HttpGet(Constants.TrustFrameworkPolicesUri);
                            var response = SendRequest(request);
                            PrintListOfPolicies(response);
                            break;
                        }
                    case Commands.GET:
                        {
                            // Get a specific policy using "GET /trustFrameworkPolicies/{id}"
                            request = UserMode.HttpGetID(Constants.TrustFrameworkPolicyByIDUri, Inputs.PolicyId);
                            var response = SendRequest(request);
                            SavePolicyToFile(response);
                            PrintGeneric("Get operation ", response);
                            break;
                        }
                    case Commands.CREATE:
                        {
                            // Create a policy using "POST /trustFrameworkPolicies" with XML in the body
                            string xml = System.IO.File.ReadAllText(Inputs.Path);
                            request = UserMode.HttpPost(Constants.TrustFrameworkPolicesUri, xml);
                            var response = SendRequest(request);
                            PrintGeneric("Create operation", response);
                            break;
                        }
                    case Commands.UPDATE:
                        {
                            // Update using "PUT /trustFrameworkPolicies/{id}" with XML in the body
                            string xml = System.IO.File.ReadAllText(Inputs.Path);
                            request = UserMode.HttpPutID(Constants.TrustFrameworkPolicyByIDUri, Inputs.PolicyId, xml);
                            var response = SendRequest(request);
                            PrintGeneric("Update operation", response);
                            break;
                        }
                    case Commands.DELETE:
                        {
                            // Delete using "DELETE /trustFrameworkPolicies/{id}"
                            request = UserMode.HttpDeleteID(Constants.TrustFrameworkPolicyByIDUri, Inputs.PolicyId);
                            var response = SendRequest(request);
                            PrintGeneric("Delete operation", response);
                            break;
                        }
                    case Commands.GETTOKENS:
                        {
                            PrintTokens();
                            break;
                        }
                    default:
                        return;
                }
            }
            catch (Exception e)
            {
                Print(request);
                Console.WriteLine("\nError {0} {1}", e.Message, e.InnerException != null ? e.InnerException.Message : "");
            }
        }

        /// <summary>
        /// Prints the tokens.
        /// </summary>
        private static void PrintTokens()
        {
            //string token = AuthenticationHelper.GetAccessTokenForUserAsync().Result;
            // Console.WriteLine($"Access token: Bearer {token}");

            // string cacheContent = System.Text.Encoding.Default.GetString(TokenCacheHelper.CacheContent);

            string cacheContent = Convert.ToBase64String(TokenCacheHelper.CacheContent);

            Console.WriteLine("--------------Token cache------");
            Console.Write(cacheContent);

            //var obj = JArray.Parse(cacheContent);
            //Console.Write(obj.ToString(Newtonsoft.Json.Formatting.Indented));
        }

        /// <summary>
        /// Prints the generic.
        /// </summary>
        /// <param name="ops">The v.</param>
        /// <param name="response">The response.</param>
        private static void PrintGeneric(string ops, HttpResponseMessage response)
        {
            string content = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Error Calling the Graph API HTTP Status={0}", response.StatusCode);
                Console.WriteLine(content);
                return;
            }

            Console.WriteLine($"{ops} completed successfully");

        }

        /// <summary>
        /// Saves the policy to file.
        /// </summary>
        /// <param name="response">The response.</param>
        private static void SavePolicyToFile(HttpResponseMessage response)
        {
            string content = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Error Calling the Graph API HTTP Status={0}", response.StatusCode);
                Console.WriteLine(content);
                return;
            }

            File.WriteAllText(Inputs.Path, content);
        }

        /// <summary>
        /// Prints the list of policies.
        /// </summary>
        /// <param name="response">The response.</param>
        private static void PrintListOfPolicies(HttpResponseMessage response)
        {
            string content = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Error Calling the Graph API HTTP Status={0}", response.StatusCode);
                Console.WriteLine(content);
                return;
            }            

            JObject obj = JObject.Parse(content);
            JArray list = (JArray) obj["value"];

            Console.WriteLine("\n\nFollowing custom policies exist in the tenant");

            foreach (JToken token in list)
            {
                // policyList.Add(token["id"].ToString());
                Console.WriteLine(token["id"].ToString());
            }
        }

        /// <summary>
        /// Sends the request.
        /// </summary>
        /// <param name="request">The request.</param>
        private static HttpResponseMessage SendRequest(HttpRequestMessage request)
        {
            HttpClient httpClient = new HttpClient();
            Task<HttpResponseMessage> response = httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            return response.Result;
        }

        public static void Print(Task<HttpResponseMessage> responseTask)
        {
            responseTask.Wait();
            HttpResponseMessage response = responseTask.Result;

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Error Calling the Graph API HTTP Status={0}", response.StatusCode);
            }

            Console.WriteLine(response.Headers);
            Task<string> taskContentString = response.Content.ReadAsStringAsync();
            taskContentString.Wait();
            Console.WriteLine(taskContentString.Result);
        }

        public static void Print(HttpRequestMessage request)
        {
            if(request != null)
            {
                Console.Write(request.Method + " ");
                Console.WriteLine(request.RequestUri);
                Console.WriteLine("");
            }
        }

        /// <summary>
        /// Prints the help.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static void PrintHelp(string[] args)
        {
            string appName = "B2CPolicyClient";
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n\nHelp text for B2CPolicyClient app--------------------------------------------------");
            Console.WriteLine("- Square brackets indicate optional arguments");
            Console.WriteLine(
                "- If valid encoded tokens are passed, they are used as credential, else an interactive flow will be invoked. " +
                "\n- The encoded tokens are retrieved using Tokens command. The tokens in output of the command is supplied back " +
                "\nif using -usetokens option.");
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n1. Print encoded Tokens                   \n\t{appName} -Tokens -tenant <TenantId> -appId <appId>");
            Console.WriteLine($"\n2. List                                   \n\t{appName} -List -tenant <Tenant> -appId <appId> [-UseTokens] [<EncodedTokens>]");
            Console.WriteLine($"\n3. Download policy content to a file      \n\t{appName} -Get -p <PolicyID> -tenant <TenantId> -appId <appId> -path <filePath> [-UseTokens] [<EncodedTokens>]");
            Console.WriteLine($"\n4. Create policy from a file              \n\t{appName} -Create -tenant <TenantId> -appId <appId> -path <filePath> [-UseTokens] [<EncodedTokens>]");
            Console.WriteLine($"\n5. Update or create a policy from a file  \n\t{appName} -Update -tenant <TenantId> -appId <appId> -p <PolicyId> -path <filePath> [-UseTokens] [<EncodedTokens>]");
            Console.WriteLine($"\n6. Delete                                 \n\t{appName} -Delete -tenant <TenantId> -appId <appId> -p <PolicyId> [-UseTokens] [<EncodedTokens>]");
            Console.WriteLine($"\n7. Help                                   \n\t{appName} -Help");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("");
        }
    }
}

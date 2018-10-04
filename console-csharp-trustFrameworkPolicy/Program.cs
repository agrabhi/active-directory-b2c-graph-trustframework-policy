using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace console_csharp_trustframeworkpolicy
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ReadKey();

            // validate parameters
            if (!CheckValidParameters(args))
            {
                return;
            }

            if (!ParseCommandLine(args))
            {
                return;
            }

            PerformCommand();
        }

        private static bool ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string inp = args[i];
                switch (inp.ToUpper())
                {
                    case "-LIST":
                    case "-CREATE":
                    case "-GET":
                    case "-UPDATE":
                    case "-DELETE":
                        Inputs.Command = (Commands)Enum.Parse(typeof(Commands), inp.Remove(0, 1).ToUpper());
                        break;

                    case "-TENANTID":
                        // TODO: Length check here
                        i++;
                        Inputs.TenantId = args[i];
                        break;

                    case "-P":
                    case "-POLICY":
                        // TODO: Length check here
                        i++;
                        Inputs.PolicyId = args[i];
                        break;

                    case "-PATH":
                        // TODO: Length check here
                        i++;
                        Inputs.Path = args[i];
                        break;

                    case "-APPID":
                        // TODO: Length check here
                        i++;
                        Inputs.ClientId = args[i];
                        break;

                    default:
                        PrintHelp(args);
                        return false;                 
                }
            }

            return true;
        }

        private static void PerformCommand()
        {
            HttpRequestMessage request = null;
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            try
            {
                // Login as global admin of the Azure AD B2C tenant
                UserMode.LoginAsAdmin();

                // Graph client does not yet support trustFrameworkPolicy, so using HttpClient to make rest calls
                switch (Inputs.Command)
                {
                    case Commands.LIST:
                        {
                            // List all polcies using "GET /trustFrameworkPolicies"
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
        /// Prints the generic.
        /// </summary>
        /// <param name="ops">The v.</param>
        /// <param name="response">The response.</param>
        private static void PrintGeneric(string ops, HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Error Calling the Graph API HTTP Status={0}", response.StatusCode);
                return;
            }

            Console.WriteLine($"{ops} completes successfully");

        }

        /// <summary>
        /// Saves the policy to file.
        /// </summary>
        /// <param name="response">The response.</param>
        private static void SavePolicyToFile(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Error Calling the Graph API HTTP Status={0}", response.StatusCode);
                return;
            }

            string content = response.Content.ReadAsStringAsync().Result;
            File.WriteAllText(Inputs.Path, content);
        }

        /// <summary>
        /// Prints the list of policies.
        /// </summary>
        /// <param name="response">The response.</param>
        private static void PrintListOfPolicies(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Error Calling the Graph API HTTP Status={0}", response.StatusCode);
                return;
            }

            string content = response.Content.ReadAsStringAsync().Result;

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

        public static bool CheckValidParameters(string[] args)
        {
            // TODO: Validate inputs
            return true;
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

        private static void PrintHelp(string[] args)
        {
            string appName = "B2CPolicyClient";
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("- Square brackets indicate optional arguments");
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Tokens                       : {0} Tokens -tenant {Tenant} -appId {appId} -replyUri {replyUri}", appName);
            Console.WriteLine("List                         : {0} -List -tenant {Tenant} -appId {appId} -replyUri {replyUri}", appName);
            Console.WriteLine("Get                          : {0} -Get -p [PolicyID] -tenant {Tenant} -appId {appId} -replyUri {replyUri}", appName);
            Console.WriteLine("                             : {0} -Get -p B2C_1A_PolicyName -tenant {Tenant} -appId {appId} -replyUri {replyUri}", appName);
            Console.WriteLine("Create                       : {0} -Create -path [RelativePathToXML] -tenant {Tenant} -appId {appId} -replyUri {replyUri}", appName);
            Console.WriteLine("                             : {0} -Create -path policytemplate.xml -tenant {Tenant} -appId {appId} -replyUri {replyUri}", appName);
            Console.WriteLine("Update                       : {0} -Update -p [PolicyID] [RelativePathToXML] -tenant {Tenant} -appId {appId} -replyUri {replyUri}", appName);
            Console.WriteLine("                             : {0} -Update -p B2C_1A_PolicyName -path updatepolicy.xml -tenant {Tenant} -appId {appId} -replyUri {replyUri}", appName);
            Console.WriteLine("Delete                       : {0} -Delete -p [PolicyID] -tenant {Tenant} -appId {appId} -replyUri {replyUri}", appName);
            Console.WriteLine("                             : {0} -Delete -p B2C_1A_PolicyName -tenant {Tenant} -appId {appId} -replyUri {replyUri}", appName);
            Console.WriteLine("Help                         : {0} -Help", appName);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("");

            if(args.Length == 0)
            {
                Console.WriteLine("[press any key to exit]");
                Console.ReadKey();
            }
        }
    }
}

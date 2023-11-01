using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Net;

namespace CMS.Legacy.Adapters
{
    public static class OLIProxy
    {




        [FunctionName("Next")]
        public static async Task<IActionResult> ProcessNext(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Next")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");


            if (!CheckOLIBasicAuth(req))
            {
                return new UnauthorizedResult();
            }



            //Note: this is expected as a complete URI without the secret query parameters
            string ActualNextFunctionPath = System.Environment.GetEnvironmentVariable("Proxy:ActualNextFunctionPath", EnvironmentVariableTarget.Process);

            //Mote this is expected to be the name=value query parameter, either sig=... or code=...
            string ActualNextFunctionSecret = System.Environment.GetEnvironmentVariable("Proxy:ActualNextFunctionSecret", EnvironmentVariableTarget.Process);


            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            using (var httpClient = new HttpClient())
            {
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(ActualNextFunctionPath + "?" + ActualNextFunctionSecret, content);

                var responseString = await response.Content.ReadAsStringAsync();

                return new ObjectResult(responseString) { StatusCode = (int)response.StatusCode };
            }
        }



        [FunctionName("Acknowledge")]
        public static async Task<IActionResult> DoAcknowledge(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "{submissionId}/Acknowledge")] HttpRequest req, string submissionId,
           ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            if (!CheckOLIBasicAuth(req))
            {
                return new UnauthorizedResult();
            }

            //Note: this is expected as a complete URI, *including* the submissionid querystring, without the secret query parameters
            //e.g. https://some-fqdn/api/acknowledge?submissionid=
            string ActualAcknowledgeFunctionPath = System.Environment.GetEnvironmentVariable("Proxy:ActualAcknowledgeFunctionPath", EnvironmentVariableTarget.Process);

            //Mote this is expected to be the name=value query parameter, either sig=... or code=...
            string ActualAcknowledgeFunctionSecret = System.Environment.GetEnvironmentVariable("Proxy:ActualAcknowledgeFunctionSecret", EnvironmentVariableTarget.Process);


            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            using (var httpClient = new HttpClient())
            {
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(ActualAcknowledgeFunctionPath + submissionId + "&" + ActualAcknowledgeFunctionSecret, content);

                var responseString = await response.Content.ReadAsStringAsync();

                return new ObjectResult(responseString) { StatusCode = (int)response.StatusCode };
            }
        }


        [FunctionName("LocalNextTester")]
        public static async Task<IActionResult> DoLocalNextTest(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            return new OkObjectResult("I go next next next");
        }


        [FunctionName("LocalAcknowledgeTester")]
        public static async Task<IActionResult> DoLocalAcknowledgeTest(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req, 
        ILogger log)
        {
          
          string formSubmissionId = req.Query["formSubmissionId"];
          return new OkObjectResult("I acknowledge that you sent:" + formSubmissionId);
        }

        public static bool CheckOLIBasicAuth(HttpRequest req)
        {
            string authHeader = req.Headers["Authorization"];
            if (authHeader != null && authHeader.StartsWith("Basic"))
            {
                //Extract credentials from header
                string encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();
                Encoding encoding = Encoding.GetEncoding("iso-8859-1");
                string usernamePassword = encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));

                int seperatorIndex = usernamePassword.IndexOf(':');

                string OLIUsername = System.Environment.GetEnvironmentVariable("Proxy:OLIUserName", EnvironmentVariableTarget.Process);
                string OLIPassword = System.Environment.GetEnvironmentVariable("Proxy:OLIPassword", EnvironmentVariableTarget.Process);

                var username = usernamePassword.Substring(0, seperatorIndex);
                var password = usernamePassword.Substring(seperatorIndex + 1);

                //Check if username and password are correct
                if (username.Equals(OLIUsername) && password.Equals(OLIPassword))
                {
                    return true;
                }
            }
            return false;
        }

    }
}

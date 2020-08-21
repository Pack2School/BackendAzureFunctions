using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Microsoft.AspNetCore.SignalR;
using Pack2SchoolFunction;
using Pack2SchoolFunction.Templates;

namespace Pack2SchoolFunctions.AzureObjects
{
    public class ConnectionFunctions
    {

        private static readonly AzureSignalR SignalR = new AzureSignalR(Environment.GetEnvironmentVariable("AzureSignalRconnectionString"));
        private static readonly string SignalRName = Environment.GetEnvironmentVariable("SignalRName");

        [FunctionName("Negotiate")]
        public static async Task<SignalRConnectionInfo> NegotiateConnection(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestMessage request,
            ILogger log)
        {
            try
            {
                
                ConnectionRequest connectionRequest = await Utilities.ExtractContent<ConnectionRequest>(request);
                log.LogInformation($"Negotiating connection for user: <{connectionRequest.UserId}>.");

                string clientHubUrl = SignalR.GetClientHubUrl(SignalRName);
                string accessToken = SignalR.GenerateAccessToken(clientHubUrl, connectionRequest.UserId);
                return new SignalRConnectionInfo { AccessToken = accessToken, Url = clientHubUrl };
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to negotiate connection.");
                throw;
            }
        }
    }
}

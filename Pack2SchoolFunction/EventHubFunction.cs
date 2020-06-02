using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Pack2SchoolFunction;

namespace Pack2SchoolFunctions
{
    public static class EventHubFunction
    {
        [FunctionName("EventHubFunction")]
        public static async Task Run([EventHubTrigger("pack2schoolhub", Connection = "myConnection")] EventData[] events, [SignalR(HubName = "Pack2SchoolSignalR1")] IAsyncCollector<SignalRMessage> signalRMessages,  ILogger log)
        {
            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                    log.LogInformation("the message:" + messageBody);
                    var baseUrl = "https://pack2schoolfunctions.azurewebsites.net/api/UpdateStudent";
                    string g= await Utilities.sendHttpRequest(baseUrl, HttpMethod.Post, messageBody);
                    await Task.Yield();
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }
    }
}

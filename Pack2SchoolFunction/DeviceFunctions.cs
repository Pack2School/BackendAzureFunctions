using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Pack2SchoolFunction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Pack2SchoolFunctions
{
    public static class DeviceFunction
    {
        [FunctionName("EventHubFunction")]
        public static async Task Run([EventHubTrigger("pack2schoolhub", Connection = "myConnection")] EventData[] events, [SignalR(HubName = "Pack2SchoolSignalR1")] IAsyncCollector<SignalRMessage> signalRMessages, 
           ILogger log)
        {
            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                    var baseUrl = "https://pack2schoolfunctions.azurewebsites.net/api/UpdateStudent";
                    await Utilities.sendHttpRequest(baseUrl, HttpMethod.Post, messageBody);
                    await Task.Yield();
                }
                catch (Exception e)
                { 
                    exceptions.Add(e);
                }
            }

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }
    }
}

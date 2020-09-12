using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pack2SchoolFunctions
{
    public static class DeviceFunction
    {
        public static string stickers = "stickers";
        public static string error = "error";
        public static string deviceIdProperty = "iothub-connection-device-id";

        [FunctionName("ProcessDeviceMessage")]
        public static async Task Run([EventHubTrigger("pack2schoolhub", Connection = "myConnection")] EventData[] events, [SignalR(HubName = "Pack2SchoolSignalR1")] IAsyncCollector<SignalRMessage> signalRMessages,
           ILogger log)
        {
            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                List<string> missingSubjects = null;
                DataBaseAndScanUpdates dataBaseAndScanUpdates;

                eventData.SystemProperties.TryGetValue(deviceIdProperty, out var deviceIdObj);
                string userId = (string)deviceIdObj;

                string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                log.LogInformation($"C# Event Hub trigger function processed a message: {messageBody}");

                JObject obj = JObject.Parse(messageBody);
                var errorMessage = obj[error]?.ToString();

                if (errorMessage != null)
                {
                    dataBaseAndScanUpdates = new DataBaseAndScanUpdates(errorMessage);
                }
                else
                {
                    var subjects = obj[stickers].Select(x => x.ToString()).ToList();
                    var subjectsUpdate = await SubjectsTableUtilities.UpdateBagContent(userId, subjects);
                    var extraSubjects = subjectsUpdate[0];
                    missingSubjects = subjectsUpdate[1];
                    dataBaseAndScanUpdates = new DataBaseAndScanUpdates(userId, missingSubjects: missingSubjects, extraSubjects: extraSubjects);
                }

                await signalRMessages.AddAsync(
                new SignalRMessage
                {
                    UserId = userId,
                    Target = ProjectConsts.SignalRTarget,
                    Arguments = new object[] { dataBaseAndScanUpdates }
                });


                var parentsEntities = UsersTableUtilities.GetParentsEntitiesFromChildId(userId);

                foreach (var parentEntity in parentsEntities)
                {

                    if (parentEntity.UserEmail != null && dataBaseAndScanUpdates.missingSubjects != null && dataBaseAndScanUpdates.missingSubjects.Count > 0)
                    {
                        await EmailSender.sendEmailAsync(userId, parentEntity.UserEmail, missingSubjects);
                    }
                }
            }
        }
    }
}


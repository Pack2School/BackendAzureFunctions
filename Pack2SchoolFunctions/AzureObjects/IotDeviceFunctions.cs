using System;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Pack2SchoolFunction.Tables;

namespace Pack2SchoolFunctions.AzureObjects
{
    public class IotDeviceFunctions
    {

        private static string connectionStringFormat = "HostName=Pack2SchoolIoThub.azure-devices.net;DeviceId={0};SharedAccessKey={1}";

        public async static Task<string> AddDeviceAsync(string deviceId)
        {

            Device device;

            var registryManager = RegistryManager.CreateFromConnectionString(Environment.GetEnvironmentVariable("IotHubConnectionString"));

            try
            {

                var d = new Device(deviceId);

              //  device = await registryManager.AddDeviceAsync(d);
            }
            catch (DeviceAlreadyExistsException)
            {
                Console.WriteLine("Already existing device:");
           //     device = await registryManager.
             //   GetDeviceAsync(deviceId);
            }
            //  return string.Format(connectionStringFormat, deviceId, device.Authentication.SymmetricKey.PrimaryKey);
            return null;
        }

        public static async Task SendCloudToDeviceMessageAsync(string operaion, string userId)
        {
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Environment.GetEnvironmentVariable("IotHubConnectionString"));
            var targetDevice = userId;
            var commandMessage = new
            Message(Encoding.ASCII.GetBytes(operaion));
            await serviceClient.SendAsync(targetDevice, commandMessage, TimeSpan.FromMinutes(1));

        }
      
    }
}

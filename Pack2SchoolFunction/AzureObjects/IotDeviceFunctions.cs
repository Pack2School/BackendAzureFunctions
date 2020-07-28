using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;

namespace Pack2SchoolFunctions.AzureObjects
{
    public class IotDeviceFunctions
    {

        private static string connectionStringFormat = "HostName=Pack2SchoolIoThub.azure-devices.net;DeviceId={0};SharedAccessKey={1}";
        private static RegistryManager registryManager = RegistryManager.CreateFromConnectionString(Environment.GetEnvironmentVariable("IotHubConnectionString"));

        public async static Task<string> AddDeviceAsync(string deviceId)
        {

            Device device;

            try
            {

                var d = new Device(deviceId);

                device = await registryManager.AddDeviceAsync(d);
            }
            catch (DeviceAlreadyExistsException)
            {
                Console.WriteLine("Already existing device:");
                device = await registryManager.
                GetDeviceAsync(deviceId);
            }
            return string.Format(connectionStringFormat, deviceId, device.Authentication.SymmetricKey.PrimaryKey);
        }
    }
}

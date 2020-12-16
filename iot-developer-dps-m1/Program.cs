/*
  This demo application accompanies Pluralsight course 'Microsoft Azure IoT Developer: Configure Solutions for Time Series Insights (TSI)', 
  by Jurgen Kevelaers. See https://app.pluralsight.com/profile/author/jurgen-kevelaers.

  MIT License

  Copyright (c) 2020 Jurgen Kevelaers

  Permission is hereby granted, free of charge, to any person obtaining a copy
  of this software and associated documentation files (the "Software"), to deal
  in the Software without restriction, including without limitation the rights
  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  copies of the Software, and to permit persons to whom the Software is
  furnished to do so, subject to the following conditions:

  The above copyright notice and this permission notice shall be included in all
  copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  SOFTWARE.
*/

using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;

namespace iot_developer_dps_m1
{
  class Program
  {
    // TODO: set your DPS info here:
    private const string dpsGlobalDeviceEndpoint = "TODO.azure-devices-provisioning.net";
    private const string dpsIdScope = "TODO";

    // TODO: set the keys for the symmetric key enrollment group here:
    private const string enrollmentGroupPrimaryKey = "TODO";
    private const string enrollmentGroupSecondaryKey = "TODO";

    private static readonly string[] deviceUsages = new [] { "car", "building", "plane" };
    private static readonly Random random = new Random();

    static async Task Main(string[] args)
    {
      Console.WriteLine("*** Press ENTER to start device registrations ***");
      Console.ReadLine();

      await RegisterDevices(10);

      Console.WriteLine("*** Press ENTER to quit ***");
      Console.ReadLine();
    }

    private static async Task RegisterDevices(int deviceCount)
    {
      // register given number of devices
      for (int i = 1; i <= deviceCount; i++)
      {
        var deviceUsage = deviceUsages[random.Next(0, deviceUsages.Length)];

        var deviceRegistrationId = $"{deviceUsage}-device-{System.Guid.NewGuid().ToString().ToLower()}";

        Console.WriteLine($"Will register device {i}/{deviceCount}: {deviceRegistrationId}...");

        await RegisterDevice(deviceRegistrationId);
      }
    }

    private static async Task RegisterDevice(string deviceRegistrationId)
    {
      // using symmetric keys
      using var securityProvider = new SecurityProviderSymmetricKey(
        registrationId: deviceRegistrationId,
        primaryKey: ComputeKeyHash(enrollmentGroupPrimaryKey, deviceRegistrationId),
        secondaryKey: ComputeKeyHash(enrollmentGroupSecondaryKey, deviceRegistrationId));

      // Amqp transport
      using var transportHandler = new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly);

      // set up provisioning client for given device
      var provisioningDeviceClient = ProvisioningDeviceClient.Create(
        globalDeviceEndpoint: dpsGlobalDeviceEndpoint,
        idScope: dpsIdScope,
        securityProvider: securityProvider,
        transport: transportHandler);

      // register device
      var deviceRegistrationResult = await provisioningDeviceClient.RegisterAsync();

      Console.WriteLine($"   Device registration result: {deviceRegistrationResult.Status}");
      if (!string.IsNullOrEmpty(deviceRegistrationResult.AssignedHub))
      {
        Console.WriteLine($"   Assigned to hub '{deviceRegistrationResult.AssignedHub}'");
      }
      Console.WriteLine();
    }

    private static string ComputeKeyHash(string key, string payload)
    {
      using var hmac = new HMACSHA256(Convert.FromBase64String(key));

      return Convert.ToBase64String(
        hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload)));
    }
  }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Azure.Core;

namespace Azure.ResourceManager.Samples.Common
{
    public static class Utilities
    {
        public static Action<string> LoggerMethod { get; set; }
        public static Func<string> PauseMethod { get; set; }
        public static string ProjectPath { get; set; }
        private static Random _random => new Random();

        static Utilities()
        {
            LoggerMethod = Console.WriteLine;
            PauseMethod = Console.ReadLine;
            ProjectPath = ".";
        }

        public static void Log(string message)
        {
            LoggerMethod.Invoke(message);
        }

        public static void Log(object obj)
        {
            if (obj != null)
            {
                LoggerMethod.Invoke(obj.ToString());
            }
            else
            {
                LoggerMethod.Invoke("(null)");
            }
        }

        public static void Log()
        {
            Utilities.Log("");
        }

        public static string ReadLine() => PauseMethod.Invoke();

        public static string CreateRandomName(string namePrefix) => $"{namePrefix}{_random.Next(9999)}";

        public static string CreatePassword() => "azure12345QWE!";

        public static string CreateUsername() => "tirekicker";

        public static async Task<List<T>> ToEnumerableAsync<T>(this IAsyncEnumerable<T> asyncEnumerable)
        {
            List<T> list = new List<T>();
            await foreach (T item in asyncEnumerable)
            {
                list.Add(item);
            }
            return list;
        }

        public static async Task<VirtualNetworkResource> GetVirtualNetwork(ResourceGroupResource resourceGroup, string vnetName = null)
        {
            vnetName = vnetName is null ? CreateRandomName("vnet") : vnetName;

            VirtualNetworkData vnetInput = new VirtualNetworkData()
            {
                Location = resourceGroup.Data.Location,
                AddressPrefixes = { "10.10.0.0/16" },
                Subnets =
                {
                    new SubnetData() { Name = "default", AddressPrefix = "10.10.1.0/24" },
                    new SubnetData() { Name = "subnet1", AddressPrefix = "10.10.2.0/24" },
                    new SubnetData() { Name = "subnet2", AddressPrefix = "10.10.3.0/24" }
                }
            };
            var vnetLro = await resourceGroup.GetVirtualNetworks().CreateOrUpdateAsync(WaitUntil.Completed, vnetName, vnetInput);
            return vnetLro.Value;
        }

        public static async Task<NetworkInterfaceResource> GetNetworkInterface(ResourceGroupResource resourceGroup, VirtualNetworkResource vnet, ResourceIdentifier publicIPId, string nicName = null)
        {
            nicName = nicName is null ? CreateRandomName("nic") : nicName;

            // Get subnet id
            var subnetLro = await vnet.GetSubnets().GetAsync("default");
            var subnetId = subnetLro.Value.Data.Id;

            // Create a network interface
            var subnetInput = new NetworkInterfaceData()
            {
                Location = resourceGroup.Data.Location,
                IPConfigurations =
                {
                    new NetworkInterfaceIPConfigurationData()
                    {
                        Name = "default-config",
                        PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                        PublicIPAddress = new PublicIPAddressData()
                        {
                            Id = publicIPId
                        },
                        Subnet = new SubnetData()
                        {
                            Id = subnetId
                        }
                    }
                }
            };
            var networkInterfaceLro = await resourceGroup.GetNetworkInterfaces().CreateOrUpdateAsync(WaitUntil.Completed, nicName, subnetInput);
            return networkInterfaceLro.Value;
        }

        public static async Task<VirtualMachineResource> GetVirtualMachine(ResourceGroupResource resourceGroup, ResourceIdentifier nicId, string vmName = null)
        {
            vmName = vmName is null ? CreateRandomName("vm") : vmName;

            // Create a vm
            VirtualMachineCollection vmCollection = resourceGroup.GetVirtualMachines();
            VirtualMachineData vmInput = new VirtualMachineData(resourceGroup.Data.Location)
            {
                HardwareProfile = new VirtualMachineHardwareProfile()
                {
                    VmSize = VirtualMachineSizeType.StandardF2
                },
                OSProfile = new VirtualMachineOSProfile()
                {
                    AdminUsername = CreateUsername(),
                    AdminPassword = CreatePassword(),
                    ComputerName = vmName,
                },
                NetworkProfile = new VirtualMachineNetworkProfile()
                {
                    NetworkInterfaces =
                        {
                            new VirtualMachineNetworkInterfaceReference()
                            {
                                Id = nicId,
                                Primary = true,
                            }
                        }
                },
                StorageProfile = new VirtualMachineStorageProfile()
                {
                    OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage)
                    {
                        OSType = SupportedOperatingSystemType.Linux,
                        Caching = CachingType.ReadWrite,
                        ManagedDisk = new VirtualMachineManagedDisk()
                        {
                            StorageAccountType = StorageAccountType.StandardLrs
                        }
                    },
                    ImageReference = new ImageReference()
                    {
                        Publisher = "Canonical",
                        Offer = "UbuntuServer",
                        Sku = "16.04-LTS",
                        Version = "latest",
                    }
                }
            };
            var vmLro = await vmCollection.CreateOrUpdateAsync(WaitUntil.Completed, vmName, vmInput);
            return vmLro.Value;
        }
    }
}
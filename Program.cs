// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.Samples.Common;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using System.Reflection;
using System.Threading.Tasks;
using System;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Compute;
using System.Net.NetworkInformation;

namespace ManageIPAddress
{
    public class Program
    {
        private static ResourceIdentifier? _resourceGroupId = null;

        /**
         * Azure Network sample for managing IP address -
         *  - Assign a public IP address for a virtual machine during its creation
         *  - Assign a public IP address for a virtual machine through an virtual machine update action
         *  - Get the associated public IP address for a virtual machine
         *  - Get the assigned public IP address for a virtual machine
         *  - Remove a public IP address from a virtual machine.
         */
        public static async Task RunSample(ArmClient client)
        {
            string publicIPAddressName1 = Utilities.CreateRandomName("pip1-");
            string publicIPAddressName2 = Utilities.CreateRandomName("pip2-");
            string publicIPAddressLeafDNS1 = Utilities.CreateRandomName("dns-pip1-");
            string publicIPAddressLeafDNS2 = Utilities.CreateRandomName("dns-pip2-");
            string nicName1 = Utilities.CreateRandomName("nic1-");
            string nicName2 = Utilities.CreateRandomName("nic2-");

            try
            {
                // Get default subscription
                SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();

                // Create a resource group in the EastUS region
                string rgName = Utilities.CreateRandomName("NetworkSampleRG");
                Utilities.Log($"Creating resource group...");
                ArmOperation<ResourceGroupResource> rgLro = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, rgName, new ResourceGroupData(AzureLocation.EastUS));
                ResourceGroupResource resourceGroup = rgLro.Value;
                _resourceGroupId = resourceGroup.Id;
                Utilities.Log("Created a resource group with name: " + resourceGroup.Data.Name);

                //============================================================
                // Assign a public IP address for a VM during its creation

                // Define a public IP address to be used during VM creation time

                Utilities.Log("Creating a public IP address...");
                PublicIPAddressData publicIPInput1 = new PublicIPAddressData()
                {
                    Location = resourceGroup.Data.Location,
                    PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                    DnsSettings = new PublicIPAddressDnsSettings()
                    {
                        DomainNameLabel = publicIPAddressLeafDNS1
                    }
                };
                var publicIPLro1 = await resourceGroup.GetPublicIPAddresses().CreateOrUpdateAsync(WaitUntil.Completed, publicIPAddressName1, publicIPInput1);
                PublicIPAddressResource publicIP1 = publicIPLro1.Value;
                Utilities.Log($"Created a public IP address: {publicIP1.Data.Name}");

                // Use the pre-created public IP for the new VM

                Utilities.Log("Creating a Windows VM...");
                VirtualNetworkResource vnet = await Utilities.GetVirtualNetwork(resourceGroup);
                NetworkInterfaceResource nic = await Utilities.GetNetworkInterface(resourceGroup, vnet, publicIP1.Data.Id, nicName1);
                VirtualMachineResource vm = await Utilities.GetVirtualMachine(resourceGroup, nic.Data.Id);
                Utilities.Log($"Created VM: {vm.Data.Name}");

                //============================================================
                // Gets the public IP address associated with the VM's primary NIC

                string attachedNicName = vm.Data.NetworkProfile.NetworkInterfaces.First().Id.Name;
                string associatedPIP = nic.Data.IPConfigurations.First().PublicIPAddress.Id.Name;
                Utilities.Log($"Current vm attached NIC: {attachedNicName}");
                Utilities.Log($"{attachedNicName} associated public ip: {associatedPIP}");

                //============================================================
                // Assign a new public IP address for the VM

                // Define a new public IP address
                Utilities.Log($"Creating another public IP address...");
                PublicIPAddressData publicIPInput2 = new PublicIPAddressData()
                {
                    Location = resourceGroup.Data.Location,
                    PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                    DnsSettings = new PublicIPAddressDnsSettings()
                    {
                        DomainNameLabel = publicIPAddressLeafDNS2
                    }
                };
                var publicIPLro2 = await resourceGroup.GetPublicIPAddresses().CreateOrUpdateAsync(WaitUntil.Completed, publicIPAddressName2, publicIPInput2);
                PublicIPAddressResource publicIP2 = publicIPLro2.Value;
                Utilities.Log($"Created a public IP address: {publicIP2.Data.Name}");

                // Created a new NIC with PublicIP2
                Utilities.Log($"Creating a new NIC...");
                var nic2 = await Utilities.GetNetworkInterface(resourceGroup, vnet, publicIP2.Data.Id, nicName2);
                Utilities.Log($"Created a new NIC: {nic2.Data.Name}");

                // Update VM's primary NIC to use the new public IP address
                Utilities.Log("Updating the VM's primary NIC with new public IP address...");
                Utilities.Log("Stop vm for update its attached NIC...");
                await vm.DeallocateAsync(WaitUntil.Completed);

                Utilities.Log("Updating vm to attach a new NIC...");
                VirtualMachinePatch updateVmInput = new VirtualMachinePatch()
                {
                    NetworkProfile = new VirtualMachineNetworkProfile()
                    {
                        NetworkInterfaces =
                        {
                            new VirtualMachineNetworkInterfaceReference()
                            {
                                Id = nic2.Data.Id,
                                Primary = true,
                            }
                        }
                    },
                };
                var updateVmLro = await vm.UpdateAsync(WaitUntil.Completed, updateVmInput);
                vm = updateVmLro.Value;


                //============================================================
                // Gets the updated public IP address associated with the VM

                // Get the associated public IP address for a virtual machine
                attachedNicName = vm.Data.NetworkProfile.NetworkInterfaces.First().Id.Name;
                associatedPIP = nic2.Data.IPConfigurations.First().PublicIPAddress.Id.Name;
                nic = await resourceGroup.GetNetworkInterfaces().GetAsync(nicName1);
                Utilities.Log($"After update, vm attached NIC: {attachedNicName}");
                Utilities.Log($"After update, {attachedNicName} associated public ip: {associatedPIP}");

                //============================================================
                // Remove public IP associated with the VM

                Utilities.Log("Removing public IP address associated with the VM...");
                var subnetLro = await vnet.GetSubnets().GetAsync("default");
                var subnetId = subnetLro.Value.Data.Id;
                var subnetInput = new NetworkInterfaceData()
                {
                    Location = resourceGroup.Data.Location,
                    IPConfigurations =
                    {
                        new NetworkInterfaceIPConfigurationData()
                        {
                            Name = "default-config",
                            PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                            Subnet = new SubnetData()
                            {
                                Id = subnetId
                            }
                        }
                    }
                };
                var updateNicLro = await resourceGroup.GetNetworkInterfaces().CreateOrUpdateAsync(WaitUntil.Completed, nicName2, subnetInput);
                nic2 = updateNicLro.Value;
                Utilities.Log("Removed public IP address associated with the VM");
                Utilities.Log($"nic2 associated public ip is null: {nic2.Data.IPConfigurations.First().PublicIPAddress is null}");

                //============================================================
                // Delete the public ip
                Utilities.Log("Deleting the public IP address...");
                await publicIP2.DeleteAsync(WaitUntil.Completed);
                Utilities.Log("Deleted the public IP address");
            }
            finally
            {
                try
                {
                    if (_resourceGroupId is not null)
                    {
                        Utilities.Log($"Deleting Resource Group...");
                        await client.GetResourceGroupResource(_resourceGroupId).DeleteAsync(WaitUntil.Completed);
                        Utilities.Log($"Deleted Resource Group: {_resourceGroupId.Name}");
                    }
                }
                catch (NullReferenceException)
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
                }
                catch (Exception ex)
                {
                    Utilities.Log(ex);
                }
            }
        }

        public static async Task Main(string[] args)
        {
            try
            {
                //=================================================================
                // Authenticate
                var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
                var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                var subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
                ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                ArmClient client = new ArmClient(credential, subscription);

                await RunSample(client);
            }
            catch (Exception ex)
            {
                Utilities.Log(ex);
            }
        }
    }
}
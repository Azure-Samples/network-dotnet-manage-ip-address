---
page_type: sample
languages:
- csharp
products:
- azure
extensions:
  services: virtual-network
  platforms: dotnet
---

# Getting started on managing network user defined routes in C# #

 Azure Network sample for managing IP address -
  - Assign a public IP address for a virtual machine during its creation
  - Assign a public IP address for a virtual machine through an virtual machine update action
  - Get the associated public IP address for a virtual machine
  - Get the assigned public IP address for a virtual machine
  - Remove a public IP address from a virtual machine.


## Running this Sample ##

To run this sample:

Set the environment variable `CLIENT_ID`,`CLIENT_SECRET`,`TENANT_ID`,`SUBSCRIPTION_ID`,`Current_Machine_PublicIP` with the full path for an auth file. See [how to create an auth file](https://github.com/Azure/azure-libraries-for-net/blob/master/AUTH.md).

    git clone https://github.com/Azure-Samples/network-dotnet-manage-ip-address.git

    cd network-dotnet-manage-ip-address

    dotnet build

    bin\Debug\net452\ManageIPAddress.exe

## More information ##

[Azure Management Libraries for C#](https://github.com/Azure/azure-sdk-for-net)
[Azure .Net Developer Center](https://azure.microsoft.com/en-us/develop/net/)
If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212)

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
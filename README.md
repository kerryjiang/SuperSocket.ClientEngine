# SuperSocket.ClientEngine [![NuGet Version](https://img.shields.io/nuget/v/SuperSocket.ClientEngine.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.ClientEngine/)

SuperSocket.ClientEngine is a .NET library for socket client rapid development. It provides easy to use and efficient APIs to simplify your socket development work about asynchronous connecting, data sending, data receiving, network protocol analysising and transfer layer encryption.


### Install from NuGet

	PM> Install-Package SuperSocket.ClientEngine
	

### Basic usage

	var client = new EasyClient();
	
	// Initialize the client with the receive filter and request handler
	client.Initialize(new TerminatorReceiveFilter<StringPackageInfo>(), (request) => {
		Console.WriteLine(request.Key);
	});
	
	// send data to the server
	client.Send("ECHO kerry");

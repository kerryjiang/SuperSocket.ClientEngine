# SuperSocket.ClientEngine [![Build Status](https://travis-ci.org/kerryjiang/SuperSocket.ClientEngine.svg?branch=master)](https://travis-ci.org/kerryjiang/SuperSocket.ClientEngine) [![NuGet Version](https://img.shields.io/nuget/v/SuperSocket.ClientEngine.svg?style=flat)](https://www.nuget.org/packages/SuperSocket.ClientEngine/)

SuperSocket.ClientEngine is a .NET library for socket client rapid development. It provides easy to use and efficient APIs to simplify your socket development work about asynchronous connecting, data sending, data receiving, network protocol analysising and transfer layer encryption.

### Build from Source Code

	1. Download source code from Github or clone the repository;
	2. Restore dependencies using NuGet; (Latest NuGet Visual Studio extension will restore dependencies before build automatically. You also can follow [This NuGet doc](https://docs.nuget.org/consume/package-restore#package-restore-approaches) to do it manually.)
	3. Choose a correct solution file to build;


### Install from NuGet

	PM> Install-Package SuperSocket.ClientEngine
	

### Usage


#### Create your ReceiveFilter Implementation according Your Network Protocol

SuperSocket.ClientEngine provides some powerfull basic ReceiveFilter classes (under the namespace "SuperSocket.ProtoBase") to help you simplify your protocol analysis:

	TerminatorReceiveFilter
	BeginEndMarkReceiveFilter
	FixedHeaderReceiveFilter
	FixedSizeReceiveFilter
	CountSpliterReceiveFilter
	
You should design your own ReceiveFilter according yoru protocol details base on the basic ReceiveFilters provided by SuperSocket.ClientEngine:

	class MyReceiveFilter : TerminatorReceiveFilter<StringPackageInfo>
	{
		public MyReceiveFilter()
		: base(Encoding.ASCII.GetBytes("||")) // two vertical bars as package terminator
		{
		}
		
		// other code you need implement according yoru protocol details
	}
	

#### Create an Instance of EasyClient and Initialize it with the ReceiveFilter which is created in the previous step

	var client = new EasyClient();
	
	// Initialize the client with the receive filter and request handler
	client.Initialize(new MyReceiveFilter(), (request) => {
		// handle the received request
		Console.WriteLine(request.Key);
	});
	
	
#### Make a Connection and then Start the Communication
	
	
	// Connect to the server
	var connected = await client.ConnectAsync(new IPEndPoint(IPAddress.Parse("192.168.10.11"), 25));
	
	if (connected)
	{
		// Send data to the server
		client.Send(Encoding.ASCII.GetBytes("LOGIN kerry"));
	}
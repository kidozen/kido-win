#KidoZen SDK for Windows Runtime 4.5 and Windows Phone 8.0

The KidoZen SDK for Windows provides libraries and code samples for developers to build connected applications using KidoZen. This guide walks through the steps for setting up the SDK and running the tests.

##Get Set Up

To get Your Credentials Register in [kidozen.com](http://kidozen.com).

##Get the Windows SDK

Minimum requirements for using the KidoZen SDK for Windows are:

- Visual Studio 2012
- Windows Phone SDK 8.0
- Download [the SDK](http://www.kidozen.com/winsdk/kido-win.zip) or get the sources from [GitHub](https://github.com/kidozen/kido-win). 

The folder structure of the SDK is the following:

- `bin` Assemblies and their dependencies for each platform (runtime 4.5 and phone 8.0)
- `source` Visual Studio 2012 solution containing source code and tests.
- `source/KidoZen.Client.winrt45` folder. Contains the project for Windows Runtime 4.5 and its private clases.
- `source/KidoZen.Client.wp80` folder. Contains the project for Windows Phone 8.0 and its private clases.
- `source/src` folder. Contains common clases for the all platforms (windows Runrime 4.5 and window phone 8).
- `source/test.winrt45` folder. Contains the project of Unit Tests for Windows Runtime 4.5 and its private clases.
- `source/test.wp80` folder. Contains the project of Unit Tests for Windows Phone 8.0 and its private clases.
- `source/tests` folder. Contains common unit tests classes for the all platforms.
- `source/packages` folder. Contains the dependencies required by the SDK. 

##About the Unit Tests

The Unit Tests projects require your Marketplace's URL, Application's name, User, Password, etc directly into the code. This approach will get you running quickly, but we do not recommend it in a production application: a malicious user could use decompiling techniques to steal your KidoZen security credentials.

All unit tests demostrate how to access KidoZen from an application using the following services:

- Configuration
- EMail
- File
- Initialization
- Logging
- Marketplace
- Pubsub
- Queue
- SMS
- Storage

##To prepare the Unit Tests

- In the Visual Studio solution, chose a test project and open the file Scaffolding.cs and provide the values for all defined constant.
	- MarketplaceUrl: URL of the tenant's marketplece.
	- AppName: Name of the application that the tests will use
	- Provider: Name of the User Source to be used
	- User: User name that belongs to the User Source
	- Password: User's password
	- Emai: address that will be used in tests

- Build the project

- Run the the tests

##How to Include the KidoZen SDK for Windows within an existing Application

To use KidoZen with an existing application, you have to add all the SDK's assemblies as references for the application project. You can find the assemblies at the ´bin´ folder.

##Getting started with the code

One instance of the KZApplication object has the instances of each of the services that you can find in the Kidozen platform (Storage, Queue, etc.) 

SDK API is await/async based on all its interfaces, so it will never block the UI.

Initialize the Application: During initialization the SDK pulls the application configuration from KidoZen servers.

		var app = new KZApplication("https://marketplace's URL", "application name");
		await app.Initialize();

Authenticate: you must provide the user name, its password and the user source. The SDK hides all the calls needed to authenticate the user against the selected user source and to create a security tokens to execute all the services call. The SDK's has an internal cache to store these tokens. Each time the token is near get expired, teh SDk will renew it on the background.

		var user = await app.Authenticate("userName", "userPassword", "userSource");

Once the user is authenticated you can start using all the services:

		var tasks = app.Storage["tasks"];
		var queryResult = await tasks.Query<Task>("{}");
		...

		var queue = app.Queue["pendingDocuments"];
		var document = await queue.Dequeue<Document>();
		...

#License 

Copyright (c) 2013 KidoZen, inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

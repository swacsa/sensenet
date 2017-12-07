---
title: "Preparation of sensenet ECM's network load balance environment"
source_url: 'https://github.com/SenseNet/sensenet/docs/sensenet'
category: sensenet
version: v7.0.0
tags: [preparation, environment, sn7, nlb]
---

# Preparation of sensenet ECM's network load balance environment

This paper describes how to prepare an environment for the sensenet ECM to be network load balanced.
_(Load balancing in Azure has not been described here.)_
#### Prerequisits:  
 - [sensenet Service 7.0.0 NuGet package](https://www.nuget.org/packages/SenseNet.Services/7.0.0-beta2 "SenseNet.Services")  
After you installed this package into your web application, follow the instructions you got in the read.me file and you will be able to use sensenet ECM core features.
 
### Setting up the first site
 _(Let us assume that the folder for the website is \[site folder 1\])_
You need to edit config files:  
- Insert the following into web.config:  
```xml
	<connectionStrings>
		<add name="SnCrMsSql" connectionString="Data Source=[sql server name];Initial Catalog=[sql db name];Integrated Security=True" providerName="System.Data.SqlClient" />
		   ...
	</connectionStrings>
	...
	<sensenet>
		<packaging>
			<add key="NetworkTargets" value="\\[web server 2]\Web\[site 2 name];...;\\[web server N]\Web\[site N name]" />
			...
		</packaging>
		<messaging>
			<add key="MsmqChannelQueueName" value=".\private$\web;FormatName:DIRECT=TCP:[web server 1 ip]\private$\web" />
		</messaging>
```  
_**SnCrMsSql**_ : the connection string tells the site how the application reaches the MS-SQL database.  
_**NetworkTargets**_: it is a sequence of addresses, where sensenet ECM can connect to the other sites.  
_**MsmqChannelQueueName**_: the MSMQ channel settings for the TCP based communication among sites.

- Insert the following into SnAdminRuntime.exe.config:
```xml
	<connectionStrings>
		<add name="SnCrMsSql" connectionString="Data Source=[sql server name];Initial Catalog=[sql db name];Integrated Security=True" providerName="System.Data.SqlClient" />
		   ...
	</connectionStrings>
	...
	<sensenet>
		<packaging>
			<add key="NetworkTargets" value="\\[web server 2]\Web\[site 2 name];...;\\[web server N]\Web\[site N name]" />
			...
		</packaging>
		...
```

## Setting up further websites
As you want to create more than one website to work together in NLB, you need to set up the others as well. In the following steps the current server related values were labeled with an N to tell from the first one:
 - Create a folder for the website (from now on it is \[site folder N\])
 - Copy the site file structure from the first site's folder into the newly made \[site folder N\]  
 - Edit config files:  
Editing web.config:  
Change the following values:  
	- from \[web server 1\] to \[web server N\]
	- from \[web server 1 ip\] to \[web server N ip\]  
	- from \[site 1 name\] to \[site N name\]  
  In the sensenet\packaging section you should make sure that every other than the current website exists in NetworkTargets.



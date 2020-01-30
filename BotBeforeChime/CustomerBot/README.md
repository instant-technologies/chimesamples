# CustomerBot

This bot has been created using [Bot Framework](https://dev.botframework.com), it shows the minimum code required to build a bot.

## Summary

If you are developing a Microsoft botframework bot, and you would like to proxy a conversation over to chime if the bot cannot solve the end user request, this is an example of how you can connect the bot to chime via the DirectLine api.

## High Level Overview
```bash
Start a conversation with a bot
Request to be sent to an agent
Proxy the message over directline to a chime instance
Manage DirectLine conversation sessions
Translate incoming adaptive card types from DirectLine adaptive cards to BotFramework adaptive cards
Send a json data bag of user information into chime via an api route
Shut down conversation
```
## Usage
Open CustomerBot.sln to launch into the visual studio project. Check out the readme.md inside the solution folder for important usage information. 
## Important Files

```bash
CustomerBot.cs
DirectLineSession.cs
SeekerData.cs
```

## Prerequisites

- [.NET Core SDK](https://dotnet.microsoft.com/download) version 2.1

  ```bash
  # determine dotnet version
  dotnet --version
  ```

## To try this sample

- In a terminal, navigate to `CustomerBot`

    ```bash
    # change into project folder
    cd CustomerBot
    ```

- Run the bot from a terminal or from Visual Studio, choose option A or B.

  A) From a terminal

  ```bash
  # run the bot
  dotnet run
  ```

  B) Or from Visual Studio

  - Launch Visual Studio
  - File -> Open -> Project/Solution
  - Navigate to `CustomerBot` folder
  - Select `CustomerBot.csproj` file
  - Press `F5` to run the project

## Testing the bot using Bot Framework Emulator

[Bot Framework Emulator](https://github.com/microsoft/botframework-emulator) is a desktop application that allows bot developers to test and debug their bots on localhost or running remotely through a tunnel.

- Install the Bot Framework Emulator version 4.3.0 or greater from [here](https://github.com/Microsoft/BotFramework-Emulator/releases)

### Connect to the bot using Bot Framework Emulator

- Launch Bot Framework Emulator
- File -> Open Bot
- Enter a Bot URL of `http://localhost:3978/api/messages`

## Deploy the bot to Azure

To learn more about deploying a bot to Azure, see [Deploy your bot to Azure](https://aka.ms/azuredeployment) for a complete list of deployment instructions.

## Technical Discussion

To run this project against a Chime instance, you will need the following information on a Chime bot framework endpoint (dispatcher) found in customerbot.cs
```bash
bot.BotId = "MSAppId";
bot.BotName = "Directline Bot Name";
bot.BotSecret = "Directline Secret";
```

Chime sends down conversations using Adaptive Cards – and bot framework handles these cards differently.  In the example, there is code to transform these cards.

When the sample bot acts as the proxy, you can make an authenticated\non authenticated post via HTTP using this endpoint to pass along some custom values that Chime might care about and have those values associated with a chat session.  This information is wrapped using the seekerdata object.  This information can be passed at any point in the conversation. 

You can see if these values are being passed back into the system by checking on a sessions details. 

![Capture](https://user-images.githubusercontent.com/60370937/73483658-ce4efa00-436d-11ea-9213-ff7c7ba858ce.PNG)

A developer may want to query a queue for status on agents, waiting seekers, or other information.  There is an authenticated end point available via this sample end point… where Chime can return this information (this sample does not include this request). 

This is an example of a json response to this route. 

![queue info](https://user-images.githubusercontent.com/60370937/73482866-811e5880-436c-11ea-8cc6-66462ee4b02a.png)

## Further reading

We do currently have an api route that can retreive queue level information and we are currently working on a way to authenticate outside bots to this route. 

For information on how to implement a node.js implementation of this bot check out https://github.com/microsoft/BotFramework-DirectLineJS


- [Bot Framework Documentation](https://docs.botframework.com)
- [Bot Basics](https://docs.microsoft.com/azure/bot-service/bot-builder-basics?view=azure-bot-service-4.0)
- [Activity processing](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-concept-activity-processing?view=azure-bot-service-4.0)
- [Azure Bot Service Introduction](https://docs.microsoft.com/azure/bot-service/bot-service-overview-introduction?view=azure-bot-service-4.0)
- [Azure Bot Service Documentation](https://docs.microsoft.com/azure/bot-service/?view=azure-bot-service-4.0)
- [.NET Core CLI tools](https://docs.microsoft.com/en-us/dotnet/core/tools/?tabs=netcore2x)
- [Azure CLI](https://docs.microsoft.com/cli/azure/?view=azure-cli-latest)
- [Azure Portal](https://portal.azure.com)
- [Language Understanding using LUIS](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/)
- [Channels and Bot Connector Service](https://docs.microsoft.com/en-us/azure/bot-service/bot-concepts?view=azure-bot-service-4.0)

# Bot_Before_Chime
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
Open Bot_Before_Chime.sln to launch into the visual studio project. Check out the readme.md inside the solution folder for important usage information. 
## Important Files

```bash
SampleBot.cs
DirectLineSession.cs
```

## Prerequisites

- [.NET Core SDK](https://dotnet.microsoft.com/download) version 3.1

  ```bash
  # determine dotnet version
  dotnet --version
  ```

## To try this sample

- In a terminal, navigate to `Bot_Before_Chime`

    ```bash
    # change into project folder
    cd # Bot_Before_Chime
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
  - Navigate to `Bot_Before_Chime` folder
  - Select `Bot_Before_Chime.csproj` file
  - Press `F5` to run the project

## Testing the bot using Bot Framework Emulator

[Bot Framework Emulator](https://github.com/microsoft/botframework-emulator) is a desktop application that allows bot developers to test and debug their bots on localhost or running remotely through a tunnel.

- Install the Bot Framework Emulator version 4.5.0 or greater from [here](https://github.com/Microsoft/BotFramework-Emulator/releases)

### Connect to the bot using Bot Framework Emulator

- Launch Bot Framework Emulator
- File -> Open Bot
- Enter a Bot URL of `http://localhost:3978/api/messages`

## Deploy the bot to Azure

To learn more about deploying a bot to Azure, see [Deploy your bot to Azure](https://aka.ms/azuredeployment) for a complete list of deployment instructions.

## Technical Discussion

To run this project against a Chime instance, you will need the following information on a Chime bot framework endpoint (dispatcher) found in SampleBot.cs
```bash
bot.BotId = "MSAppId";
bot.BotName = "Directline Bot Name";
bot.BotSecret = "Directline Secret";
```

Chime sends down conversations using Adaptive Cards – and bot framework handles these cards differently.  In the example, there is code to transform these cards.


## Further reading

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

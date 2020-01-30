## BotBeforeChime

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
## Technical Discussion

To run this project against a Chime instance, you will need the following information on a Chime bot framework endpoint (dispatcher) found in customerbot.cs
```bash
bot.BotId = "MSAppId";
bot.BotName = "Directline Bot Name";
bot.BotSecret = "Directline Secret";
```

Chime sends down conversations using Adaptive Cards – and bot framework handles these cards differently.  In the example, there is code to transform these cards.

When the sample bot acts as the proxy, you can make an authenticated\non authenticated post via HTTP using this endpoint to pass along some custom values that Chime might care about and have those values associated with a chat session.  This information is wrapped using the seekerdata object.  This information can be passed at any point in the conversation.  

A developer may want to query a queue for status on agents, waiting seekers, or other information.  There is an authenticated end point available via this sample end point… where Chime can return this information (this sample does not include this request).  
This is the endpoint
This is the return value

## Usage
Open CustomerBot.sln to launch into the visual studio project. Check out the readme.md inside the solution folder for important usage information. 
## Important Files

```bash
CustomerBot.cs
DirectLineSession.cs
SeekerData.cs
```

## Additional notes
We do currently have an api route that can retreive queue level information and we are currently working on a way to authenticate outside bots to this route. 

For information on how to implement a node.js implementation of this bot check out https://github.com/microsoft/BotFramework-DirectLineJS


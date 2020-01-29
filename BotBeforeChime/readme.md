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
## Important Files

```bash
CustomerBot.cs
DirectLineSession.cs
SeekerData.cs
```

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
/// <summary>
/// This bot is very basic and mostly built directly off an example from microsoft. It asks a few questions and prepares an initial question to post to Chime directline bots and integrate into the Chime system. 
/// </summary>
namespace CustomerBot
{
    public class EmptyBot : ActivityHandler
    {
        private BotState _conversationState;
        private BotState _userState;
        private static DirectLineSession _botSession;
        private static bool istransfered = false;
        public EmptyBot(ConversationState conversationState, UserState userState)
        {
            _conversationState = conversationState;
            _userState = userState;
        }
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Hi!"), cancellationToken);
                }
            }
        }
        private static async Task FillOutUserProfileAsync(ConversationFlow flow, UserProfile profile, ITurnContext turnContext)
        {
            string input = turnContext.Activity.Text?.Trim();
            string message;
            
            switch (flow.LastQuestionAsked)
            {
                case ConversationFlow.Question.None:
                    await turnContext.SendActivityAsync("Let's get started. What is your name?");
                    flow.LastQuestionAsked = ConversationFlow.Question.Name;
                    break;
                case ConversationFlow.Question.Name:
                    if (ValidateName(input, out string name, out message))
                    {
                        profile.Name = name;
                        await turnContext.SendActivityAsync($"Hi {profile.Name}.");
                        await turnContext.SendActivityAsync("What can I help you with?");
                        flow.LastQuestionAsked = ConversationFlow.Question.Question;
                        break;
                    }
                    else
                    {
                        await turnContext.SendActivityAsync(message ?? "I'm sorry, I didn't understand that.");
                        break;
                    }
                case ConversationFlow.Question.Question:
                    if (ValidateQuestion(input, out string question, out message))
                    {
                        profile.Question = question;
                        await turnContext.SendActivityAsync($"Okay {profile.Name} let me see if I can help with your question: {profile.Question}.");
                        await turnContext.SendActivityAsync($"{profile.Name} I can't really help you with that...");
                        var reply = MessageFactory.Text("Would you like to be transfered to an Agent?");

                        reply.SuggestedActions = new SuggestedActions()
                        {
                            Actions = new List<CardAction>()
                        {
                            new CardAction() { Title = "Yes", Type = ActionTypes.ImBack, Value = "You are being transfered." },
                        },
                        };
                        await turnContext.SendActivityAsync(reply);
                        flow.LastQuestionAsked = ConversationFlow.Question.Agent;
                        break;
                    }
                    else
                    {
                        await turnContext.SendActivityAsync(message ?? "I'm sorry, I didn't understand that.");
                        break;
                    }
                case ConversationFlow.Question.Agent:
                    //Here is where we will connect through something to an agent in chime....
                    if (istransfered == false)
                    {
                        //Here I forward the turn context into a session handler. The turn context will be used to forward replies from the agent back to the end user. 
                        SessionHandler session = new SessionHandler(profile, turnContext);
                        var bot = new BotInfo();
                        //You must get this information from azure portal. you will need the Microsoft App ID, application name and directline secret.
                        bot.BotId = "MSAppId";
                        bot.BotName = "Directline Bot Name";
                        bot.BotSecret = "Directline Secret";
                        _botSession = new DirectLineSession(session, bot);
                        await _botSession.Start();
                        await _botSession.SendMessage(profile.Question, null);
                        istransfered = true;
                    }
                    else
                    {
                        await _botSession.SendMessage(input, null);
                    }
                    
                    break;
            }
        }
        
        private static bool ValidateName(string input, out string name, out string message)
        {
            name = null;
            message = null;

            if (string.IsNullOrWhiteSpace(input))
            {
                message = "Please enter a name that contains at least one character.";
            }
            else
            {
                name = input.Trim();
            }

            return message is null;
        }
        private static bool ValidateQuestion(string input, out string question, out string message)
        {
            question = null;
            message = null;

            if (string.IsNullOrWhiteSpace(input))
            {
                message = "Please enter a name that contains at least one character.";
            }
            else
            {
                question = input.Trim();
            }

            return message is null;
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {

            var conversationStateAccessors = _conversationState.CreateProperty<ConversationFlow>(nameof(ConversationFlow));
            var flow = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationFlow());

            var userStateAccessors = _userState.CreateProperty<UserProfile>(nameof(UserProfile));
            var profile = await userStateAccessors.GetAsync(turnContext, () => new UserProfile());

            await FillOutUserProfileAsync(flow, profile, turnContext);

            // Save changes.
            await _conversationState.SaveChangesAsync(turnContext);
            await _userState.SaveChangesAsync(turnContext);
        }
    }
}

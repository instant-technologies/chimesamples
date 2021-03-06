﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
/// <summary>
/// This bot is intended to ask a few questions then proxy a conversation over to a Chime for Teams queue via DirectLineSession.cs which uses the directline sdk.  
/// </summary>
namespace Bot_Before_Chime.Bots
{
    public class SampleBot : ActivityHandler
    {
        private BotState _conversationState;
        private BotState _userState;
        private static DirectLineSession _botSession;
        private static bool istransfered = false;
        public SampleBot(ConversationState conversationState, UserState userState)
        {
            _conversationState = conversationState;
            _userState = userState;
        }
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            //This override will handle a new member being added to the chat. 
            foreach (var member in membersAdded)
            {
                //checks to see if a new member has been added to the list of channel accounts
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    //this will send an initial message to each new member added. In this case the message is 'Hi'
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Hi!"), cancellationToken);
                }
            }
        }
        /// <summary>
        /// This method will be called to get some initial information from the user prior to being proxied over to chime via directline 
        /// </summary>
        /// <param name="flow"></param>
        /// <param name="profile"></param>
        /// <param name="turnContext"></param>
        /// <returns></returns>
        private static async Task FillOutUserProfileAsync(ConversationFlow flow, UserProfile profile, ITurnContext turnContext)
        {
            
            string input = turnContext.Activity.Text?.Trim();
            string message;
            //This switch case will move based on an order of a conversation flow. Here I simply ask "What is your name?" and "What is your question?" 
            //I will save those values to a sample profile class to be sent over directline. 
            //Chime will interpret these values based off the ChannelAccount Class. 
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
                    //At this point we will utilize the directlinesession.cs class to proxy a conversation between a botframework bot to chime via directline sdk.
                    //We will send over the turnContext to the sessionhandler so the directline proxy knows where/when to send messages coming back from a chime agent. 
                    if (istransfered == false)
                    {
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
        /// <summary>
        /// This override will handle new message activity. 
        /// </summary>
        /// <param name="turnContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

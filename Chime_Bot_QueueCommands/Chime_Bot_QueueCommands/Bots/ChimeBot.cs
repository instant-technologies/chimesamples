// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace Chime_Bot_QueueCommands
{
    public class Chime_Bot_QueueCommands : ActivityHandler
    {
        private BotState _conversationState;
        public Chime_Bot_QueueCommands(ConversationState conversationState)
        {
            _conversationState = conversationState;
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var conversationStateAccessors = _conversationState.CreateProperty<ConversationFlow>(nameof(ConversationFlow));
            var flow = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationFlow());

            await DisplayOptions(flow, turnContext);

            await _conversationState.SaveChangesAsync(turnContext);
        }
        private static async Task DisplayOptions(ConversationFlow flow, ITurnContext turnContext)
        {
            string input = turnContext.Activity.Text?.Trim();
            switch (flow.LastQuestionAsked)
            {
                case ConversationFlow.Question.None:
                    await turnContext.SendActivityAsync("Here I will show you how to use buttons to call a few actions within Chime.");
                    // /agent: To Route to an agent in chime you can use the "/agent" command at any time.
                    // /agent /{Tag}: To Route to an agent and apply a skilltag {tag}, e.g. /agent /outlookPW
                    // /exit or /end: End the chat and mark it as Deflected
                    // /transfer {queueId} - transfer the chat to the queue with the ID {queueId} e.g. /transfer 2
                    var reply = MessageFactory.Text("What action would you like to perform?");

                    reply.SuggestedActions = new SuggestedActions()
                    {
                        Actions = new List<CardAction>()
                        {

                            new CardAction() { Title = "Route to Agent", Type = ActionTypes.ImBack, Value = "/agent" },
                            new CardAction() { Title = "Route to Agent / Add a skill tag", Type = ActionTypes.ImBack, Value = "/agent /outlookPW" },
                            new CardAction() { Title = "End Conversation", Type = ActionTypes.ImBack, Value = "/end" },
                            new CardAction() { Title = "Transfer to VIP Queue", Type = ActionTypes.ImBack, Value = "/transfer 2" },
                        },
                    };
                    await turnContext.SendActivityAsync(reply);
                    flow.LastQuestionAsked = ConversationFlow.Question.FirstMessage;
                    break;
                case ConversationFlow.Question.FirstMessage:
                    
                    break;
            }
        }
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Welcome."), cancellationToken);
                }
            }
        }
    }
}

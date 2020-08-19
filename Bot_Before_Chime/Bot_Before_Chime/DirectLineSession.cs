using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Activity = Microsoft.Bot.Connector.DirectLine.Activity;
using Attachment = Microsoft.Bot.Schema.Attachment;
using CardAction = Microsoft.Bot.Schema.CardAction;
using ChannelAccount = Microsoft.Bot.Schema.ChannelAccount;
using Conversation = Microsoft.Bot.Connector.DirectLine.Conversation;
using Timer = System.Timers.Timer;

namespace Bot_Before_Chime
{
    public class DirectLineSession : IDisposable
    {
        [Serializable]
        private class AuthenticationResults
        {
            public Conversation Conversation { get; set; }
            public DirectLineClient Client { get; set; }
        }

        [Serializable]
        private class Message
        {
            volatile string _watermark;

            public string Watermark
            {
                get => _watermark;
                set => _watermark = value;
            }
        }
        public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();
        private readonly Message _message = new Message();
        private AuthenticationResults _result;
        private readonly ChannelAccount _from;
        private Guid _guid;

        private readonly Queue<string> _processedActivities = new Queue<string>();
        public SessionHandler _session;
        public BotInfo Bot { get; }
        
        public DirectLineSession(SessionHandler session, BotInfo botInfo)
        {
            _guid = Guid.NewGuid();
            _from = new ChannelAccount(_guid.ToString(), session.profile.Name, RoleTypes.User);
          
            Bot = botInfo;
            if (Bot.BotSecret == null)
            {
                throw new ArgumentException("Bot Secret is null", nameof(botInfo.BotSecret));
            }
            _session = session;
        }
        public async Task Start()
        {

            _result = await StartConversationAsync(CancellationTokenSource.Token);
            await RetrieveMessagesAsync(_result.Conversation, _session, CancellationTokenSource);
            await RefreshTokensAsync(_result.Conversation, _result.Client, CancellationTokenSource.Token);
        }
        private async Task<AuthenticationResults> StartConversationAsync(CancellationToken cancelToken)
        {

            var client = new DirectLineClient(Bot.BotSecret);
            var conversation = await client.Conversations.StartConversationAsync(cancelToken).ConfigureAwait(false);
            if (conversation == null)
            {
                throw new InvalidOperationException("Unable to create DirectLine conversation - conversation is null");
            }

            return
                new AuthenticationResults
                {
                    Conversation = conversation,
                    Client = client
                };
        }
        private async Task RetrieveMessagesAsync(Conversation conversation, SessionHandler session, CancellationTokenSource cancelSource)
        {
            if (cancelSource.IsCancellationRequested)
            {
                return;
            }

#if DEBUG
            const int receiveChunkSize = 100;
#else
            const int receiveChunkSize = 1024;
#endif

            var websocket = new ClientWebSocket();
            try
            {
                await websocket.ConnectAsync(new Uri(conversation.StreamUrl), cancelSource.Token);
              
            }
            catch (Exception ex)
            {
                return;
            }

            var _ = Task.Run(async () => {
                try
                {
                    while (websocket.State == WebSocketState.Open)
                    {
                        try
                        {
                            var allBytes = new List<byte>();
                            var result = new WebSocketReceiveResult(0, WebSocketMessageType.Text, false);
                            var buffer = new byte[receiveChunkSize];

                            while (!result.EndOfMessage)
                            {
                                if (cancelSource.IsCancellationRequested)
                                {
                                    return;
                                }
                                result = await websocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancelSource.Token);
                                allBytes.AddRange(buffer.Where(b => b != 0));
                                buffer = new byte[receiveChunkSize];
                            }

                            var message = Encoding.UTF8.GetString(allBytes.ToArray()).Trim();
                            
                            var activitySet = JsonConvert.DeserializeObject<ActivitySet>(message);
                            if (activitySet != null)
                            {
                                _message.Watermark = activitySet.Watermark;
                            }

                            if (CanDisplayMessage(message, activitySet, out var activities))
                            {
                                activities.ForEach(async activity => {

                                    List<Attachment> attachments = new List<Attachment>();
                                    if (activity.Attachments != null)
                                    {
                                        attachments = activity.Attachments.Select(a => new Microsoft.Bot.Schema.Attachment
                                        {
                                            Name = a.Name,
                                            Content = a.Content,
                                            ContentType = a.ContentType,
                                            ContentUrl = a.ContentUrl,
                                            ThumbnailUrl = a.ThumbnailUrl
                                        }).ToList();
                                    }
                                    var reply = MessageFactory.Attachment(attachments);

                                    await session.context.SendActivityAsync(reply);
                                });

                                foreach (var activity in activities)
                                {

                                    await HandleMessage(activity);
                                }
                            }
                            else
                            {
                                foreach (var activity in activities)
                                {
                                    await HandleSeekerMessage(activity, session);
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // do nothing here, the directline session got cancelled
                        }
                        catch (WebSocketException wsex)
                        {
                           
                        }
                        catch (Exception ex)
                        {
                            
                        }
                    }
                   
                }
                catch (Exception ex)
                {
                   
                }
            }).ContinueWith(async task => {
                if (cancelSource.IsCancellationRequested)
                {
                    return;
                }
                
                await RetrieveMessagesAsync(conversation, session, cancelSource);
            });

        }
        private bool CanDisplayMessage(string message, ActivitySet activitySet, out List<Activity> activities)
        {
            if (activitySet == null)
            {
                activities = new List<Activity>();
            }
            else
            {
                activities = activitySet.Activities
                    .Where(activity => IsFromBot(activity) && HasContent(activity))
                    .Select(activity => activity)
                    .OrderBy(a => a.Timestamp)
                    .ToList();
            }

            return !string.IsNullOrWhiteSpace(message) && activities.Any();

        }

        private bool IsFromBot(Activity activity)
        {
            if (activity == null)
            {
                return false;
            }

            return activity.From.Id == Bot.BotId
                   /* I don't know why, but the Q&A maker bots do this, rather than using an actual id for the bot */
                   || activity.From.Id == activity.From.Name
                   || Bot.BotName == activity.From.Id;
        }

        private static bool HasContent(Activity activity)
        {
            if (activity == null)
            {
                return false;
            }
            var hasText = !string.IsNullOrEmpty(activity.Text);
            var hasAttachments = activity.Attachments != null && activity.Attachments.Any();
            return hasText || hasAttachments;
        }
        private async Task HandleSeekerMessage(Activity activity, SessionHandler session)
        {
            try
            {

                var attachments = new List<Attachment>();
                if (activity.Attachments != null)
                {
                    attachments = activity.Attachments.Select(a => new Attachment
                    {
                        Name = a.Name,
                        Content = a.Content,
                        ContentType = a.ContentType,
                        ContentUrl = a.ContentUrl,
                        ThumbnailUrl = a.ThumbnailUrl
                    }
                    ).ToList();
                }

                var attachmentLayout = activity.AttachmentLayout ?? AttachmentLayoutTypes.List;


                if (string.IsNullOrEmpty(activity.Text) && attachments.Any())
                {
                    await SendMessage(string.Empty, new AttachmentData(attachmentLayout, attachments.ToArray()));
                }
                else
                {
                    if (activity.SuggestedActions?.Actions?.Any() == true)
                    {
                        var card = new Microsoft.Bot.Schema.HeroCard
                        {
                           
                            Text = activity.Text.Replace("<br>", Environment.NewLine),
                            Buttons = activity.SuggestedActions.Actions.Select(a => new CardAction
                            {
                                Type = a.Type,
                                Title = a.Title,
                                Image = a.Image,
                                Value = a.Value
                            }
                            ).ToList()
                        };
                        var attachment = card.ToAttachment();
                        attachments.Add(attachment);
                        var reply = MessageFactory.Attachment(attachments);
                        await session.context.SendActivityAsync(reply);
                    }
                    else
                    {
                        await SendMessage(activity.Text, new AttachmentData(attachmentLayout, attachments.ToArray()));
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
        private async Task HandleMessage(Activity activity)
        {
            try
            {
                
                var attachments = new List<Attachment>();
                if (activity.Attachments != null)
                {
                    attachments = activity.Attachments.Select(a => new Attachment
                    {
                        Name = a.Name,
                        Content = a.Content,
                        ContentType = a.ContentType,
                        ContentUrl = a.ContentUrl,
                        ThumbnailUrl = a.ThumbnailUrl
                    }
                    ).ToList();
                }

                var attachmentLayout = activity.AttachmentLayout ?? AttachmentLayoutTypes.List;
               

                if (string.IsNullOrEmpty(activity.Text) && attachments.Any())
                {
                    await SendMessage(string.Empty, new AttachmentData(attachmentLayout, attachments.ToArray()));
                }
                else
                {
                    if (activity.SuggestedActions?.Actions?.Any() == true)
                    {
                        var card = new Microsoft.Bot.Schema.HeroCard
                        {
                            Text = activity.Text.Replace("<br>", Environment.NewLine),
                            Buttons = activity.SuggestedActions.Actions.Select(a => new CardAction
                            {
                                Type = a.Type,
                                Title = a.Title,
                                Image = a.Image,
                                Value = a.Value
                            }
                            ).ToList()
                        };
                        var attachment = card.ToAttachment();
                        attachments.Add(attachment);
                        await SendMessage(string.Empty, new AttachmentData(attachmentLayout, attachments.ToArray()));
                    }
                    else
                    {
                        await SendMessage(activity.Text, new AttachmentData(attachmentLayout, attachments.ToArray()));
                    }
                }
            }
            catch (Exception ex)
            {
                
            }
        }
        public async Task SendMessage(string message, object value)
        {
            var activity = (Activity)Activity.CreateMessageActivity();
            activity.ChannelData = "";
            activity.From = new Microsoft.Bot.Connector.DirectLine.ChannelAccount(_guid.ToString(), _from.Name);
            activity.Text = message;
            if (value != null)
            {
                activity.Value = value;
            }

            var response = await _result.Client.Conversations.PostActivityAsync(_result.Conversation.ConversationId, activity, CancellationTokenSource.Token);
           
        }
        private async Task RefreshTokensAsync(Conversation conversation, DirectLineClient client, CancellationToken cancelToken)
        {
            const int toMilliseconds = 1000;
            const int beforeExpiration = 60000;

            var _ = Task.Run(async () => {
                try
                {
                    
                    var millisecondsToRefresh = ((conversation.ExpiresIn ?? 10) * toMilliseconds) - beforeExpiration;

                    while (true)
                    {
                        await Task.Delay(millisecondsToRefresh, cancelToken);

                        await client.Conversations.ReconnectToConversationAsync(
                            conversation.ConversationId,
                            _message.Watermark,
                            cancelToken);
                    }
                }
                catch (OperationCanceledException oce)
                {
                    Console.WriteLine(oce.Message);
                }
                catch (ObjectDisposedException)
                {
                    // do nothing, CancelationTokenSource has been disposed
                }
                catch (Exception ex)
                {

                }
            }, cancelToken);
            await Task.FromResult(0);
        }
        public void Dispose()
        {
            CancellationTokenSource.Cancel();
            CancellationTokenSource.Dispose();
        }
    }
}

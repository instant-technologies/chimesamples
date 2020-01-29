using Activity = Microsoft.Bot.Connector.DirectLine.Activity;
using Conversation = Microsoft.Bot.Connector.DirectLine.Conversation;
using Attachment = Microsoft.Bot.Schema.Attachment;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.DirectLine;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Linq;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using System.IO;
using System.Net.Http;
using System.Net;
using Nancy.Json;

namespace CustomerBot
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
            _from = new ChannelAccount(_guid.ToString(), session.profile.Name);
            
            Bot = botInfo;
            _session = session;
        }
        public async Task Start()
        {

            _result = await StartConversationAsync(CancellationTokenSource.Token);
            await RetrieveMessagesAsync(_result.Conversation, _session, CancellationTokenSource);

            await RefreshTokensAsync(_result.Conversation, _result.Client, CancellationTokenSource.Token);

        }
        private async Task RefreshTokensAsync(Conversation conversation, DirectLineClient client, CancellationToken cancelToken)
        {
            const int toMilliseconds = 1000;
            const int beforeExpiration = 60000;

            var runTask = Task.Run(async () => {
                try
                {
                    
                    int millisecondsToRefresh = ((conversation.ExpiresIn ?? 10) * toMilliseconds) - beforeExpiration;

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
            }, cancelToken);
            await Task.FromResult(0);
        }
        private async Task<AuthenticationResults> StartConversationAsync(CancellationToken cancelToken)
        {
            var client = new DirectLineClient(Bot.BotSecret);
            
            var conversation = await client.Conversations.StartConversationAsync(cancelToken);
            

            return
                new AuthenticationResults
                {
                    Conversation = conversation,
                    Client = client
                };
        }
        private async Task RetrieveMessagesAsync(Conversation conversation, SessionHandler session, CancellationTokenSource cancelSource)
        {
            
            const int receiveChunkSize = 1024;
            var websocket = new ClientWebSocket();
            try
            {
                await websocket.ConnectAsync(new Uri(conversation.StreamUrl), cancelSource.Token);
               
            }
            catch (Exception ex)
            {
                
            }
            var runTask = Task.Run(async () => {
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
                                result = await websocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancelSource.Token);
                                allBytes.AddRange(buffer);
                                buffer = new byte[receiveChunkSize];
                            }

                            var message = Encoding.UTF8.GetString(allBytes.ToArray()).Trim();

                            var activitySet = JsonConvert.DeserializeObject<ActivitySet>(message);
                            if (activitySet != null)
                            {
                                _message.Watermark = activitySet.Watermark;
                              
                            }

                            if (CanDisplayMessage(message, activitySet, out List<Activity> activities))
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
                            }
                            
                        }
                        catch (OperationCanceledException)
                        {
                            // do nothing here, the directline session got cancelled
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
        public static Microsoft.Bot.Schema.Attachment CreateAdaptiveCardAttachment()
        {
            // combine path for cross platform support
            string[] paths = { ".", "Resources", "adaptiveCard.json" };
            var adaptiveCardJson = File.ReadAllText(Path.Combine(paths));

            var adaptiveCardAttachment = new Microsoft.Bot.Schema.Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCardJson),
            };
            return adaptiveCardAttachment;
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
                    .ToList();
            }

            return !string.IsNullOrWhiteSpace(message) && activities.Any();

        }
        private bool IsFromBot(Activity activity)
        {
            return activity.From.Id == Bot.BotId
                   /* I don't know why, but the Q&A maker bots do this, rather than using an actual id for the bot */
                   || activity.From.Id == activity.From.Name;
        }
        private bool HasContent(Activity activity)
        {
            return !string.IsNullOrEmpty(activity.Text) || activity.Attachments.Any();
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
            await PostSeekerDataAsync();
        }
        public async Task PostSeekerDataAsync()
        {
           // In the future we will be using directline ChannelData attributes to handle these name value pairs but for now we send a json data bag into the chime system via an api route. 
            string json = new JavaScriptSerializer().Serialize(new SeekerData
            {
                firstName = _session.profile.Name,
                lastName = "Bot visitor",
                email = "foo1@bar.com",
                location = "somewheresville",
                question = _session.profile.Question,
                webVisitor = true,
                domainAuthenticated = false,
                sip = "",
                SeekerDN = "Bot Visitor",
                ip = "123.456.789.10",
                referrerURL = "www.blabla.com",
                hostname = "chimebot",
                id = 1,
                sessionGuid = _guid.ToString(),
                seekerOffsetMinutes = -30,
                platform = "Windows",
        });


            string myJson = "seekerData:" + json;
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(
                    "https://ch-teams-net1.imchime.com/Chime/webclient/SeekerData/" + _guid.ToString(),
                     new StringContent(myJson, Encoding.UTF8, "application/json"));
            }

        }
        public void Dispose()
        {
            CancellationTokenSource.Cancel();
            CancellationTokenSource.Dispose();
        }
    }
}

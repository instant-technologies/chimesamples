using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.DirectLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot_Before_Chime
{
    public class SessionHandler
    {
        private UserProfile userprofile;
        private ITurnContext turnContext;
        internal SessionHandler(UserProfile profile, ITurnContext turncontext)
        {
            this.userprofile = profile;
            this.turnContext = turncontext;
        }
        public UserProfile profile
        {
            get { return this.userprofile; }
            set { this.userprofile = value; }
        }
        public ITurnContext context
        {
            get { return this.turnContext; }
            set { this.turnContext = value; }
        }

    }
}

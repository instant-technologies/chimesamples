using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomerBot.BotConnections
{
    public class ChatID : IEquatable<ChatID>
    {
        private string ID { get; }

        public ChatID(string id)
        {
            ID = id;
        }

        public bool Equals(ChatID other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(ID, other.ID, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ChatID)obj);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(ID);
        }

        public static bool operator ==(ChatID left, ChatID right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ChatID left, ChatID right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return ID;
        }


    }
}

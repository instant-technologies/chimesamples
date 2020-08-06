using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Chime_Bot_QueueCommands
{
    public class ConversationFlow
    {
        // Identifies the last question asked.
        public enum Question
        {
            FirstMessage,
            SecondMessage,
            ThirdMessage,
            None, // Our last action did not involve a question.
        }

        // The last question asked.
        public Question LastQuestionAsked { get; set; } = Question.None;

    }
}

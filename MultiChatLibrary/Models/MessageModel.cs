using System.Text;

namespace MultiChatLibrary.Models
{
    public class MessageModel
    {
        public MessageModel(string issuer, string payload, State type)
        {
            Issuer = issuer;
            Payload = payload;
            Type = type;
        }

        public enum State : byte
        {
            CONNECT,
            DISCONNECT,
            MESSAGE
        }

        public string Issuer { get; set; }
        public string Payload { get; set; }
        public State Type { get; set; }

        public override string ToString()
        {
            StringBuilder messageString = new StringBuilder();
            messageString.Append($"@type:{Type}|");
            messageString.Append($"@issuer:{Issuer}|");
            messageString.Append($"@payload:{Payload}|\n");
            return messageString.ToString();
        }
    }
}
using System;
using System.Text;
using MultiChatLibrary.Models;
using static MultiChatLibrary.Models.MessageModel;

namespace MultiChatLibrary
{
    public static class MultiChatLibrary
    {
        private const string DELIMITER = "|";

        public static MessageModel ExtractMessage(string text)
        {
            Console.WriteLine(text);
            string msg = GetBetween(text, "@payload:", DELIMITER);
            string issuer = GetBetween(text, "@issuer:", DELIMITER);
            Enum.TryParse(GetBetween(text, "@type:", DELIMITER), out State state);
            return new MessageModel(issuer, msg, state);
        }

        private static string GetBetween(string strSource, string strStart, string strEnd)
        {
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                int Start, End;
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }

            return "";
        }
    }
}
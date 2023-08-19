using System.Net;

namespace MultiChatLibrary.Models
{
    public class SettingsModel
    {
        private const string Delimiter = "|";
        private const string Eom = "\n";

        private enum Type : byte
        {
            Connect
        }

        public int BufferSize { get; set; } = 1024;

        public IPAddress IpAddress { get; set; } = IPAddress.Any;

        public string Message { get; set; } = $"@type:{Type.Connect}{Delimiter}@issuer:SERVER{Delimiter}@payload:Starting...{Delimiter}{Eom}";

        public string Name { get; set; } = "NOTS Chat Server";

        public int Port { get; set; } = 9000;
    }
}
using System.Net;

namespace MultiChatLibrary.Models
{
    public class SettingsModel
    {
        private const string DELIMITER = "|";
        private const string EOM = "\n";

        private enum Type : byte
        {
            CONNECT,
            DISCONNECT,
            MESSAGE
        }

        public int BufferSize { get; set; } = 1024;

        public IPAddress IPAddress { get; set; } = IPAddress.Any;

        public string Message { get; set; } = $"@type:{Type.CONNECT}{DELIMITER}@issuer:SERVER{DELIMITER}@payload:Starting...{DELIMITER}{EOM}";

        public string Name { get; set; } = "NOTS Chat Server";

        public int Port { get; set; } = 9000;
    }
}
#region Namespace reference

using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MultiChatLibrary.Models;
using MultiChatLibrary.Validators;
using static MultiChatLibrary.Models.MessageModel;

#endregion Namespace reference

namespace ChatClient
{
    public partial class MainWindow : Window
    {
        #region Fields

        private const string Delimiter = "|";
        private const string Eom = "\n";

        private readonly SettingsModel _settings = new SettingsModel();

        private NetworkStream _stream;

        private TcpClient _tcpClient;

        private enum Type : byte
        {
            Connect,
            Disconnect,
            Message
        }

        #endregion Fields

        #region Form events

        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion Form events

        #region Methods

        /// <summary>
        /// Adds a message to the messagelog.
        /// </summary>
        /// <param name="message"><see cref="string">string</see> representing the message to be added.</param>
        /// <param name="issuer"><see cref="string">string</see> representing the issuer of the message.</param>
        private void AddMessage(string message, string issuer = "") => Dispatcher.Invoke(() => ListChats.Items.Add($"[{DateTime.Now:HH:mm:ss}] {(issuer != "" ? $"{issuer}: " : "")}{message}"));

        /// <summary>
        /// Converts the type, issuer and payload to a string able to be sent across the network.
        /// </summary>
        /// <param name="type">Type of message, either: Connect, DISCONNECT or MESSAGE</param>
        /// <param name="issuer">Sender of the message</param>
        /// <param name="payload">Payload of the message</param>
        /// <returns>String in right format to be received by the server.</returns>
        private string ConvertMessage(Type type, string issuer, string payload = "") => $"@type:{type}{Delimiter}@issuer:{issuer}{Delimiter}@payload:{payload}{Delimiter}{Eom}";

        /// <summary>
        /// Toggles the connect/disconnect to server button.
        /// </summary>
        private void ToggleStartStopButton()
        {
            switch (BtnConnect.Visibility)
            {
                case Visibility.Hidden:
                    BtnConnect.Visibility = Visibility.Visible;
                    break;
                case Visibility.Visible:
                    BtnConnect.Visibility = Visibility.Hidden;
                    break;
                case Visibility.Collapsed:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (BtnDisconnect.Visibility)
            {
                case Visibility.Hidden:
                    BtnDisconnect.Visibility = Visibility.Visible;
                    break;
                case Visibility.Visible:
                    BtnDisconnect.Visibility = Visibility.Hidden;
                    break;
                case Visibility.Collapsed:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion Methods

        #region Control events

        /// <summary>
        /// Attempts to connect the client to the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            AddMessage("Connecting...", "INFO");
            var errors = SettingsValidator.ValidateSettings(_settings);
            if (errors != null)
            {
                foreach (var error in errors)
                {
                    AddMessage(error, "ERROR");
                }
            }
            else
            {
                try
                {
                    _tcpClient = new TcpClient(_settings.IpAddress.ToString(), _settings.Port);
                    _stream = _tcpClient.GetStream();

                    var buffer = Encoding.ASCII.GetBytes(ConvertMessage(Type.Connect, _settings.Name));
                    await _stream.WriteAsync(buffer, 0, buffer.Length);

                    _ = Task.Run(ReceiveData);
                    AddMessage("Connected!", "INFO");
                    Dispatcher.Invoke(ToggleStartStopButton);
                }
                catch (SocketException)
                {
                    AddMessage("Connection refused!", "ERROR");
                }
                catch (Exception ex)
                {
                    AddMessage(ex.ToString(), "ERROR");
                }
            }
        }

        /// <summary>
        /// Disconnects client from the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            var buffer = Encoding.ASCII.GetBytes(ConvertMessage(Type.Disconnect, _settings.Name));

            await _stream.WriteAsync(buffer, 0, buffer.Length);
            AddMessage("Disconnected...", "INFO");
            Dispatcher.Invoke(ToggleStartStopButton);
            _tcpClient.Close();
        }

        /// <summary>
        /// Send messages to clients and server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            _settings.Message = ConvertMessage(Type.Message, TxtName.Text, TxtMessage.Text);

            SettingsValidator.ValidateSettings(_settings);

            var message = MultiChatLibrary.MultiChatLibrary.ExtractMessage(_settings.Message);
            if (message.Issuer == _settings.Name) message.Issuer = "You";

            var buffer = Encoding.ASCII.GetBytes(_settings.Message);
            try
            {
                if (!_stream.CanWrite) return;
                if (message.Type == State.Connect) return;
                await _stream.WriteAsync(buffer, 0, buffer.Length);
                AddMessage(message.Payload, message.Issuer);
            }
            catch (ObjectDisposedException)
            {
                AddMessage("You are not connected!", "ERROR");
            }
            catch (Exception)
            {
                AddMessage("You are not connected!", "ERROR");
            }
            finally
            {
                TxtMessage.Clear();
                TxtMessage.Focus();
            }
        }

        /// <summary>
        /// Updates the Settings object to correspond the input given by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtBufferSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(TxtBufferSize.Text, out var bufferSize);
            _settings.BufferSize = bufferSize;
        }

        /// <summary>
        /// Updates the Settings object to correspond the input given by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtIPServer_TextChanged(object sender, TextChangedEventArgs e)
        {
            IPAddress.TryParse(TxtIpServer.Text, out var ip);
            _settings.IpAddress = ip;
        }

        /// <summary>
        /// Updates the Settings object to correspond the input given by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtMessage_TextChanged(object sender, TextChangedEventArgs e) => _settings.Message = TxtMessage.Text;

        /// <summary>
        /// Updates the Settings object to correspond the input given by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtName_TextChanged(object sender, TextChangedEventArgs e) => _settings.Name = TxtName.Text;

        /// <summary>
        /// Updates the Settings object to correspond the input given by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(TxtPort.Text, out var port);
            _settings.Port = port;
        }

        #endregion Control events

        #region Threads

        /// <summary>
        /// Receives data from server.
        /// </summary>
        private async void ReceiveData()
        {
            var buffer = new byte[_settings.BufferSize];

            while (_stream.CanRead)
            {
                var textBuffer = "";
                while (textBuffer.IndexOf(Eom) < 0)
                {
                    try
                    {
                        var readBytes = await _stream.ReadAsync(buffer, 0, _settings.BufferSize);
                        textBuffer += Encoding.ASCII.GetString(buffer, 0, readBytes);
                        Console.WriteLine(textBuffer);
                    }
                    catch (IOException)
                    {
                        _tcpClient.Close();
                        _stream.Close();
                        Dispatcher.Invoke(ToggleStartStopButton);
                        AddMessage("Server disconnected", "ERROR");
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        _tcpClient.Close();
                        _stream.Close();
                        Dispatcher.Invoke(ToggleStartStopButton);
                        AddMessage(e.ToString(), "ERROR");
                        break;
                    }
                }
                
                var message = MultiChatLibrary.MultiChatLibrary.ExtractMessage(textBuffer);
                Console.WriteLine(message.ToString());
                if (message.Issuer != _settings.Name)
                {
                    switch (message.Type)
                    {
                        case State.Connect:
                            AddMessage($"{message.Issuer} connected!");
                            break;

                        case State.Disconnect:
                            AddMessage($"{message.Issuer} disconnected!");
                            break;

                        case State.Message:
                            AddMessage(message.Payload, message.Issuer);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        #endregion Threads

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                var buffer = Encoding.ASCII.GetBytes(ConvertMessage(Type.Disconnect, _settings.Name));

                await _stream.WriteAsync(buffer, 0, buffer.Length);
                AddMessage("Disconnected...", "INFO");
                Dispatcher.Invoke(ToggleStartStopButton);
                _tcpClient.Close();
            }
            catch (ObjectDisposedException)
            {
                AddMessage("You are not connected!", "ERROR");
            }
            catch (Exception ex)
            {
                AddMessage(ex.ToString(), "ERROR");
            }
        }
    }
}
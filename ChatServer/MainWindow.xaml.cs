#region Namespace reference

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CSharp.RuntimeBinder;
using MultiChatLibrary.Models;
using MultiChatLibrary.Validators;
using static MultiChatLibrary.Models.MessageModel;

#endregion Namespace reference

namespace ChatServer
{
    public partial class MainWindow : Window
    {
        #region Fields

        private const string Eom = "\n";
        private readonly List<TcpClient> _connectionList = new List<TcpClient>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly SettingsModel _settings = new SettingsModel();
        private TcpListener _tcpListener;

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
        private void AddMessage(string message, string issuer = "")
        {
            Dispatcher.Invoke(() => ListChats.Items.Add($"[{DateTime.Now:HH:mm:ss}] {(issuer != "" ? $"{issuer}: " : "")}{message}"));
        }

        /// <summary>
        /// Toggles the start/stop server button.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void ToggleStartStopButton()
        {
            switch (BtnStart.Visibility)
            {
                // If btnStart is hidden
                case Visibility.Hidden:
                    BtnStart.Visibility = Visibility.Visible;
                    break;
                // If btnStart is visible
                case Visibility.Visible:
                    BtnStart.Visibility = Visibility.Hidden;
                    break;
                case Visibility.Collapsed:
                    break;
                default:
                    throw new RuntimeBinderException("Something went wrong.");
            }

            switch (BtnStop.Visibility)
            {
                // If btnStop is hidden
                case Visibility.Hidden:
                    BtnStop.Visibility = Visibility.Visible;
                    break;
                // If btnStop is visible
                case Visibility.Visible:
                    BtnStop.Visibility = Visibility.Hidden;
                    break;
                case Visibility.Collapsed:
                    break;
                default:
                    throw new RuntimeBinderException("Something went wrong.");
            }
        }

        #endregion Methods

        #region Control events

        /// <summary>
        /// Starts <see cref="TcpListener">TcpListener</see> and awaits <see cref="TcpClient">TcpClients</see>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            var errors = SettingsValidator.ValidateSettings(_settings);
            // If there are no errors in the user input
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
                    _tcpListener = new TcpListener(_settings.IpAddress, _settings.Port);
                    _tcpListener.Start();

                    Dispatcher.Invoke(ToggleStartStopButton);
                    AddMessage("Listening for client...", "INFO");
                    TcpClient client;
                    while ((client = await _tcpListener.AcceptTcpClientAsync()) != null)
                    {
                        var localClient = client;
                        _ = Task.Run(() => ReceiveData(localClient));
                    }
                }
                catch (ObjectDisposedException)
                {
                    AddMessage("Server stopped.", "INFO");
                }
                catch (OperationCanceledException error)
                {
                    AddMessage(error.ToString(), "ERROR");
                }
                catch (SocketException error)
                {
                    AddMessage(error.ToString(), "ERROR");
                    AddMessage("Port already in use.", "ERROR");
                }
                catch (Exception ex)
                {
                    AddMessage(ex.ToString(), "ERROR");
                    Dispatcher.Invoke(ToggleStartStopButton);
                }
            }
        }

        /// <summary>
        /// Stops the <see cref="TcpListener">TcpListener</see> and disconnects all connected clients.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            AddMessage("Stopping the server...", "INFO");
            _cts.Cancel();
            Dispatcher.Invoke(ToggleStartStopButton);
            _tcpListener.Stop();
            // If there are clients connected
            if (_connectionList.Count <= 0) return;
            foreach (var client in _connectionList)
            {
                client.Close();
            }
        }

        /// <summary>
        /// Updates the Settings object to correspond the input given by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtBufferSize_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            int.TryParse(TxtBufferSize.Text, out var bufferSize);
            _settings.BufferSize = bufferSize;
        }

        /// <summary>
        /// Updates the Settings object to correspond the input given by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtIpAddress_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            IPAddress.TryParse(TxtIpAddress.Text, out var ip);
            _settings.IpAddress = ip;
        }

        /// <summary>
        /// Updates the Settings object to correspond the input given by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => _settings.Name = TxtName.Text;

        /// <summary>
        /// Updates the Settings object to correspond the input given by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtPort_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            int.TryParse(TxtPort.Text, out var portNumber);
            _settings.Port = portNumber;
        }

        #endregion Control events

        #region Threads

        /// <summary>
        /// Receives data from all clients.
        /// </summary>
        /// <param name="client"><see cref="TcpClient"/> on a different thread than namespace.</param>
        /// <returns></returns>
        private async Task ReceiveData(TcpClient client)
        {
            _connectionList.Add(client);

            var buffer = new byte[_settings.BufferSize];
            var netStream = client.GetStream();

            // While the network stream of the client can read.
            while (netStream.CanRead)
            {
                var textBuffer = "";
                // While the EOM in the message is not reached.
                while (textBuffer.IndexOf(Eom) < 0)
                {
                    try
                    {
                        var readBytes = await netStream.ReadAsync(buffer, 0, _settings.BufferSize);
                        textBuffer += Encoding.ASCII.GetString(buffer, 0, readBytes);
                    }
                    catch (IOException)
                    {
                        client.Close();
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        netStream.Close();
                        client.Close();
                        Dispatcher.Invoke(ToggleStartStopButton);
                        AddMessage(e.ToString(), "ERROR");
                        break;
                    }
                }

                var message = MultiChatLibrary.MultiChatLibrary.ExtractMessage(textBuffer);

                // Switch between message types
                await MessageTypeSwitch(client, message);
            }
        }

        private async Task MessageTypeSwitch(TcpClient client, MessageModel message)
        {
            switch (message.Type)
            {
                case State.Connect:
                    if (message.Issuer != "")
                    {
                        await Dispatcher.InvokeAsync(() => ListClients.Items.Add(message.Issuer));
                        AddMessage($"{message.Issuer} connected!");
                        _ = SendMessageToClients(message);
                    }

                    break;

                case State.Disconnect:
                    await Dispatcher.InvokeAsync(() => ListClients.Items.Remove(message.Issuer));
                    AddMessage($"{message.Issuer} disconnected!");
                    _ = SendMessageToClients(message);
                    break;

                case State.Message:
                    if (message.Payload != "")
                    {
                        AddMessage(message.Payload, message.Issuer);
                        _ = SendMessageToClients(message);
                    }

                    break;

                default:
                    client.Close();
                    break;
            }
        }

        private async Task SendMessageToClients(MessageModel message)
        {
            try
            {
                foreach (var c in _connectionList)
                {
                    var networkStream = c.GetStream();
                    var serverMessageByteArray = Encoding.ASCII.GetBytes(message.ToString());
                    await networkStream.WriteAsync(serverMessageByteArray, 0, serverMessageByteArray.Length);
                }
            }
            catch (ObjectDisposedException)
            {
                AddMessage("Client disconnected.", "INFO");
            }
            catch (Exception ex)
            {
                AddMessage(ex.ToString(), "ERROR in SendMessageToClients");
            }
        }

        #endregion Threads
    }
}
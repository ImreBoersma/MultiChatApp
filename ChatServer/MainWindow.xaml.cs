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
using MultiChatLibrary.Models;
using MultiChatLibrary.Validators;
using static MultiChatLibrary.Models.MessageModel;

#endregion Namespace reference

namespace ChatServer
{
    public partial class MainWindow : Window
    {
        #region Fields

        private const string EOM = "\n";
        private readonly List<TcpClient> connectionList = new List<TcpClient>();
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private readonly SettingsModel settings = new SettingsModel();
        private readonly SettingsValidator validator = new SettingsValidator();
        private TcpListener tcpListener;

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
            Dispatcher.Invoke(() => listChats.Items.Add($"[{DateTime.Now:HH:mm:ss}] {(issuer != "" ? $"{issuer}: " : "")}{message}"));
        }

        /// <summary>
        /// Toggles the start/stop server button.
        /// </summary>
        private void ToggleStartStopButton()
        {
            // If btnStart is hidden
            if (btnStart.Visibility == Visibility.Hidden)
            {
                btnStart.Visibility = Visibility.Visible;
            }
            // If btnStart is visible
            else if (btnStart.Visibility == Visibility.Visible)
            {
                btnStart.Visibility = Visibility.Hidden;
            }

            // If btnStop is hidden
            if (btnStop.Visibility == Visibility.Hidden)
            {
                btnStop.Visibility = Visibility.Visible;
            }
            // If btnStop is visible
            else if (btnStop.Visibility == Visibility.Visible)
            {
                btnStop.Visibility = Visibility.Hidden;
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
            List<string> errors = validator.validateSettings(settings);
            // If there are no errors in the user input
            if (errors != null)
            {
                foreach (string error in errors)
                {
                    AddMessage(error, "ERROR");
                }
            }
            else
            {
                try
                {
                    tcpListener = new TcpListener(settings.IPAddress, settings.Port);
                    tcpListener.Start();

                    Dispatcher.Invoke(() => ToggleStartStopButton());
                    AddMessage("Listening for client...", "INFO");
                    TcpClient client;
                    while ((client = await tcpListener.AcceptTcpClientAsync()) != null)
                    {
                        _ = Task.Run(() => ReceiveData(client));
                    }
                }
                catch (ObjectDisposedException)
                { }
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
                    Dispatcher.Invoke(() => ToggleStartStopButton());
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
            cts.Cancel();
            Dispatcher.Invoke(() => ToggleStartStopButton());
            tcpListener.Stop();
            // If there are clients connected
            if (connectionList.Count > 0)
            {
                foreach (TcpClient client in connectionList)
                {
                    client.Close();
                }
            }
        }

        /// <summary>
        /// Updates the Settings object to correspond the input given by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtBufferSize_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            int.TryParse(txtBufferSize.Text, out int bufferSize);
            settings.BufferSize = bufferSize;
        }

        /// <summary>
        /// Updates the Settings object to correspond the input given by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtIpAddress_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            IPAddress.TryParse(txtIpAddress.Text, out IPAddress ip);
            settings.IPAddress = ip;
        }

        /// <summary>
        /// Updates the Settings object to correspond the input given by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => settings.Name = txtName.Text.ToString();

        /// <summary>
        /// Updates the Settings object to correspond the input given by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtPort_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            int.TryParse(txtPort.Text, out int portNumber);
            settings.Port = portNumber;
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
            connectionList.Add(client);

            byte[] buffer = new byte[settings.BufferSize];
            NetworkStream netStream = client.GetStream();

            // While the networkstream of the client can read.
            while (netStream.CanRead)
            {
                string textBuffer = "";
                // While the EOM in the message is not reached.
                while (textBuffer.IndexOf(EOM) < 0)
                {
                    try
                    {
                        int readBytes = await netStream.ReadAsync(buffer, 0, settings.BufferSize);
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
                        Dispatcher.Invoke(() => ToggleStartStopButton());
                        AddMessage(e.ToString(), "ERROR");
                        break;
                    }
                }

                MessageModel message = MultiChatLibrary.MultiChatLibrary.ExtractMessage(textBuffer);

                // Switch between message types
                switch (message.Type)
                {
                    case State.CONNECT:
                        if (message.Issuer != "")
                        {
                            await Dispatcher.InvokeAsync(() => listClients.Items.Add(message.Issuer));
                            AddMessage($"{message.Issuer} connected!");
                            _ = SendMessageToClients(message);
                            break;
                        }
                        break;

                    case State.DISCONNECT:
                        await Dispatcher.InvokeAsync(() => listClients.Items.Remove(message.Issuer));
                        AddMessage($"{message.Issuer} disconnected!");
                        _ = SendMessageToClients(message);
                        break;

                    case State.MESSAGE:
                        if (message.Payload != "")
                        {
                            AddMessage(message.Payload, message.Issuer);
                            _ = SendMessageToClients(message);
                            break;
                        }
                        break;

                    default:
                        client.Close();
                        break;
                }
            }
        }

        private async Task SendMessageToClients(MessageModel message)
        {
            try
            {
                foreach (TcpClient c in connectionList)
                {
                    NetworkStream networkStream = c.GetStream();
                    byte[] serverMessageByteArray = Encoding.ASCII.GetBytes(message.ToString());
                    await networkStream.WriteAsync(serverMessageByteArray, 0, serverMessageByteArray.Length);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                AddMessage(ex.ToString(), "ERROR in SendMessageToClients");
            }
        }

        #endregion Threads
    }
}
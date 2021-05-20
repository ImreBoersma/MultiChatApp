#region Namespace reference

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MultiChatLibrary.Models;
using MultiChatLibrary.Validators;
using static MultiChatLibrary.Models.MessageModel;

#endregion Namespace reference

namespace ChatClient
{
    public partial class MainWindow : Window
    {
        #region Fields

        private const string DELIMITER = "|";
        private const string EOM = "\n";

        private readonly SettingsModel Settings = new SettingsModel();

        private readonly SettingsValidator Validator = new SettingsValidator();

        private NetworkStream stream;

        private TcpClient tcpClient;

        private enum Type : byte
        {
            CONNECT,
            DISCONNECT,
            MESSAGE
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
        private void AddMessage(string message, string issuer = "") => Dispatcher.Invoke(() => listChats.Items.Add($"[{DateTime.Now:HH:mm:ss}] {(issuer != "" ? $"{issuer}: " : "")}{message}"));

        /// <summary>
        /// Converts the type, issuer and payload to a string able to be sent across the network.
        /// </summary>
        /// <param name="type">Type of message, either: CONNECT, DISCONNECT or MESSAGE</param>
        /// <param name="issuer">Sender of the message</param>
        /// <param name="payload">Payload of the message</param>
        /// <returns>String in right format to be received by the server.</returns>
        private string ConvertMessage(Type type, string issuer, string payload = "") => $"@type:{type}{DELIMITER}@issuer:{issuer}{DELIMITER}@payload:{payload}{DELIMITER}{EOM}";

        /// <summary>
        /// Toggles the connect/disconnect to server button.
        /// </summary>
        private void ToggleStartStopButton()
        {
            if (btnConnect.Visibility == Visibility.Hidden)
            {
                btnConnect.Visibility = Visibility.Visible;
            }
            else if (btnConnect.Visibility == Visibility.Visible)
            {
                btnConnect.Visibility = Visibility.Hidden;
            }

            if (btnDisconnect.Visibility == Visibility.Hidden)
            {
                btnDisconnect.Visibility = Visibility.Visible;
            }
            else if (btnDisconnect.Visibility == Visibility.Visible)
            {
                btnDisconnect.Visibility = Visibility.Hidden;
            }
        }

        #endregion Methods

        #region Control events

        /// <summary>
        /// Attempts to connect the client to the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            AddMessage("Connecting...", "INFO");
            List<string> errors = Validator.validateSettings(Settings);
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
                    tcpClient = new TcpClient(Settings.IPAddress.ToString(), Settings.Port);
                    stream = tcpClient.GetStream();

                    byte[] buffer = Encoding.ASCII.GetBytes(ConvertMessage(Type.CONNECT, Settings.Name));
                    await stream.WriteAsync(buffer, 0, buffer.Length);

                    _ = Task.Run(() => ReceiveData());
                    AddMessage("Connected!", "INFO");
                    Dispatcher.Invoke(() => ToggleStartStopButton());
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
            byte[] buffer = Encoding.ASCII.GetBytes(ConvertMessage(Type.DISCONNECT, Settings.Name));

            await stream.WriteAsync(buffer, 0, buffer.Length);
            AddMessage("Disconnected...", "INFO");
            Dispatcher.Invoke(() => ToggleStartStopButton());
            tcpClient.Close();
        }

        /// <summary>
        /// Send messages to clients and server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            Settings.Message = ConvertMessage(Type.MESSAGE, txtName.Text, txtMessage.Text);

            Validator.validateSettings(Settings);

            MessageModel message = MultiChatLibrary.MultiChatLibrary.ExtractMessage(Settings.Message);
            if (message.Issuer == Settings.Name) message.Issuer = "You";

            byte[] buffer = Encoding.ASCII.GetBytes(Settings.Message);
            try
            {
                if (stream.CanWrite)
                {
                    if (message.Type != MessageModel.State.CONNECT)
                    {
                        await stream.WriteAsync(buffer, 0, buffer.Length);
                        AddMessage(message.Payload, message.Issuer);
                    }
                }
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception)
            {
                AddMessage("You are not connected!", "ERROR");
            }
            finally
            {
                txtMessage.Clear();
                txtMessage.Focus();
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
            Settings.BufferSize = bufferSize;
        }

        /// <summary>
        /// Updates the Settings object to correspond the input given by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtIPServer_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            IPAddress.TryParse(txtIPServer.Text, out IPAddress ip);
            Settings.IPAddress = ip;
        }

        /// <summary>
        /// Updates the Settings object to correspond the input given by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtMessage_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => Settings.Message = txtMessage.Text;

        /// <summary>
        /// Updates the Settings object to correspond the input given by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => Settings.Name = txtName.Text;

        /// <summary>
        /// Updates the Settings object to correspond the input given by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtPort_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            int.TryParse(txtPort.Text, out int port);
            Settings.Port = port;
        }

        #endregion Control events

        #region Threads

        /// <summary>
        /// Receives data from server.
        /// </summary>
        private async void ReceiveData()
        {
            byte[] buffer = new byte[Settings.BufferSize];

            while (stream.CanRead)
            {
                string textBuffer = "";
                while (textBuffer.IndexOf(EOM) < 0)
                {
                    try
                    {
                        int readBytes = await stream.ReadAsync(buffer, 0, Settings.BufferSize);
                        textBuffer += Encoding.ASCII.GetString(buffer, 0, readBytes);
                        Console.WriteLine(textBuffer);
                    }
                    catch (IOException)
                    {
                        tcpClient.Close();
                        stream.Close();
                        Dispatcher.Invoke(() => ToggleStartStopButton());
                        AddMessage("Server disconnected", "ERROR");
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        tcpClient.Close();
                        stream.Close();
                        Dispatcher.Invoke(() => ToggleStartStopButton());
                        AddMessage(e.ToString(), "ERROR");
                        break;
                    }
                }
                MessageModel message = MultiChatLibrary.MultiChatLibrary.ExtractMessage(textBuffer);
                Console.WriteLine(message.ToString());
                if (message.Issuer != Settings.Name)
                {
                    switch (message.Type)
                    {
                        case State.CONNECT:
                            AddMessage($"{message.Issuer} connected!");
                            break;

                        case State.DISCONNECT:
                            AddMessage($"{message.Issuer} disconnected!");
                            break;

                        case State.MESSAGE:
                            AddMessage(message.Payload, message.Issuer);
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        #endregion Threads

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                byte[] buffer = Encoding.ASCII.GetBytes(ConvertMessage(Type.DISCONNECT, Settings.Name));

                await stream.WriteAsync(buffer, 0, buffer.Length);
                AddMessage("Disconnected...", "INFO");
                Dispatcher.Invoke(() => ToggleStartStopButton());
                tcpClient.Close();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                AddMessage(ex.ToString(), "ERROR");
            }
        }
    }
}
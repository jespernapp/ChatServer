using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections;
using System.Windows;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChatClient
{
    public partial class MainWindow : Window
    {
        private HubConnection _connection;

        public MainWindow()
        {
            InitializeComponent();
            //Enter för att skicka meddelande
            MessageInput.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    SendButton_Click(null, null);
                    e.Handled = true;
                }
            };
        }

        private bool _isConnected = false;
        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {

            if (_isConnected || (_connection != null && _connection.State == HubConnectionState.Connected))
            {
                ChatLog.Text += $"Already connected\n";
                return;
            }

            string username = UsernameBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(username) || username == "Namn")
            {
                ChatLog.Text += $"Please enter a username first\n";
                return;
            }
            await StartConnection(username);
        }

        private async Task StartConnection(string username)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7045/chathub") // Din SignalR-serveradress
                .WithAutomaticReconnect()
                .Build();

            _connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var time = DateTime.Now.ToString("HH:mm");
                    ChatList.Items.Add($"[{time}] {user}: {message}");
                    //autoscroll
                    ChatList.ScrollIntoView(ChatList.Items[ChatList.Items.Count - 1]);
                });
            });

            _connection.On<IEnumerable<string>>("UpdateUserList", (users) =>
            {
                Dispatcher.Invoke(() =>
                {
                    UserList.Items.Clear();
                    foreach (var user in users)
                    {
                        UserList.Items.Add(user);
                    }
                });
            });

            try
            {
                await _connection.StartAsync();
                await _connection.InvokeAsync("RegisterUser", username);
                _isConnected = true;
                ConnectButton.IsEnabled = false;
                ChatLog.Text += $"Connected as {username}";

            }
            catch (Exception ex)
            {
                ChatLog.Text += $"❌ Connection error: {ex.Message}\n";
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (_connection == null || _connection.State != HubConnectionState.Connected)
            {
                ChatLog.Text += "❌ Not connected to the server.\n";
                return;
            }

            string user = UsernameBox.Text.Trim();
            string message = MessageInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(message))
            {
                ChatLog.Text += "⚠️ Message cannot be empty.\n";
                return;
            }

            try
            {
                await _connection.InvokeAsync("SendMessage", user, message);
                MessageInput.Text = ""; // empty after send
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}");
                ChatLog.Text += $"❌ Error sending message: {ex.Message}\n";
            }
        }
    }
}
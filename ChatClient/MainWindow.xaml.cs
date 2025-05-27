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

            if (_isConnected == false)
            {
                ChatLog.Text += $"Already connected\n";
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
                ChatLog.Text += $"Connected as {username}";

            }
            catch (Exception ex)
            {
                ChatLog.Text += $"❌ Fel vid anslutning: {ex.Message}\n";
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (_connection.State == HubConnectionState.Connected)
            {
                string user = UsernameBox.Text;
                string message = MessageInput.Text;

                try
                {
                    await _connection.InvokeAsync("SendMessage", user, message);
                    MessageInput.Text = ""; // Töm efter skickat
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fel vid sändning: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Ej ansluten till servern.");
            }
        }
    }
}
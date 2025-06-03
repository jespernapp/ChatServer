using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections;
using System.Windows;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Media;

namespace ChatClient
{
    public partial class MainWindow : Window


    {
        private HubConnection _connection;

        //useristyping
        private DateTime _lastTypingTime = DateTime.MinValue;

        private bool _isTyping = false;

        private readonly TimeSpan TypingTimeout = TimeSpan.FromSeconds(3);

        private System.Windows.Threading.DispatcherTimer _typingTimer;

        public MainWindow()
        {
            InitializeComponent();
            DisconnectButton.IsEnabled = false;
            //autoclear UserNamebox
            UsernameBox.GotFocus += (s, e) =>
            {
                if (UsernameBox.Text == "Name")
                    UsernameBox.Text = "";
            };

            //Autoclear MessageInput
            MessageInput.GotFocus += (s, e) =>
            {
                if (MessageInput.Text == "Message")
                    MessageInput.Text = "";
            };


            //Enter för att skicka meddelande
            MessageInput.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    SendButton_Click(null, null);
                    e.Handled = true;
                }
            };
            //Enter för att ansluta
            UsernameBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    ConnectButton_Click(null, null);
                    e.Handled = true;
                }
            };

            MessageInput.TextChanged += async (s, e) =>
            {
                if (!_isTyping && _connection != null && _connection.State == HubConnectionState.Connected)
                {
                    _isTyping = true;
                    await _connection.InvokeAsync("UserTyping", UsernameBox.Text.Trim());
                }
                _lastTypingTime = DateTime.Now;
            };
        }

        private bool _isConnected = false;
        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {

            if (_isConnected || (_connection != null && _connection.State == HubConnectionState.Connected))
            {
                ChatList.Items.Add($"Already connected\n");
                return;
            }

            string username = UsernameBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(username) || username == "Namn")
            {
                ChatList.Items.Add($"Please enter a username first\n");
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

                    if (user != "Server")
                        PlayNotificationSound();
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

            _connection.On<string>("UserTyping", (typingUser) =>
            {
                Dispatcher.Invoke(() =>
                {
                    TypingIndicator.Text = $"{username} is typing...";
                });
            });

            _connection.On<string>("UserStoppedTyping", (typingUser) =>
            {
                Dispatcher.Invoke(() =>
                {
                    TypingIndicator.Text = "";
                });
            });

            try
            {
                await _connection.StartAsync();
                await _connection.InvokeAsync("RegisterUser", username);
                _isConnected = true;
                ConnectButton.IsEnabled = false;
                UsernameBox.IsEnabled = false;
                var time = DateTime.Now.ToString("HH:mm");
                ChatList.Items.Add($"[{time}] Connected as {username}");
                DisconnectButton.IsEnabled = true;
                MessageInput.IsEnabled = true;
                MessageInput.Focus();
                InitTypingTimer();
            }
            catch (Exception ex)
            {
                ChatList.Items.Add($"❌ Connection error: {ex.Message}\n");
            }
        }




        private void PlayNotificationSound()
        {
            try
            {
                var player = new System.Windows.Media.MediaPlayer();
                player.Open(new Uri("Sounds/Notification.mp3", UriKind.Relative));
                player.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error playing notification sound: {ex.Message}");
            }
        }

        private void InitTypingTimer()
        {
            _typingTimer = new System.Windows.Threading.DispatcherTimer();
            _typingTimer.Interval = TimeSpan.FromMilliseconds(500);
            _typingTimer.Tick += async (s, e) =>
            {
                if (_isTyping && DateTime.Now - _lastTypingTime > TypingTimeout)
                {
                    _isTyping = false;
                    await _connection.InvokeAsync("UserStoppedTyping", UsernameBox.Text.Trim());
                }
            };
            _typingTimer.Start();
        }


        private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_connection != null && _connection.State == HubConnectionState.Connected)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();//dispose old connection to prevent crash
                _isConnected = false;
                var time = DateTime.Now.ToString("HH:mm");
                ChatList.Items.Add($"[{time}] Disconnected from server.");

                DisconnectButton.IsEnabled = false;
                ConnectButton.IsEnabled = true;
                UsernameBox.IsEnabled = true;
                MessageInput.IsEnabled = false;

                //stop typing-timer
                _typingTimer?.Stop();
                _typingTimer = null;
                TypingIndicator.Text = ""; // clear typing indicator
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (_connection == null || _connection.State != HubConnectionState.Connected)
            {
                ChatList.Items.Add("❌ Not connected to the server.\n");
                return;
            }

            string user = UsernameBox.Text.Trim();
            string message = MessageInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(message))
            {
                ChatList.Items.Add("⚠️ Message cannot be empty.\n");
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
                ChatList.Items.Add($"❌ Error sending message: {ex.Message}\n");
            }
        }


    }
}
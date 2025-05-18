using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Windows;

namespace ChatClient
{
    public partial class MainWindow : Window
    {
        private HubConnection _connection;

        public MainWindow()
        {
            InitializeComponent();
            StartConnection();
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

        private async void StartConnection()
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
                    ChatList.Items.Add($"{user}: {message}");
                    //autoscroll
                    ChatList.ScrollIntoView(ChatList.Items[ChatList.Items.Count - 1]);
                });
            });

            try
            {
                await _connection.StartAsync();
                ChatLog.Text += "🔌 Ansluten till servern\n";
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
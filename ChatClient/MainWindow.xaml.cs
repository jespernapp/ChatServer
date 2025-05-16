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
                    ChatLog.Text += $"{user}: {message}{Environment.NewLine}";
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
            var user = UsernameBox.Text;
            var message = MessageBox.Text;

            if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(message))
            {
                await _connection.InvokeAsync("SendMessage", user, message);
                MessageBox.Clear();
            }
        }
    }
}
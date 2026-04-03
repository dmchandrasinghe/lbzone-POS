using System.Net.Http;
using System.Windows;
using LasanthaPOS.Desktop.Services;

namespace LasanthaPOS.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly ApiService _api;

    public LoginWindow()
    {
        InitializeComponent();
        _api = new ApiService();
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Visibility = Visibility.Collapsed;
        var username = TxtUsername.Text.Trim();
        var password = TxtPassword.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            TxtError.Text = "Please enter username and password.";
            TxtError.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            var success = await _api.LoginAsync(username, password);
            if (success)
            {
                var main = new MainWindow(_api);
                main.Show();
                Close();
            }
            else
            {
                TxtError.Text = "Invalid username or password.";
                TxtError.Visibility = Visibility.Visible;
            }
        }
        catch (HttpRequestException)
        {
            TxtError.Text = "Cannot connect to API (http://localhost:5100). Ensure Docker is running.";
            TxtError.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            TxtError.Text = $"Error: {ex.Message}";
            TxtError.Visibility = Visibility.Visible;
        }
    }
}

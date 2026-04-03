using System.Windows;
using LasanthaPOS.Desktop.Models;
using LasanthaPOS.Desktop.Services;

namespace LasanthaPOS.Desktop.Views;

public partial class CustomerDialog : Window
{
    private readonly ApiService _api;
    private readonly Customer? _existing;

    public CustomerDialog(ApiService api, Customer? existing)
    {
        InitializeComponent();
        _api = api;
        _existing = existing;
        if (existing is not null)
        {
            TxtTitle.Text = "Edit Customer";
            TxtName.Text = existing.Name;
            TxtPhone.Text = existing.Phone;
            TxtEmail.Text = existing.Email;
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Visibility = Visibility.Collapsed;
        if (string.IsNullOrWhiteSpace(TxtName.Text) || string.IsNullOrWhiteSpace(TxtPhone.Text))
        {
            TxtError.Text = "Name and Phone are required.";
            TxtError.Visibility = Visibility.Visible;
            return;
        }

        var customer = new
        {
            Id = _existing?.Id ?? 0,
            Name = TxtName.Text.Trim(),
            Phone = TxtPhone.Text.Trim(),
            Email = TxtEmail.Text.Trim(),
            LoyaltyCardId = _existing?.LoyaltyCardId ?? "",
            LoyaltyPoints = _existing?.LoyaltyPoints ?? 0
        };

        if (_existing is null)
            await _api.PostAsync("customers", customer);
        else
            await _api.PutAsync($"customers/{_existing.Id}", customer);

        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

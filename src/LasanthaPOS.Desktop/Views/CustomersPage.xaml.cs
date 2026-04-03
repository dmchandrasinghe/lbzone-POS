using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LasanthaPOS.Desktop.Models;
using LasanthaPOS.Desktop.Services;

namespace LasanthaPOS.Desktop.Views;

public partial class CustomersPage : Page
{
    private readonly ApiService _api;

    public CustomersPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
        _ = LoadAll();
    }

    private async Task LoadAll()
    {
        var customers = await _api.GetAsync<Customer>("customers");
        DgCustomers.ItemsSource = customers;
    }

    private async void BtnLoadAll_Click(object sender, RoutedEventArgs e) => await LoadAll();

    private async void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        var q = TxtSearch.Text.Trim();
        if (string.IsNullOrWhiteSpace(q)) { await LoadAll(); return; }
        var results = await _api.GetAsync<Customer>($"customers/search?q={Uri.EscapeDataString(q)}");
        DgCustomers.ItemsSource = results;
    }

    private void TxtSearch_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) BtnSearch_Click(sender, new RoutedEventArgs());
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CustomerDialog(_api, null);
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true) _ = LoadAll();
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is Customer customer)
        {
            var dialog = new CustomerDialog(_api, customer);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true) _ = LoadAll();
        }
    }
}

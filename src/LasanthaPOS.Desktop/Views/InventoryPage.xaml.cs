using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LasanthaPOS.Desktop.Models;
using LasanthaPOS.Desktop.Services;

namespace LasanthaPOS.Desktop.Views;

public partial class InventoryPage : Page
{
    private readonly ApiService _api;
    private List<Product> _products = new();

    public InventoryPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
        _ = LoadAll();
    }

    private async Task LoadAll()
    {
        _products = await _api.GetAsync<Product>("products");
        DgProducts.ItemsSource = _products;
        TxtStatus.Text = $"{_products.Count} products loaded.";
    }

    private async void BtnLoadAll_Click(object sender, RoutedEventArgs e) => await LoadAll();

    private async void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        var q = TxtSearch.Text.Trim();
        if (string.IsNullOrWhiteSpace(q)) { await LoadAll(); return; }
        var results = await _api.GetAsync<Product>($"products/search?q={Uri.EscapeDataString(q)}");
        DgProducts.ItemsSource = results;
        TxtStatus.Text = $"{results.Count} results for '{q}'.";
    }

    private async void TxtSearch_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) BtnSearch_Click(sender, new RoutedEventArgs());
    }

    private async void BtnLowStock_Click(object sender, RoutedEventArgs e)
    {
        var products = await _api.GetAsync<Product>("products/low-stock");
        DgProducts.ItemsSource = products;
        TxtStatus.Text = $"{products.Count} low stock items.";
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ProductDialog(_api, null);
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true) _ = LoadAll();
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is Product product)
        {
            var dialog = new ProductDialog(_api, product);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true) _ = LoadAll();
        }
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is Product product)
        {
            var confirm = MessageBox.Show($"Delete '{product.Name}'?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm == MessageBoxResult.Yes)
            {
                await _api.DeleteAsync($"products/{product.Id}");
                await LoadAll();
            }
        }
    }
}

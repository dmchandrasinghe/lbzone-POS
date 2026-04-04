using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LasanthaPOS.Desktop.Models;
using LasanthaPOS.Desktop.Services;
using Microsoft.Win32;

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

    private async void BtnImportCsv_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select CSV file to import",
            Filter = "CSV Files (*.csv)|*.csv",
            DefaultExt = ".csv"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            using var stream = File.OpenRead(dlg.FileName);
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            content.Add(fileContent, "file", Path.GetFileName(dlg.FileName));

            var response = await _api.PostMultipartAsync("products/import-csv", content);
            var json = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show($"Import completed.\n{json}", "Import Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadAll();
            }
            else
            {
                MessageBox.Show($"Import failed:\n{json}", "Import Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title = "Export Inventory as CSV",
            FileName = $"inventory_{DateTime.Today:yyyyMMdd}.csv",
            Filter = "CSV Files (*.csv)|*.csv",
            DefaultExt = ".csv"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var bytes = await _api.GetBytesAsync("products/export-csv");
            File.WriteAllBytes(dlg.FileName, bytes);
            TxtStatus.Text = $"Exported to: {dlg.FileName}";
            MessageBox.Show($"Inventory exported successfully.\n\n{dlg.FileName}",
                "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnDownloadTemplate_Click(object sender, RoutedEventArgs e)    {
        var dlg = new SaveFileDialog
        {
            Title = "Save CSV Template",
            FileName = "inventory_template.csv",
            Filter = "CSV Files (*.csv)|*.csv",
            DefaultExt = ".csv"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var bytes = await _api.GetBytesAsync("products/csv-template");
            File.WriteAllBytes(dlg.FileName, bytes);
            MessageBox.Show($"Template saved to:\n{dlg.FileName}\n\nFill in the rows and use Import CSV to add products.",
                "Template Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCategories_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CategoriesSuppliersDialog(_api, mode: "Categories");
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true) _ = LoadAll();
    }

    private void BtnSuppliers_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CategoriesSuppliersDialog(_api, mode: "Suppliers");
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true) _ = LoadAll();
    }
}

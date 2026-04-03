using System.Windows;
using LasanthaPOS.Desktop.Models;
using LasanthaPOS.Desktop.Services;

namespace LasanthaPOS.Desktop.Views;

public partial class ProductDialog : Window
{
    private readonly ApiService _api;
    private readonly Product? _existing;
    private List<Category> _categories = new();
    private List<Supplier> _suppliers = new();

    public ProductDialog(ApiService api, Product? existing)
    {
        InitializeComponent();
        _api = api;
        _existing = existing;
        if (existing is not null) TxtTitle.Text = "Edit Product";
        _ = LoadLookups();
    }

    private async Task LoadLookups()
    {
        _categories = await _api.GetAsync<Category>("categories");
        _suppliers = await _api.GetAsync<Supplier>("suppliers");
        CboCategory.ItemsSource = _categories;
        CboSupplier.ItemsSource = _suppliers;

        if (_existing is not null)
        {
            TxtItemCode.Text = _existing.ItemCode;
            TxtName.Text = _existing.Name;
            TxtBuyingPrice.Text = _existing.BuyingPrice.ToString("F2");
            TxtSellingPrice.Text = _existing.SellingPrice.ToString("F2");
            TxtQuantity.Text = _existing.Quantity.ToString();
            TxtReorder.Text = _existing.ReorderThreshold.ToString();
            DtPurchase.SelectedDate = _existing.PurchaseDate;
            DtExpiration.SelectedDate = _existing.ExpirationDate;
            TxtWarranty.Text = _existing.WarrantyMonths?.ToString() ?? "";
            CboCategory.SelectedItem = _categories.FirstOrDefault(c => c.Id == _existing.CategoryId);
            CboSupplier.SelectedItem = _suppliers.FirstOrDefault(s => s.Id == _existing.SupplierId);
        }
        else
        {
            DtPurchase.SelectedDate = DateTime.Today;
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(TxtItemCode.Text) || string.IsNullOrWhiteSpace(TxtName.Text)
            || !decimal.TryParse(TxtBuyingPrice.Text, out var buy) || buy < 0
            || !decimal.TryParse(TxtSellingPrice.Text, out var sell) || sell < 0
            || !int.TryParse(TxtQuantity.Text, out var qty) || qty < 0)
        {
            TxtError.Text = "Please fill all required fields with valid values.";
            TxtError.Visibility = Visibility.Visible;
            return;
        }

        int? warranty = int.TryParse(TxtWarranty.Text, out var w) ? w : null;
        var category = CboCategory.SelectedItem as Category;
        var supplier = CboSupplier.SelectedItem as Supplier;

        var product = new
        {
            Id = _existing?.Id ?? 0,
            ItemCode = TxtItemCode.Text.Trim(),
            Name = TxtName.Text.Trim(),
            CategoryId = category?.Id ?? 0,
            SupplierId = supplier?.Id ?? 0,
            BuyingPrice = buy,
            SellingPrice = sell,
            Quantity = qty,
            ReorderThreshold = int.TryParse(TxtReorder.Text, out var r) ? r : 5,
            PurchaseDate = DtPurchase.SelectedDate ?? DateTime.Today,
            ExpirationDate = DtExpiration.SelectedDate,
            WarrantyMonths = warranty
        };

        try
        {
            if (_existing is null)
                await _api.PostAsync("products", product);
            else
                await _api.PutAsync($"products/{_existing.Id}", product);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            TxtError.Text = $"Error: {ex.Message}";
            TxtError.Visibility = Visibility.Visible;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

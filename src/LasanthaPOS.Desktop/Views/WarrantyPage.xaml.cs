using System.Windows.Controls;
using LasanthaPOS.Desktop.Models;
using LasanthaPOS.Desktop.Services;

namespace LasanthaPOS.Desktop.Views;

public partial class WarrantyPage : Page
{
    private readonly ApiService _api;

    public WarrantyPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
        _ = Load();
    }

    private async Task Load()
    {
        var warranties = await _api.GetAsync<Warranty>("warranties");
        DgWarranties.ItemsSource = warranties;
    }
}

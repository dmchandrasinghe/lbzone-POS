using System.Windows;
using System.Windows.Controls;
using LasanthaPOS.Desktop.Models;
using LasanthaPOS.Desktop.Services;

namespace LasanthaPOS.Desktop.Views;

public partial class CategoriesSuppliersDialog : Window
{
    private readonly ApiService _api;
    private readonly string _mode;
    private bool _changed = false;

    public CategoriesSuppliersDialog(ApiService api, string mode)
    {
        InitializeComponent();
        _api = api;
        _mode = mode;

        Title = $"Manage {mode}";
        TxtTitle.Text = $"Manage {mode}";

        if (mode == "Suppliers")
        {
            ColPhone.Visibility = Visibility.Visible;
            ColEmail.Visibility = Visibility.Visible;
            PnlPhone.Visibility = Visibility.Visible;
            PnlEmail.Visibility = Visibility.Visible;
        }

        _ = LoadData();
    }

    private async Task LoadData()
    {
        if (_mode == "Categories")
            Dg.ItemsSource = await _api.GetAsync<Category>("categories");
        else
            Dg.ItemsSource = await _api.GetAsync<Supplier>("suppliers");
    }

    private async void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        TxtAddError.Visibility = Visibility.Collapsed;
        var name = TxtNewName.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            TxtAddError.Text = "Name is required.";
            TxtAddError.Visibility = Visibility.Visible;
            return;
        }

        if (_mode == "Categories")
        {
            await _api.PostAsync("categories", new { Name = name });
        }
        else
        {
            await _api.PostAsync("suppliers", new
            {
                Name = name,
                ContactPhone = TxtNewPhone.Text.Trim(),
                Email = TxtNewEmail.Text.Trim()
            });
        }

        TxtNewName.Clear();
        TxtNewPhone.Clear();
        TxtNewEmail.Clear();
        _changed = true;
        await LoadData();
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;

        if (_mode == "Categories" && btn.Tag is Category cat)
        {
            var confirm = MessageBox.Show(
                $"Delete category '{cat.Name}'?\nProducts in this category will lose their category link.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            await _api.DeleteAsync($"categories/{cat.Id}");
            _changed = true;
            await LoadData();
        }
        else if (_mode == "Suppliers" && btn.Tag is Supplier sup)
        {
            var confirm = MessageBox.Show(
                $"Delete supplier '{sup.Name}'?\nProducts from this supplier will lose their supplier link.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            await _api.DeleteAsync($"suppliers/{sup.Id}");
            _changed = true;
            await LoadData();
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = _changed;
        Close();
    }
}

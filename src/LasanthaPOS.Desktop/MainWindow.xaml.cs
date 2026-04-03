using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LasanthaPOS.Desktop.Services;
using LasanthaPOS.Desktop.Views;

namespace LasanthaPOS.Desktop;

public partial class MainWindow : Window
{
    private readonly ApiService _api;

    public MainWindow(ApiService api)
    {
        InitializeComponent();
        _api = api;
        TxtCurrentUser.Text = $"{_api.CurrentUser}\n{_api.CurrentRole}";
        MainFrame.Navigate(new PosPage(_api));
    }

    private void Nav_POS(object sender, RoutedEventArgs e) => MainFrame.Navigate(new PosPage(_api));
    private void Nav_Inventory(object sender, RoutedEventArgs e) => MainFrame.Navigate(new InventoryPage(_api));
    private void Nav_Customers(object sender, RoutedEventArgs e) => MainFrame.Navigate(new CustomersPage(_api));
    private void Nav_Warranty(object sender, RoutedEventArgs e) => MainFrame.Navigate(new WarrantyPage(_api));
    private void Nav_Report(object sender, RoutedEventArgs e) => MainFrame.Navigate(new ReportPage(_api));

    private void Nav_Logout(object sender, RoutedEventArgs e)
    {
        var login = new Views.LoginWindow();
        login.Show();
        Close();
    }
}

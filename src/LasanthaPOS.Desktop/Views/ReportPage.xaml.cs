using System.Windows;
using System.Windows.Controls;
using LasanthaPOS.Desktop.Models;
using LasanthaPOS.Desktop.Services;

namespace LasanthaPOS.Desktop.Views;

public partial class ReportPage : Page
{
    private readonly ApiService _api;

    public ReportPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
        DtReport.SelectedDate = DateTime.Today;
        _ = LoadReport(DateTime.Today);
    }

    private async void BtnLoad_Click(object sender, RoutedEventArgs e)
    {
        if (DtReport.SelectedDate.HasValue)
            await LoadReport(DtReport.SelectedDate.Value);
    }

    private async Task LoadReport(DateTime date)
    {
        try
        {
            var summary = await _api.GetSingleAsync<DailySummary>($"sales/daily-summary?date={date:yyyy-MM-dd}");
            if (summary is null) return;
            TxtTotalSales.Text = summary.TotalSales.ToString();
            TxtRevenue.Text = $"${summary.TotalRevenue:F2}";
            TxtDiscounts.Text = $"${summary.TotalDiscount:F2}";
            TxtCashSales.Text = summary.CashSales.ToString();
            TxtCardSales.Text = summary.CardSales.ToString();
            TxtStatus.Text = $"Report for {date:dd MMMM yyyy}";
        }
        catch
        {
            TxtStatus.Text = "Failed to load report. Check server connection.";
        }
    }
}

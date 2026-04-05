using System.Net.Http;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using LasanthaPOS.Desktop.Models;
using LasanthaPOS.Desktop.Services;

namespace LasanthaPOS.Desktop.Views;

public partial class PosPage : Page
{
    private readonly ApiService _api;
    private readonly List<CartItem> _cart = new();
    private List<Product> _searchResults = new();
    private List<Customer> _customerResults = new();
    private Product? _selectedProduct;
    private Customer? _selectedCustomer;

    // Last completed sale — used for reprint
    private List<CartItem>? _lastCart;
    private decimal _lastTotal;
    private string _lastPayment = "";
    private decimal _lastPaid;
    private Customer? _lastCustomer;

    public PosPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void BtnSearch_Click(object sender, RoutedEventArgs e) => await SearchProducts();
    private async void TxtSearch_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await SearchProducts();
    }

    private async Task SearchProducts()
    {
        var q = TxtSearch.Text.Trim();
        if (string.IsNullOrWhiteSpace(q)) return;
        _searchResults = await _api.GetAsync<Product>($"products/search?q={Uri.EscapeDataString(q)}");
        LstProducts.ItemsSource = _searchResults;
    }

    private void LstProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedProduct = LstProducts.SelectedItem as Product;
        TxtSelectedProduct.Text = _selectedProduct?.ToString() ?? "No product selected";
    }

    private void BtnAddToCart_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProduct is null) { ShowStatus("Select a product first.", false); return; }
        if (!int.TryParse(TxtQty.Text, out var qty) || qty <= 0) { ShowStatus("Invalid quantity.", false); return; }
        if (!decimal.TryParse(TxtItemDiscount.Text, out var disc) || disc < 0) disc = 0;

        var existing = _cart.FirstOrDefault(c => c.ProductId == _selectedProduct.Id);
        if (existing is not null)
            existing.Quantity += qty;
        else
            _cart.Add(new CartItem
            {
                ProductId = _selectedProduct.Id,
                ItemCode = _selectedProduct.ItemCode,
                ProductName = _selectedProduct.Name,
                Quantity = qty,
                UnitPrice = _selectedProduct.SellingPrice,
                DiscountAmount = disc,
                PurchaseDate = _selectedProduct.PurchaseDate,
                ExpirationDate = _selectedProduct.ExpirationDate
            });

        RefreshCart();
    }

    private void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (LstCart.SelectedItem is CartItem item)
        {
            _cart.Remove(item);
            RefreshCart();
        }
    }

    private void BtnClearCart_Click(object sender, RoutedEventArgs e)
    {
        _cart.Clear();
        RefreshCart();
    }

    private void RefreshCart()
    {
        LstCart.ItemsSource = null;
        LstCart.ItemsSource = _cart;
        UpdateTotals();
    }

    private void UpdateTotals()
    {
        if (TxtSubtotal == null || TxtBillDiscount == null ||
            TxtPointsRedeem == null || TxtTotal == null) return;
        var subtotal = _cart.Sum(c => c.LineTotal);
        decimal.TryParse(TxtBillDiscount.Text, out var billDisc);
        decimal.TryParse(TxtPointsRedeem.Text, out var points);
        var total = Math.Max(0, subtotal - billDisc - (points / 100m));
        TxtSubtotal.Text = $"${subtotal:F2}";
        TxtTotal.Text = $"${total:F2}";
        UpdateChange();
    }

    private void UpdateChange()
    {
        if (TxtTotal == null || TxtChange == null || TxtAmountPaid == null) return;
        decimal.TryParse(TxtTotal.Text.TrimStart('$'), out var total);
        decimal.TryParse(TxtAmountPaid.Text, out var paid);
        TxtChange.Text = $"${Math.Max(0, paid - total):F2}";
    }

    private void TxtBillDiscount_TextChanged(object sender, TextChangedEventArgs e) => UpdateTotals();
    private void TxtPointsRedeem_TextChanged(object sender, TextChangedEventArgs e) => UpdateTotals();
    private void TxtAmountPaid_TextChanged(object sender, TextChangedEventArgs e) => UpdateChange();

    private async void BtnFindCustomer_Click(object sender, RoutedEventArgs e)
    {
        var q = TxtCustomerSearch.Text.Trim();
        if (string.IsNullOrWhiteSpace(q)) return;
        _customerResults = await _api.GetAsync<Customer>($"customers/search?q={Uri.EscapeDataString(q)}");
        LstCustomers.ItemsSource = _customerResults;
    }

    private void LstCustomers_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedCustomer = LstCustomers.SelectedItem as Customer;
        TxtSelectedCustomer.Text = _selectedCustomer is not null
            ? $"Selected: {_selectedCustomer.Name} | Points: {_selectedCustomer.LoyaltyPoints}"
            : "No customer selected";
    }

    private async void BtnCompleteSale_Click(object sender, RoutedEventArgs e)
    {
        if (_cart.Count == 0) { ShowStatus("Cart is empty.", false); return; }
        decimal.TryParse(TxtBillDiscount.Text, out var billDisc);
        decimal.TryParse(TxtPointsRedeem.Text, out var pointsRedeem);
        decimal.TryParse(TxtAmountPaid.Text, out var amountPaid);
        decimal.TryParse(TxtTotal.Text.TrimStart('$'), out var total);

        var payment = (CboPayment.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Cash";

        var sale = new
        {
            customerId = _selectedCustomer?.Id,
            subTotal = _cart.Sum(c => c.LineTotal),
            discountAmount = billDisc,
            taxAmount = 0m,
            total,
            paymentMethod = payment,
            amountPaid,
            change = Math.Max(0, amountPaid - total),
            pointsRedeemed = (int)pointsRedeem,
            createdBy = _api.CurrentUser ?? "Unknown",
            items = _cart.Select(c => new
            {
                productId = c.ProductId,
                itemCode = c.ItemCode,
                productName = c.ProductName,
                quantity = c.Quantity,
                unitPrice = c.UnitPrice,
                discountAmount = c.DiscountAmount,
                lineTotal = c.LineTotal
            }).ToList()
        };

        try
        {
            var response = await _api.PostAsync("sales", sale);
            if (response.IsSuccessStatusCode)
            {
                // Save for reprint before clearing cart
                _lastCart = _cart.ToList();
                _lastTotal = total;
                _lastPayment = payment;
                _lastPaid = amountPaid;
                _lastCustomer = _selectedCustomer;

                PrintReceipt(total, payment, amountPaid);
                ShowStatus("Sale completed successfully!", true);
                _cart.Clear();
                _selectedCustomer = null;
                TxtSelectedCustomer.Text = "No customer selected";
                RefreshCart();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                ShowStatus($"Error: {error}", false);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Connection error: {ex.Message}", false);
        }
    }

    private void PrintReceipt(decimal total, string payment, decimal paid,
        List<CartItem>? cartOverride = null, Customer? customerOverride = null)
    {
        var printItems = cartOverride ?? _cart;
        var customer = customerOverride ?? _selectedCustomer;        var printDialog = new PrintDialog();

        // Use default printer without showing dialog — set to true to let user pick
        // printDialog.ShowDialog();

        // Build a FlowDocument receipt
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Courier New"),
            FontSize = 11,
            PageWidth = 280,        // ~80mm receipt paper width in pixels
            PagePadding = new Thickness(10),
            ColumnGap = 0,
            ColumnWidth = 260
        };

        void AddCentered(string text, double size = 11, bool bold = false)
        {
            var p = new Paragraph(new Run(text))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 1, 0, 1),
                FontSize = size,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal
            };
            doc.Blocks.Add(p);
        }

        void AddLine(string text)
        {
            doc.Blocks.Add(new Paragraph(new Run(text))
            {
                Margin = new Thickness(0, 0, 0, 0),
                FontSize = 10
            });
        }

        void AddDivider(char ch = '-')
        {
            AddLine(new string(ch, 38));
        }

        void AddItemRow(string name, int qty, decimal unitPrice, decimal lineTotal)
        {
            var table = new Table { FontSize = 10, Margin = new Thickness(0) };
            table.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            var rg = new TableRowGroup();
            var row = new TableRow();
            var nameCell = new TableCell(new Paragraph(new Run(name.Length > 18 ? name[..18] : name)));
            var qtyCell = new TableCell(new Paragraph(new Run($"{qty}x${unitPrice:F2}"))) { TextAlignment = TextAlignment.Right };
            var totalCell = new TableCell(new Paragraph(new Run($"${lineTotal:F2}"))) { TextAlignment = TextAlignment.Right };
            row.Cells.Add(nameCell);
            row.Cells.Add(qtyCell);
            row.Cells.Add(totalCell);
            rg.Rows.Add(row);
            table.RowGroups.Add(rg);
            doc.Blocks.Add(table);
        }

        void AddTotalRow(string label, string value, bool bold = false)
        {
            var table = new Table { FontSize = bold ? 12 : 10, Margin = new Thickness(0) };
            table.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            var rg = new TableRowGroup();
            var row = new TableRow();
            var labelCell = new TableCell(new Paragraph(new Run(label)))
            {
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal
            };
            var valueCell = new TableCell(new Paragraph(new Run(value)))
            {
                TextAlignment = TextAlignment.Right,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal
            };
            row.Cells.Add(labelCell);
            row.Cells.Add(valueCell);
            rg.Rows.Add(row);
            table.RowGroups.Add(rg);
            doc.Blocks.Add(table);
        }

        // Header
        AddCentered("LASANTHA ELECTRONICS", 13, bold: true);
        AddCentered("Point of Sale", 10);
        AddCentered($"Date: {DateTime.Now:dd/MM/yyyy  HH:mm}", 10);
        if (customer is not null)
            AddCentered($"Customer: {customer.Name}", 10);
        AddDivider('=');

        // Items
        AddLine($" {"Item",-18} {"Qty x Price",12} {"Total",7}");
        AddDivider();
        foreach (var item in printItems)
        {
            AddItemRow(item.ProductName, item.Quantity, item.UnitPrice, item.LineTotal);
            AddLine($"   Buying Date : {item.PurchaseDate:dd/MM/yyyy}");
            AddLine($"   Expire Date : {(item.ExpirationDate.HasValue ? item.ExpirationDate.Value.ToString("dd/MM/yyyy") : "N/A")}");
        }

        AddDivider('=');

        // Totals
        var subtotal = printItems.Sum(c => c.LineTotal);
        AddTotalRow("  Subtotal:", $"${subtotal:F2}");
        if (paid - total > 0.009m)
            AddTotalRow("  Discount:", $"-${subtotal - total:F2}");
        AddTotalRow("  TOTAL:", $"${total:F2}", bold: true);
        AddDivider();
        AddTotalRow($"  Payment ({payment}):", $"${paid:F2}");
        AddTotalRow("  Change:", $"${Math.Max(0, paid - total):F2}");

        AddDivider('=');
        AddCentered("Thank you for shopping!", 10);
        AddCentered("Lasantha Electronics", 10);
        AddCentered("", 6);  // spacing at bottom

        // Print
        var docPaginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
        docPaginator.PageSize = new System.Windows.Size(printDialog.PrintableAreaWidth > 0
            ? printDialog.PrintableAreaWidth : 280, 1122);

        try
        {
            printDialog.PrintDocument(docPaginator, $"Receipt - {DateTime.Now:yyyyMMdd-HHmm}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Print failed: {ex.Message}\n\nEnsure a printer is connected and set as default.",
                "Print Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnReprint_Click(object sender, RoutedEventArgs e)
    {
        if (_lastCart is null || _lastCart.Count == 0)
        {
            ShowStatus("No previous sale to reprint.", false);
            return;
        }
        PrintReceipt(_lastTotal, _lastPayment, _lastPaid,
            cartOverride: _lastCart, customerOverride: _lastCustomer);
    }

    private void ShowStatus(string msg, bool success)
    {
        TxtStatus.Text = msg;
        TxtStatus.Foreground = success
            ? System.Windows.Media.Brushes.Green
            : System.Windows.Media.Brushes.Red;
    }
}

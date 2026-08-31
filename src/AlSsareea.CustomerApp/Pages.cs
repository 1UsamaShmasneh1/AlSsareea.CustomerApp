using AlSsareea.CustomerApp.Core;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace AlSsareea.CustomerApp;

public abstract class RemotePage<TViewModel> : ContentPage where TViewModel : RemoteViewModel
{
    protected RemotePage(string title)
    {
        Title = title;
        ViewModel = AppServices.Get<TViewModel>();
        BindingContext = ViewModel;
    }
    protected TViewModel ViewModel { get; }
    protected void AddState(VerticalStackLayout layout)
    {
        var progress = new ActivityIndicator { HorizontalOptions = LayoutOptions.Center };
        progress.SetBinding(ActivityIndicator.IsRunningProperty, nameof(RemoteViewModel.IsBusy));
        progress.SetBinding(IsVisibleProperty, nameof(RemoteViewModel.IsBusy));
        var error = new Label { TextColor = Colors.Firebrick };
        error.SetBinding(Label.TextProperty, nameof(RemoteViewModel.ErrorMessage));
        error.SetBinding(IsVisibleProperty, nameof(RemoteViewModel.HasError));
        layout.Add(progress); layout.Add(error);
    }
    public static Button Action(string text, Func<Task> execute)
    {
        var button = new Button { Text = text, MinimumHeightRequest = 48 };
        button.Clicked += async (_, _) => await execute();
        return button;
    }
}

public sealed class SplashPage : ContentPage
{
    private readonly SplashViewModel viewModel = AppServices.Get<SplashViewModel>();
    private bool started;
    public SplashPage()
    {
        Shell.SetNavBarIsVisible(this, false);
        Content = new Grid { Children = { new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center, Spacing = 16, Children = { new Label { Text = "AlSsareea", FontSize = 36, FontAttributes = FontAttributes.Bold }, new ActivityIndicator { IsRunning = true } } } } };
    }
    protected override async void OnAppearing() { base.OnAppearing(); if (started) return; started = true; await viewModel.StartAsync(default); }
}

public sealed class OnboardingPage : ContentPage
{
    private readonly OnboardingViewModel viewModel = AppServices.Get<OnboardingViewModel>();
    public OnboardingPage()
    {
        Title = "Welcome"; BindingContext = viewModel;
        var language = new Picker { Title = "Language", ItemsSource = new[] { "en", "ar", "he" }, SelectedItem = viewModel.SelectedLanguage, MinimumHeightRequest = 48 };
        language.SelectedIndexChanged += (_, _) => { if (language.SelectedItem is string value) { viewModel.SelectedLanguage = value; FlowDirection = value is "ar" or "he" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight; } };
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 28, Spacing = 20, Children = { new Label { Text = "Fast local delivery, from nearby merchants.", FontSize = 28, FontAttributes = FontAttributes.Bold }, new Label { Text = "Browse merchants, order with backend-authoritative totals, and track delivery live." }, language, RemotePage<LoginViewModel>.Action("Continue", viewModel.CompleteAsync) } } };
    }
}

public sealed class LoginPage : RemotePage<LoginViewModel>
{
    public LoginPage() : base("Login")
    {
        var identifier = new Entry { Placeholder = "Email or phone", Keyboard = Keyboard.Email, MinimumHeightRequest = 48 };
        identifier.SetBinding(Entry.TextProperty, nameof(LoginViewModel.Identifier));
        var password = new Entry { Placeholder = "Password", IsPassword = true, MinimumHeightRequest = 48 };
        password.SetBinding(Entry.TextProperty, nameof(LoginViewModel.Password));
        var otp = new Entry { Placeholder = "OTP code", Keyboard = Keyboard.Numeric, MinimumHeightRequest = 48 };
        otp.SetBinding(Entry.TextProperty, nameof(LoginViewModel.OtpCode));
        var layout = new VerticalStackLayout { Padding = 24, Spacing = 14, Children = { new Label { Text = "Welcome back", FontSize = 30, FontAttributes = FontAttributes.Bold }, identifier, password, Action("Login", ViewModel.LoginAsync), Action("Request OTP", ViewModel.RequestOtpAsync), otp, Action("Verify OTP", ViewModel.VerifyOtpAsync) } };
        AddState(layout); Content = new ScrollView { Content = layout };
    }
}

public class MainPage : MerchantListPage
{
    public MainPage() : base("Home") { }
}

public sealed class SearchPage : MerchantListPage
{
    public SearchPage() : base("Search merchants") { }
}

public class MerchantListPage : RemotePage<MerchantDiscoveryViewModel>
{
    private readonly CollectionView list;
    protected MerchantListPage(string title) : base(title)
    {
        var search = new SearchBar { Placeholder = "Search merchants", MinimumHeightRequest = 48 };
        search.SetBinding(SearchBar.TextProperty, nameof(MerchantDiscoveryViewModel.Query));
        search.TextChanged += async (_, _) => await ViewModel.SearchDebouncedAsync();
        var open = new HorizontalStackLayout { Spacing = 8, Children = { new Label { Text = "Open now", VerticalTextAlignment = TextAlignment.Center }, new Switch() } };
        ((Switch)open.Children[1]).SetBinding(Switch.IsToggledProperty, nameof(MerchantDiscoveryViewModel.OpenNow));
        ((Switch)open.Children[1]).Toggled += async (_, _) => await ViewModel.LoadAsync(true);
        list = new CollectionView { SelectionMode = SelectionMode.Single, EmptyView = new Label { Text = "No merchants found.", Margin = 20 } };
        list.SetBinding(ItemsView.ItemsSourceProperty, nameof(MerchantDiscoveryViewModel.Items));
        list.ItemTemplate = new DataTemplate(() =>
        {
            var name = new Label { FontSize = 19, FontAttributes = FontAttributes.Bold }; name.SetBinding(Label.TextProperty, nameof(CustomerMerchantSummary.DisplayName));
            var description = new Label { MaxLines = 2 }; description.SetBinding(Label.TextProperty, nameof(CustomerMerchantSummary.Description));
            var status = new Label(); status.SetBinding(Label.TextProperty, nameof(CustomerMerchantSummary.IsOpen), stringFormat: "Open: {0}");
            return new Border { Padding = 16, Margin = new Thickness(0, 5), StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 }, Content = new VerticalStackLayout { Spacing = 4, Children = { name, description, status } } };
        });
        list.SelectionChanged += async (_, args) => { if (args.CurrentSelection.FirstOrDefault() is CustomerMerchantSummary merchant) await ViewModel.OpenAsync(merchant); list.SelectedItem = null; };
        list.RemainingItemsThreshold = 3; list.RemainingItemsThresholdReached += async (_, _) => await ViewModel.LoadMoreAsync();
        var refresh = new RefreshView { Content = list }; refresh.SetBinding(RefreshView.IsRefreshingProperty, nameof(RemoteViewModel.IsBusy)); refresh.Refreshing += async (_, _) => await ViewModel.LoadAsync(true);
        var layout = new VerticalStackLayout { Padding = 16, Spacing = 8, Children = { search, open } }; AddState(layout); layout.Add(refresh); Content = layout;
    }
    protected override async void OnAppearing() { base.OnAppearing(); if (ViewModel.State == RemoteStateKind.Initial) await ViewModel.LoadAsync(); }
}

public sealed class MerchantDetailsPage : RemotePage<MerchantDetailsViewModel>, IQueryAttributable
{
    private readonly VerticalStackLayout branches = new() { Spacing = 8 };
    public MerchantDetailsPage() : base("Merchant")
    {
        var name = new Label { FontSize = 28, FontAttributes = FontAttributes.Bold }; name.SetBinding(Label.TextProperty, "Merchant.DisplayName");
        var description = new Label(); description.SetBinding(Label.TextProperty, "Merchant.Description");
        var open = new Label(); open.SetBinding(Label.TextProperty, "Merchant.IsOpen", stringFormat: "Open: {0}");
        var layout = new VerticalStackLayout { Padding = 20, Spacing = 12, Children = { name, description, open, new Label { Text = "Branches", FontSize = 20, FontAttributes = FontAttributes.Bold }, branches, Action("Browse catalog", ViewModel.OpenCatalogAsync) } }; AddState(layout); Content = new ScrollView { Content = layout };
        ViewModel.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(MerchantDetailsViewModel.Merchant)) RenderBranches(); };
    }
    public async void ApplyQueryAttributes(IDictionary<string, object> query) { if (query.TryGetValue("merchantId", out object? value) && Guid.TryParse(value.ToString(), out Guid id)) await ViewModel.LoadAsync(id); }
    private void RenderBranches() { branches.Clear(); foreach (CustomerMerchantBranchSummary branch in ViewModel.Merchant?.Branches ?? []) branches.Add(new Label { Text = $"{branch.Name} — {branch.Street}, {branch.City} — {(branch.IsOpen ? "Open" : "Closed")}" }); }
}

public sealed class CatalogPage : RemotePage<CatalogViewModel>, IQueryAttributable
{
    private Guid merchantId;
    private readonly CollectionView products;
    public CatalogPage() : base("Catalog")
    {
        var search = new SearchBar { Placeholder = "Search this merchant", MinimumHeightRequest = 48 }; search.SetBinding(SearchBar.TextProperty, nameof(CatalogViewModel.Query)); search.SearchButtonPressed += async (_, _) => await ViewModel.LoadAsync(merchantId, true);
        products = new CollectionView { SelectionMode = SelectionMode.Single, EmptyView = "No products available." }; products.SetBinding(ItemsView.ItemsSourceProperty, nameof(CatalogViewModel.Products));
        products.ItemTemplate = new DataTemplate(() => { var name = new Label { FontSize = 18, FontAttributes = FontAttributes.Bold }; name.SetBinding(Label.TextProperty, "Text.Name"); var price = new Label(); price.SetBinding(Label.TextProperty, nameof(ProductResponse.BasePriceMinor), stringFormat: "From {0} minor units"); return new Border { Padding = 14, Margin = 4, Content = new VerticalStackLayout { Children = { name, price } } }; });
        products.SelectionChanged += async (_, args) => { if (args.CurrentSelection.FirstOrDefault() is ProductResponse product) await ViewModel.OpenProductAsync(product); products.SelectedItem = null; };
        products.RemainingItemsThreshold = 3; products.RemainingItemsThresholdReached += async (_, _) => await ViewModel.LoadMoreAsync();
        var layout = new VerticalStackLayout { Padding = 16, Spacing = 8, Children = { search } }; AddState(layout); layout.Add(products); Content = layout;
    }
    public async void ApplyQueryAttributes(IDictionary<string, object> query) { if (query.TryGetValue("merchantId", out object? value) && Guid.TryParse(value.ToString(), out merchantId)) await ViewModel.LoadAsync(merchantId); }
}

public sealed class ProductDetailsPage : RemotePage<ProductViewModel>, IQueryAttributable
{
    public ProductDetailsPage() : base("Product")
    {
        var name = new Label { FontSize = 28, FontAttributes = FontAttributes.Bold }; name.SetBinding(Label.TextProperty, "Product.Text.Name");
        var description = new Label(); description.SetBinding(Label.TextProperty, "Product.Text.Description");
        var price = new Label { FontSize = 20 }; price.SetBinding(Label.TextProperty, "Price.TotalPriceMinor", stringFormat: "{0} minor units");
        var quantity = new Stepper { Minimum = 1, Maximum = 99, Increment = 1 }; quantity.SetBinding(Stepper.ValueProperty, nameof(ProductViewModel.Quantity));
        var options = new Label(); options.SetBinding(Label.TextProperty, nameof(ProductViewModel.OptionsAvailabilityMessage));
        var layout = new VerticalStackLayout { Padding = 20, Spacing = 12, Children = { name, description, price, options, new Label { Text = "Quantity" }, quantity, Action("Add to cart", ViewModel.AddToCartAsync) } }; AddState(layout); Content = new ScrollView { Content = layout };
    }
    public async void ApplyQueryAttributes(IDictionary<string, object> query) { if (PageQueries.GuidValue(query, "merchantId", out Guid merchant) && PageQueries.GuidValue(query, "productId", out Guid product)) await ViewModel.LoadAsync(merchant, product); }
}

public sealed class CartPage : RemotePage<CartViewModel>
{
    private readonly VerticalStackLayout items = new() { Spacing = 8 };
    private readonly Entry coupon = new() { Placeholder = "Coupon code", MinimumHeightRequest = 48 };
    public CartPage() : base("Cart")
    {
        var total = new Label { FontSize = 20, FontAttributes = FontAttributes.Bold }; total.SetBinding(Label.TextProperty, "Summary.GrandTotalMinor", stringFormat: "Authoritative total: {0} minor units");
        var layout = new VerticalStackLayout { Padding = 18, Spacing = 10, Children = { new Label { Text = "Your cart", FontSize = 28, FontAttributes = FontAttributes.Bold }, items, total, coupon, Action("Apply coupon", () => ViewModel.ApplyCouponAsync(coupon.Text ?? string.Empty)), Action("Remove coupon", ViewModel.RemoveCouponAsync), Action("Clear cart", ViewModel.ClearAsync), Action("Checkout", ViewModel.CheckoutAsync) } }; AddState(layout); Content = new ScrollView { Content = layout };
        ViewModel.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(CartViewModel.Items)) Render(); };
    }
    protected override async void OnAppearing() { base.OnAppearing(); await ViewModel.LoadAsync(true); Render(); }
    private void Render()
    {
        items.Clear();
        foreach (CartItemResponse item in ViewModel.Items)
        {
            var row = new HorizontalStackLayout { Spacing = 8, Children = { new Label { Text = $"{item.ProductId} × {item.Quantity}", VerticalTextAlignment = TextAlignment.Center } } };
            row.Add(Action("−", () => item.Quantity <= 1 ? ViewModel.RemoveAsync(item) : ViewModel.ChangeQuantityAsync(item, item.Quantity - 1)));
            row.Add(Action("+", () => ViewModel.ChangeQuantityAsync(item, item.Quantity + 1)));
            row.Add(Action("Remove", () => ViewModel.RemoveAsync(item))); items.Add(row);
        }
    }
}

public sealed class AddressesPage : RemotePage<AddressesViewModel>
{
    private readonly VerticalStackLayout items = new() { Spacing = 8 };
    private readonly Entry query = new() { Placeholder = "Enter an address", MinimumHeightRequest = 48 };
    private readonly Entry label = new() { Placeholder = "Address label", Text = "Home", MinimumHeightRequest = 48 };
    private readonly Entry city = new() { Placeholder = "City", MinimumHeightRequest = 48 };
    private readonly Entry street = new() { Placeholder = "Street", MinimumHeightRequest = 48 };
    public AddressesPage() : base("Addresses")
    {
        var layout = new VerticalStackLayout { Padding = 18, Spacing = 10, Children = { label, city, street, query, Action("Find address", async () => { await ViewModel.SearchAddressAsync(query.Text ?? string.Empty); RenderSuggestions(); }), Action("Use current location", UseCurrentLocationAsync), items } }; AddState(layout); Content = new ScrollView { Content = layout };
    }
    protected override async void OnAppearing() { base.OnAppearing(); await ViewModel.LoadAsync(); RenderAddresses(); }
    private void RenderAddresses() { items.Clear(); foreach (AddressResponse address in ViewModel.Items) { var row = new VerticalStackLayout { Children = { new Label { Text = $"{address.Label}: {address.Street}, {address.City}{(address.IsDefault ? " (default)" : string.Empty)}" }, Action("Set default", () => ViewModel.SetDefaultAsync(address)), Action("Delete", () => ViewModel.DeleteAsync(address)) } }; items.Add(new Border { Padding = 12, Content = row }); } }
    private void RenderSuggestions() { items.Clear(); foreach (GeocodingResult result in ViewModel.Suggestions) items.Add(Action(result.FormattedAddress, async () => { if (await ViewModel.AddAsync(label.Text ?? string.Empty, city.Text ?? string.Empty, street.Text ?? string.Empty, result)) RenderAddresses(); })); }
    private async Task UseCurrentLocationAsync()
    {
        Location? location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(15)));
        if (location is not null && await ViewModel.ReverseAndAddAsync(label.Text ?? string.Empty, city.Text ?? string.Empty, street.Text ?? string.Empty, location.Latitude, location.Longitude)) RenderAddresses();
    }
}

public sealed class CheckoutPage : RemotePage<CheckoutViewModel>, IQueryAttributable
{
    private readonly Picker addresses = new() { Title = "Delivery address", MinimumHeightRequest = 48 };
    public CheckoutPage() : base("Checkout")
    {
        addresses.SetBinding(Picker.ItemsSourceProperty, nameof(CheckoutViewModel.Addresses)); addresses.ItemDisplayBinding = new Binding(nameof(AddressResponse.Label)); addresses.SetBinding(Picker.SelectedItemProperty, nameof(CheckoutViewModel.SelectedAddress));
        var total = new Label { FontSize = 22, FontAttributes = FontAttributes.Bold }; total.SetBinding(Label.TextProperty, "Summary.GrandTotalMinor", stringFormat: "Total: {0} minor units");
        var layout = new VerticalStackLayout { Padding = 20, Spacing = 12, Children = { addresses, total, new Label { Text = "Payment: cash / supported non-electronic method" }, Action("Place order", ViewModel.SubmitAsync) } }; AddState(layout); Content = new ScrollView { Content = layout };
    }
    public async void ApplyQueryAttributes(IDictionary<string, object> query) { if (PageQueries.GuidValue(query, "cartId", out Guid id)) await ViewModel.LoadAsync(id); }
}

public sealed class OrdersPage : RemotePage<OrdersViewModel>
{
    private readonly CollectionView list;
    public OrdersPage() : base("Orders")
    {
        list = new CollectionView { SelectionMode = SelectionMode.Single, EmptyView = "No orders yet." }; list.SetBinding(ItemsView.ItemsSourceProperty, nameof(OrdersViewModel.Items));
        list.ItemTemplate = new DataTemplate(() => { var number = new Label { FontAttributes = FontAttributes.Bold }; number.SetBinding(Label.TextProperty, nameof(OrderListItemResponse.OrderNumber)); var merchant = new Label(); merchant.SetBinding(Label.TextProperty, nameof(OrderListItemResponse.MerchantDisplayName)); var status = new Label(); status.SetBinding(Label.TextProperty, nameof(OrderListItemResponse.Status), stringFormat: "Status: {0}"); return new Border { Padding = 14, Margin = 4, Content = new VerticalStackLayout { Children = { number, merchant, status } } }; });
        list.SelectionChanged += async (_, args) => { if (args.CurrentSelection.FirstOrDefault() is OrderListItemResponse order) await ViewModel.OpenAsync(order); list.SelectedItem = null; };
        var layout = new VerticalStackLayout { Padding = 16 }; AddState(layout); layout.Add(list); Content = layout;
    }
    protected override async void OnAppearing() { base.OnAppearing(); await ViewModel.LoadAsync(true); }
}

public sealed class OrderDetailsPage : RemotePage<OrderDetailsViewModel>, IQueryAttributable
{
    public OrderDetailsPage() : base("Order details")
    {
        var number = new Label { FontSize = 26, FontAttributes = FontAttributes.Bold }; number.SetBinding(Label.TextProperty, "Order.OrderNumber");
        var merchant = new Label(); merchant.SetBinding(Label.TextProperty, "Order.Merchant.MerchantDisplayName");
        var status = new Label(); status.SetBinding(Label.TextProperty, nameof(OrderDetailsViewModel.StatusLabel));
        var total = new Label(); total.SetBinding(Label.TextProperty, "Order.TotalMinor", stringFormat: "Total: {0} minor units");
        var layout = new VerticalStackLayout { Padding = 20, Spacing = 12, Children = { number, merchant, status, total, Action("Live tracking", ViewModel.TrackAsync), Action("Cancel order", () => ViewModel.CancelAsync("Customer requested cancellation")) } }; AddState(layout); Content = new ScrollView { Content = layout };
    }
    public async void ApplyQueryAttributes(IDictionary<string, object> query) { if (PageQueries.GuidValue(query, "orderId", out Guid id)) await ViewModel.LoadAsync(id); }
}

public sealed class TrackingPage : RemotePage<TrackingViewModel>, IQueryAttributable
{
    private readonly Microsoft.Maui.Controls.Maps.Map map = new() { HeightRequest = 320, IsShowingUser = false };
    public TrackingPage() : base("Live tracking")
    {
        var connection = new Label(); connection.SetBinding(Label.TextProperty, nameof(TrackingViewModel.Connection), stringFormat: "Connection: {0}");
        var point = new Label(); point.SetBinding(Label.TextProperty, "Location.Latitude", stringFormat: "Latitude: {0:F6}");
        var longitude = new Label(); longitude.SetBinding(Label.TextProperty, "Location.Longitude", stringFormat: "Longitude: {0:F6}");
        var timestamp = new Label(); timestamp.SetBinding(Label.TextProperty, "Location.RecordedAtUtc", stringFormat: "Updated: {0:u}");
        var layout = new VerticalStackLayout { Padding = 20, Spacing = 12, Children = { new Label { Text = "Driver location", FontSize = 26, FontAttributes = FontAttributes.Bold }, connection, map, point, longitude, timestamp, new Label { Text = "Android map tiles require a deployment-supplied Google Maps client key." } } }; AddState(layout); Content = new ScrollView { Content = layout };
        ViewModel.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(TrackingViewModel.Location)) UpdateMap(); };
    }
    public async void ApplyQueryAttributes(IDictionary<string, object> query) { if (PageQueries.GuidValue(query, "orderId", out Guid id)) await ViewModel.StartAsync(id); }
    protected override async void OnDisappearing() { await ViewModel.StopAsync(); base.OnDisappearing(); }
    private void UpdateMap()
    {
        if (ViewModel.Location is null) return;
        var location = new Location(ViewModel.Location.Latitude, ViewModel.Location.Longitude);
        map.Pins.Clear(); map.Pins.Add(new Pin { Label = "Current delivery location", Location = location, Type = PinType.Generic });
        map.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(1)));
    }
}

public sealed class NotificationsPage : RemotePage<NotificationsViewModel>
{
    private readonly VerticalStackLayout items = new() { Spacing = 8 };
    private readonly VerticalStackLayout preferences = new() { Spacing = 6 };
    private readonly IPushPermissionService permission = AppServices.Get<IPushPermissionService>();
    private readonly PushRegistrationCoordinator push = AppServices.Get<PushRegistrationCoordinator>();
    public NotificationsPage() : base("Notifications")
    {
        var unread = new Label(); unread.SetBinding(Label.TextProperty, nameof(NotificationsViewModel.UnreadCount), stringFormat: "Unread: {0}");
        var layout = new VerticalStackLayout { Padding = 18, Spacing = 10, Children = { unread, Action("Enable push notifications", EnablePushAsync), Action("Mark all read", ViewModel.MarkAllReadAsync), new Label { Text = "Preferences", FontSize = 20, FontAttributes = FontAttributes.Bold }, preferences, items, Action("Load more", ViewModel.LoadMoreAsync) } }; AddState(layout); Content = new ScrollView { Content = layout };
        ViewModel.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(NotificationsViewModel.Items)) Render(); if (args.PropertyName == nameof(NotificationsViewModel.Preferences)) RenderPreferences(); };
    }
    protected override async void OnAppearing() { base.OnAppearing(); await ViewModel.LoadAsync(true); Render(); RenderPreferences(); }
    private void Render() { items.Clear(); foreach (NotificationListItem item in ViewModel.Items) items.Add(new Border { Padding = 12, Content = Action($"{(item.ReadAtUtc is null ? "● " : string.Empty)}{item.Subject ?? item.Category}\n{item.Body}", () => ViewModel.OpenAsync(item)) }); }
    private void RenderPreferences() { preferences.Clear(); foreach (NotificationPreferenceItem item in ViewModel.Preferences) { var toggle = new Switch { IsToggled = item.Enabled }; toggle.Toggled += async (_, args) => await ViewModel.SetPreferenceAsync(item, args.Value); preferences.Add(new HorizontalStackLayout { Children = { new Label { Text = $"{item.Category} ({item.Channel})", VerticalTextAlignment = TextAlignment.Center }, toggle } }); } }
    private async Task EnablePushAsync() { if (await permission.RequestAsync()) await push.RegisterAsync(default); }
}

public sealed class ProfilePage : RemotePage<ProfileViewModel>
{
    public ProfilePage() : base("Profile")
    {
        var name = new Label { FontSize = 26, FontAttributes = FontAttributes.Bold }; name.SetBinding(Label.TextProperty, "Customer.DisplayName");
        var layout = new VerticalStackLayout { Padding = 20, Spacing = 12, Children = { name, Action("Addresses", () => Shell.Current.GoToAsync(AppRoutes.Addresses)), Action("Notifications", () => Shell.Current.GoToAsync(AppRoutes.Notifications)), Action("English", () => { ViewModel.ChangeLanguage("en"); return Task.CompletedTask; }), Action("العربية", () => { ViewModel.ChangeLanguage("ar"); return Task.CompletedTask; }), Action("עברית", () => { ViewModel.ChangeLanguage("he"); return Task.CompletedTask; }), Action("Logout", ViewModel.LogoutAsync) } }; AddState(layout); Content = new ScrollView { Content = layout };
    }
    protected override async void OnAppearing() { base.OnAppearing(); await ViewModel.LoadAsync(); }
}

public sealed class LegalPage : ContentPage
{
    public LegalPage() { Title = "Legal"; Content = new ScrollView { Content = new Label { Margin = 20, Text = "Terms and privacy content is supplied by the deployment owner. No support backend is available in Phase 18." } }; }
}

internal static class PageQueries
{
    public static bool GuidValue(IDictionary<string, object> query, string key, out Guid value)
    {
        value = Guid.Empty;
        return query.TryGetValue(key, out object? raw) && Guid.TryParse(raw?.ToString(), out value);
    }
}

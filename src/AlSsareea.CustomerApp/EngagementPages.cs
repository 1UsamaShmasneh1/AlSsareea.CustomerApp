using AlSsareea.CustomerApp.Core;
#if ANDROID || IOS || MACCATALYST
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
#endif

namespace AlSsareea.CustomerApp;

public sealed class OrdersPage : RemotePage<OrdersViewModel>
{
    private readonly VerticalStackLayout items = new() { Spacing = 8 };
    public OrdersPage() : base("Orders") { var layout = new VerticalStackLayout { Padding = 16 }; AddState(layout); layout.Add(items); Content = new ScrollView { Content = layout }; ViewModel.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(OrdersViewModel.Items)) Render(); }; }
    protected override async void OnAppearing() { base.OnAppearing(); await ViewModel.LoadAsync(true); Render(); }
    private void Render() { items.Clear(); if (ViewModel.Items.Count == 0) items.Add(new Label { Text = T("NoOrders") }); foreach (OrderListItemResponse order in ViewModel.Items) items.Add(new Border { Padding = 14, Content = Action(F("OrderListFormat", order.OrderNumber, order.MerchantDisplayName, T(OrderStatusPresentation.Key(order.Status))), () => ViewModel.OpenAsync(order)) }); }
}

public sealed class OrderDetailsPage : RemotePage<OrderDetailsViewModel>, IQueryAttributable
{
    private readonly VerticalStackLayout details = new() { Spacing = 12 };
    public OrderDetailsPage() : base("OrderDetails") { var layout = new VerticalStackLayout { Padding = 20, Spacing = 12, Children = { details } }; AddState(layout); Content = new ScrollView { Content = layout }; ViewModel.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(OrderDetailsViewModel.Order)) Render(); }; }
    public async void ApplyQueryAttributes(IDictionary<string, object> query) { if (PageQueries.GuidValue(query, "orderId", out Guid id)) { await ViewModel.LoadAsync(id); Render(); } }
    private void Render()
    {
        details.Clear(); OrderDetailsResponse? order = ViewModel.Order; if (order is null) return; details.Add(new Label { Text = F("OrderNumberFormat", order.OrderNumber), FontSize = 26, FontAttributes = FontAttributes.Bold }); details.Add(new Label { Text = order.Merchant.MerchantDisplayName }); details.Add(new Label { Text = ViewModel.StatusLabel }); details.Add(new Label { Text = F("CreatedFormat", order.CreatedAtUtc.ToLocalTime()) }); details.Add(new Label { Text = F("DeliveryAddressFormat", order.DeliveryAddress.Street, order.DeliveryAddress.City) }); details.Add(new Label { Text = F("TotalWithCurrencyFormat", order.TotalMinor, order.Currency), FontAttributes = FontAttributes.Bold });
        details.Add(new Label { Text = T("OrderItems"), FontSize = 20, FontAttributes = FontAttributes.Bold }); foreach (OrderItemResponse item in ViewModel.Items) { var stack = new VerticalStackLayout { Children = { new Label { Text = F("OrderItemFormat", item.ProductName, item.Quantity, item.LineTotalMinor) } } }; if (!string.IsNullOrWhiteSpace(item.VariantName)) stack.Add(new Label { Text = item.VariantName }); foreach (OrderOptionResponse option in item.Options) stack.Add(new Label { Text = F("CartOptionFormat", option.OptionGroupName, option.OptionName) }); details.Add(new Border { Padding = 10, Content = stack }); }
        details.Add(new Label { Text = T("OrderTimeline"), FontSize = 20, FontAttributes = FontAttributes.Bold }); foreach (OrderTimelineEntryResponse entry in ViewModel.Timeline) details.Add(new Label { Text = F("TimelineEntryFormat", ViewModel.TimelineLabel(entry), entry.ChangedAtUtc.ToLocalTime(), entry.ReasonText ?? string.Empty) }); if (ViewModel.CanTrack) details.Add(Action(T("LiveTracking"), ViewModel.TrackAsync)); if (ViewModel.CanCancel) details.Add(Action(T("CancelOrder"), () => ViewModel.CancelAsync(T("CustomerCancellationReason"))));
    }
}

public sealed class TrackingPage : RemotePage<TrackingViewModel>, IQueryAttributable
{
#if ANDROID || IOS || MACCATALYST
    private readonly Microsoft.Maui.Controls.Maps.Map map = new() { HeightRequest = 320, IsShowingUser = false };
#endif
    public TrackingPage() : base("LiveTracking")
    {
        var connection = new Label(); connection.SetBinding(Label.TextProperty, nameof(TrackingViewModel.Connection), stringFormat: T("ConnectionFormat")); var point = new Label(); point.SetBinding(Label.TextProperty, "Location.Latitude", stringFormat: T("LatitudeFormat")); var longitude = new Label(); longitude.SetBinding(Label.TextProperty, "Location.Longitude", stringFormat: T("LongitudeFormat")); var timestamp = new Label(); timestamp.SetBinding(Label.TextProperty, "Location.RecordedAtUtc", stringFormat: T("UpdatedFormat")); var layout = new VerticalStackLayout { Padding = 20, Spacing = 12, Children = { new Label { Text = T("DriverLocation"), FontSize = 26, FontAttributes = FontAttributes.Bold }, connection } };
#if ANDROID || IOS || MACCATALYST
        layout.Add(map);
#endif
        layout.Add(point); layout.Add(longitude); layout.Add(timestamp); layout.Add(new Label { Text = T("MapKeyNotice") }); AddState(layout); Content = new ScrollView { Content = layout }; ViewModel.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(TrackingViewModel.Location)) UpdateMap(); };
    }
    public async void ApplyQueryAttributes(IDictionary<string, object> query) { if (PageQueries.GuidValue(query, "orderId", out Guid id)) await ViewModel.StartAsync(id); }
    protected override async void OnDisappearing() { await ViewModel.StopAsync(); base.OnDisappearing(); }
    private void UpdateMap()
    {
#if ANDROID || IOS || MACCATALYST
        if (ViewModel.Location is null) return; var location = new Location(ViewModel.Location.Latitude, ViewModel.Location.Longitude); map.Pins.Clear(); map.Pins.Add(new Pin { Label = T("CurrentDeliveryLocation"), Location = location }); map.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(1)));
#endif
    }
}

public sealed class NotificationsPage : RemotePage<NotificationsViewModel>
{
    private readonly VerticalStackLayout items = new() { Spacing = 8 }; private readonly VerticalStackLayout preferences = new() { Spacing = 6 }; private readonly IPushPermissionService permission = AppServices.Get<IPushPermissionService>(); private readonly PushRegistrationCoordinator push = AppServices.Get<PushRegistrationCoordinator>();
    public NotificationsPage() : base("Notifications")
    {
        var unread = new Label(); unread.SetBinding(Label.TextProperty, nameof(NotificationsViewModel.UnreadCount), stringFormat: T("UnreadFormat")); var layout = new VerticalStackLayout { Padding = 18, Spacing = 10, Children = { unread, Action(T("EnablePush"), EnablePushAsync), Action(T("MarkAllRead"), ViewModel.MarkAllReadAsync), new Label { Text = T("Preferences"), FontSize = 20, FontAttributes = FontAttributes.Bold }, preferences, items, Action(T("LoadMore"), ViewModel.LoadMoreAsync) } }; AddState(layout); Content = new ScrollView { Content = layout }; ViewModel.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(NotificationsViewModel.Items)) Render(); if (args.PropertyName == nameof(NotificationsViewModel.Preferences)) RenderPreferences(); };
    }
    protected override async void OnAppearing() { base.OnAppearing(); await ViewModel.LoadAsync(true); Render(); RenderPreferences(); }
    private void Render() { items.Clear(); if (ViewModel.Items.Count == 0) items.Add(new Label { Text = T("NoNotifications") }); foreach (NotificationListItem item in ViewModel.Items) items.Add(new Border { Padding = 12, Content = Action($"{(item.ReadAtUtc is null ? "● " : string.Empty)}{item.Subject ?? item.Category}\n{item.Body}", () => ViewModel.OpenAsync(item)) }); }
    private void RenderPreferences() { preferences.Clear(); foreach (NotificationPreferenceItem item in ViewModel.Preferences) { var toggle = new Switch { IsToggled = item.Enabled }; toggle.Toggled += async (_, args) => await ViewModel.SetPreferenceAsync(item, args.Value); preferences.Add(new HorizontalStackLayout { Children = { new Label { Text = F("NotificationPreferenceFormat", item.Category, item.Channel), VerticalTextAlignment = TextAlignment.Center }, toggle } }); } }
    private async Task EnablePushAsync() { if (await permission.RequestAsync()) await push.RegisterAsync(default); }
}

public sealed class ProfilePage : RemotePage<ProfileViewModel>
{
    private readonly Entry firstName; private readonly Entry lastName;
    public ProfilePage() : base("Profile") { var name = new Label { FontSize = 26, FontAttributes = FontAttributes.Bold }; name.SetBinding(Label.TextProperty, "Customer.DisplayName"); firstName = new Entry { Placeholder = T("FirstName") }; lastName = new Entry { Placeholder = T("LastName") }; var layout = new VerticalStackLayout { Padding = 20, Spacing = 12, Children = { name, firstName, lastName, Action(T("SaveProfile"), () => ViewModel.UpdateAsync(firstName.Text ?? string.Empty, lastName.Text ?? string.Empty, ViewModel.Customer?.DateOfBirth)), Action(T("Addresses"), () => Shell.Current.GoToAsync(AppRoutes.Addresses)), Action(T("Notifications"), () => Shell.Current.GoToAsync(AppRoutes.Notifications)), Action(T("LanguageEnglish"), () => ChangeLanguage("en")), Action(T("LanguageArabic"), () => ChangeLanguage("ar")), Action(T("LanguageHebrew"), () => ChangeLanguage("he")), Action(T("Logout"), ViewModel.LogoutAsync) } }; AddState(layout); Content = new ScrollView { Content = layout }; ViewModel.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(ProfileViewModel.Customer) && ViewModel.Customer is not null) { firstName.Text = ViewModel.Customer.FirstName; lastName.Text = ViewModel.Customer.LastName; } }; }
    protected override async void OnAppearing() { base.OnAppearing(); await ViewModel.LoadAsync(); }
    private Task ChangeLanguage(string language) { ViewModel.ChangeLanguage(language); FlowDirection = Strings.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight; return Task.CompletedTask; }
}

public sealed class LegalPage : LocalizedPage { public LegalPage() : base("Legal") { Content = new ScrollView { Content = new Label { Margin = 20, Text = T("LegalNotice") } }; } }

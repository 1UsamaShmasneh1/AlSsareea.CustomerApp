using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp;

public abstract class LocalizedPage : ContentPage
{
    protected LocalizedPage(string titleKey) { Strings = AppServices.Get<ILocalizationService>(); Title = T(titleKey); FlowDirection = Strings.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight; }
    protected ILocalizationService Strings { get; }
    protected string T(string key) => Strings[key];
    protected string F(string key, params object?[] values) => string.Format(Strings[key], values);
    protected static Button Action(string text, Func<Task> execute, bool enabled = true) { var button = new Button { Text = text, MinimumHeightRequest = 48, IsEnabled = enabled }; button.Clicked += async (_, _) => await execute(); return button; }
}

public abstract class RemotePage<TViewModel> : LocalizedPage where TViewModel : RemoteViewModel
{
    protected RemotePage(string titleKey) : base(titleKey) { ViewModel = AppServices.Get<TViewModel>(); BindingContext = ViewModel; }
    protected TViewModel ViewModel { get; }
    protected void AddState(VerticalStackLayout layout)
    {
        var progress = new ActivityIndicator { HorizontalOptions = LayoutOptions.Center }; progress.SetBinding(ActivityIndicator.IsRunningProperty, nameof(RemoteViewModel.IsBusy)); progress.SetBinding(IsVisibleProperty, nameof(RemoteViewModel.IsBusy));
        var error = new Label { TextColor = Colors.Firebrick }; error.SetBinding(Label.TextProperty, nameof(RemoteViewModel.ErrorMessage)); error.SetBinding(IsVisibleProperty, nameof(RemoteViewModel.HasError)); layout.Add(progress); layout.Add(error);
    }
}

public sealed class SplashPage : LocalizedPage
{
    private readonly SplashViewModel viewModel = AppServices.Get<SplashViewModel>(); private bool started;
    public SplashPage() : base("AppName") { Shell.SetNavBarIsVisible(this, false); Content = new Grid { Children = { new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center, Spacing = 16, Children = { new Label { Text = T("AppName"), FontSize = 36, FontAttributes = FontAttributes.Bold }, new ActivityIndicator { IsRunning = true } } } } }; }
    protected override async void OnAppearing() { base.OnAppearing(); if (started) return; started = true; await viewModel.StartAsync(default); }
}

public sealed class OnboardingPage : LocalizedPage
{
    private readonly OnboardingViewModel viewModel = AppServices.Get<OnboardingViewModel>();
    public OnboardingPage() : base("Welcome")
    {
        BindingContext = viewModel; var language = new Picker { Title = T("Language"), ItemsSource = new[] { T("LanguageEnglish"), T("LanguageArabic"), T("LanguageHebrew") }, SelectedIndex = viewModel.SelectedLanguage switch { "ar" => 1, "he" => 2, _ => 0 }, MinimumHeightRequest = 48 };
        language.SelectedIndexChanged += (_, _) => { string value = language.SelectedIndex switch { 1 => "ar", 2 => "he", _ => "en" }; viewModel.SelectedLanguage = value; FlowDirection = value is "ar" or "he" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight; };
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 28, Spacing = 20, Children = { new Label { Text = T("OnboardingTitle"), FontSize = 28, FontAttributes = FontAttributes.Bold }, new Label { Text = T("OnboardingBody") }, language, Action(T("Continue"), viewModel.CompleteAsync) } } };
    }
}

public sealed class LoginPage : RemotePage<LoginViewModel>
{
    public LoginPage() : base("Login")
    {
        var identifier = new Entry { Placeholder = T("Email"), MinimumHeightRequest = 48, Keyboard = Keyboard.Email }; identifier.SetBinding(Entry.TextProperty, nameof(LoginViewModel.Identifier));
        var password = new Entry { Placeholder = T("Password"), IsPassword = true, MinimumHeightRequest = 48 }; password.SetBinding(Entry.TextProperty, nameof(LoginViewModel.Password));
        var layout = new VerticalStackLayout { Padding = 24, Spacing = 14, Children = { new Label { Text = T("WelcomeBack"), FontSize = 30, FontAttributes = FontAttributes.Bold }, identifier, password, Action(T("Login"), ViewModel.LoginAsync), Action(T("ContinueWithGoogle"), ViewModel.GoogleAsync), Action(T("RegisterNewCustomer"), ViewModel.RegisterAsync) } }; AddState(layout); Content = new ScrollView { Content = layout };
    }
}

public sealed class RegisterChoicePage : RemotePage<RegisterChoiceViewModel>
{
    public RegisterChoicePage() : base("CreateAccount")
    {
        var layout = new VerticalStackLayout { Padding = 24, Spacing = 14, Children = { new Label { Text = T("CreateAccount"), FontSize = 30, FontAttributes = FontAttributes.Bold }, Action(T("ContinueWithGoogle"), ViewModel.ContinueWithGoogleAsync), Action(T("RegisterWithEmail"), ViewModel.RegisterWithEmailAsync), Action(T("BackToLogin"), ViewModel.BackToLoginAsync) } };
        AddState(layout); Content = layout;
    }
}

public sealed class RegisterEmailPage : RemotePage<RegisterEmailViewModel>
{
    public RegisterEmailPage() : base("RegisterWithEmail")
    {
        var email = Entry("Email", nameof(RegisterEmailViewModel.Email), Keyboard.Email);
        var password = Entry("Password", nameof(RegisterEmailViewModel.Password), password: true);
        var confirm = Entry("ConfirmPassword", nameof(RegisterEmailViewModel.ConfirmPassword), password: true);
        var first = Entry("FirstName", nameof(RegisterEmailViewModel.FirstName));
        var last = Entry("LastName", nameof(RegisterEmailViewModel.LastName));
        var date = new DatePicker { MinimumHeightRequest = 48, MaximumDate = DateTime.Today, Date = DateTime.Today.AddYears(-18) };
        date.DateSelected += (_, args) => ViewModel.DateOfBirth = args.NewDate.HasValue ? DateOnly.FromDateTime(args.NewDate.Value) : null;
        var layout = new VerticalStackLayout { Padding = 24, Spacing = 12, Children = { email, password, confirm, first, last, new Label { Text = T("DateOfBirthOptional") }, date, Action(T("CreateAccount"), ViewModel.RegisterAsync), new Label { Text = T("AlreadyHaveAccount") }, Action(T("SignIn"), ViewModel.BackToLoginAsync) } };
        AddState(layout); Content = new ScrollView { Content = layout };
    }
    private Entry Entry(string key, string property, Keyboard? keyboard = null, bool password = false) { var entry = new Entry { Placeholder = T(key), IsPassword = password, Keyboard = keyboard ?? Keyboard.Default, MinimumHeightRequest = 48 }; entry.SetBinding(Microsoft.Maui.Controls.Entry.TextProperty, property); return entry; }
}

public sealed class CompleteProfilePage : RemotePage<CompleteProfileViewModel>, IQueryAttributable
{
    public CompleteProfilePage() : base("CompleteProfile")
    {
        var first = Entry("FirstName", nameof(CompleteProfileViewModel.FirstName)); var last = Entry("LastName", nameof(CompleteProfileViewModel.LastName));
        var date = new DatePicker { MinimumHeightRequest = 48, MaximumDate = DateTime.Today, Date = DateTime.Today.AddYears(-18) };
        date.DateSelected += (_, args) => ViewModel.DateOfBirth = args.NewDate.HasValue ? DateOnly.FromDateTime(args.NewDate.Value) : null;
        var layout = new VerticalStackLayout { Padding = 24, Spacing = 12, Children = { new Label { Text = T("CompleteProfileBody") }, first, last, new Label { Text = T("DateOfBirthOptional") }, date, Action(T("Continue"), ViewModel.SaveAsync) } };
        AddState(layout); Content = new ScrollView { Content = layout };
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query) => ViewModel.ApplyHints(query.TryGetValue("firstName", out object? first) ? first?.ToString() : null, query.TryGetValue("lastName", out object? last) ? last?.ToString() : null);
    private Entry Entry(string key, string property) { var entry = new Entry { Placeholder = T(key), MinimumHeightRequest = 48 }; entry.SetBinding(Microsoft.Maui.Controls.Entry.TextProperty, property); return entry; }
}

public class MainPage : MerchantListPage { public MainPage() : base("Home") { } }
public sealed class SearchPage : MerchantListPage { public SearchPage() : base("Search") { } }

public class MerchantListPage : RemotePage<MerchantDiscoveryViewModel>
{
    private readonly CollectionView list;
    protected MerchantListPage(string titleKey) : base(titleKey)
    {
        var search = new SearchBar { Placeholder = T("SearchMerchants"), MinimumHeightRequest = 48 }; search.SetBinding(SearchBar.TextProperty, nameof(MerchantDiscoveryViewModel.Query)); search.TextChanged += async (_, _) => await ViewModel.SearchDebouncedAsync(); var toggle = new Switch(); toggle.SetBinding(Switch.IsToggledProperty, nameof(MerchantDiscoveryViewModel.OpenNow)); toggle.Toggled += async (_, _) => await ViewModel.LoadAsync(true);
        list = new CollectionView { SelectionMode = SelectionMode.Single, EmptyView = new Label { Text = T("NoMerchants"), Margin = 20 } }; list.SetBinding(ItemsView.ItemsSourceProperty, nameof(MerchantDiscoveryViewModel.Items));
        list.ItemTemplate = new DataTemplate(() => { var name = new Label { FontSize = 19, FontAttributes = FontAttributes.Bold }; name.SetBinding(Label.TextProperty, nameof(CustomerMerchantSummary.DisplayName)); var description = new Label { MaxLines = 2 }; description.SetBinding(Label.TextProperty, nameof(CustomerMerchantSummary.Description)); return new Border { Padding = 16, Margin = new Thickness(0, 5), Content = new VerticalStackLayout { Children = { name, description } } }; });
        list.SelectionChanged += async (_, args) => { if (args.CurrentSelection.FirstOrDefault() is CustomerMerchantSummary merchant) await ViewModel.OpenAsync(merchant); list.SelectedItem = null; }; list.RemainingItemsThreshold = 3; list.RemainingItemsThresholdReached += async (_, _) => await ViewModel.LoadMoreAsync();
        var refresh = new RefreshView { Content = list }; refresh.SetBinding(RefreshView.IsRefreshingProperty, nameof(RemoteViewModel.IsBusy)); refresh.Refreshing += async (_, _) => await ViewModel.LoadAsync(true); var layout = new VerticalStackLayout { Padding = 16, Spacing = 8, Children = { search, new HorizontalStackLayout { Spacing = 8, Children = { new Label { Text = T("OpenNow"), VerticalTextAlignment = TextAlignment.Center }, toggle } } } }; AddState(layout); layout.Add(refresh); Content = layout;
    }
    protected override async void OnAppearing() { base.OnAppearing(); if (ViewModel.State == RemoteStateKind.Initial) await ViewModel.LoadAsync(); }
}

public sealed class MerchantDetailsPage : RemotePage<MerchantDetailsViewModel>, IQueryAttributable
{
    private readonly VerticalStackLayout branches = new() { Spacing = 8 };
    public MerchantDetailsPage() : base("Merchant") { var name = new Label { FontSize = 28, FontAttributes = FontAttributes.Bold }; name.SetBinding(Label.TextProperty, "Merchant.DisplayName"); var description = new Label(); description.SetBinding(Label.TextProperty, "Merchant.Description"); var layout = new VerticalStackLayout { Padding = 20, Spacing = 12, Children = { name, description, new Label { Text = T("Branches"), FontSize = 20, FontAttributes = FontAttributes.Bold }, branches, Action(T("BrowseCatalog"), ViewModel.OpenCatalogAsync) } }; AddState(layout); Content = new ScrollView { Content = layout }; ViewModel.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(MerchantDetailsViewModel.Merchant)) RenderBranches(); }; }
    public async void ApplyQueryAttributes(IDictionary<string, object> query) { if (PageQueries.GuidValue(query, "merchantId", out Guid id)) await ViewModel.LoadAsync(id); }
    private void RenderBranches() { branches.Clear(); foreach (CustomerMerchantBranchSummary branch in ViewModel.Merchant?.Branches ?? []) branches.Add(new Label { Text = F("BranchFormat", branch.Name, branch.Street, branch.City, T(branch.IsOpen ? "Open" : "Closed")) }); }
}

public sealed class CatalogPage : RemotePage<CatalogViewModel>, IQueryAttributable
{
    private Guid merchantId; private readonly CollectionView products;
    public CatalogPage() : base("Catalog")
    {
        var search = new SearchBar { Placeholder = T("SearchMerchantCatalog"), MinimumHeightRequest = 48 }; search.SetBinding(SearchBar.TextProperty, nameof(CatalogViewModel.Query)); search.SearchButtonPressed += async (_, _) => await ViewModel.LoadAsync(merchantId, true); products = new CollectionView { SelectionMode = SelectionMode.Single, EmptyView = T("NoProducts") }; products.SetBinding(ItemsView.ItemsSourceProperty, nameof(CatalogViewModel.Products));
        products.ItemTemplate = new DataTemplate(() => { var name = new Label { FontSize = 18, FontAttributes = FontAttributes.Bold }; name.SetBinding(Label.TextProperty, "Text.Name"); var price = new Label(); price.SetBinding(Label.TextProperty, nameof(ProductResponse.BasePriceMinor), stringFormat: T("FromPriceFormat")); return new Border { Padding = 14, Margin = 4, Content = new VerticalStackLayout { Children = { name, price } } }; }); products.SelectionChanged += async (_, args) => { if (args.CurrentSelection.FirstOrDefault() is ProductResponse product) await ViewModel.OpenProductAsync(product); products.SelectedItem = null; }; products.RemainingItemsThreshold = 3; products.RemainingItemsThresholdReached += async (_, _) => await ViewModel.LoadMoreAsync(); var layout = new VerticalStackLayout { Padding = 16, Spacing = 8, Children = { search } }; AddState(layout); layout.Add(products); Content = layout;
    }
    public async void ApplyQueryAttributes(IDictionary<string, object> query) { if (PageQueries.GuidValue(query, "merchantId", out merchantId)) await ViewModel.LoadAsync(merchantId); }
}

internal static class PageQueries { public static bool GuidValue(IDictionary<string, object> query, string key, out Guid value) { value = Guid.Empty; return query.TryGetValue(key, out object? raw) && Guid.TryParse(raw?.ToString(), out value); } }

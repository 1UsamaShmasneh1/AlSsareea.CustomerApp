using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp;

public sealed class AppShell : Shell
{
    public AppShell(ILocalizationService text, IPreferencesStore preferences)
    {
        text.Apply(preferences.Language);
        FlyoutBehavior = FlyoutBehavior.Disabled;
        Items.Add(new ShellContent { Route = AppRoutes.Splash, ContentTemplate = new DataTemplate(typeof(SplashPage)), FlyoutItemIsVisible = false });
        var tabs = new TabBar { Route = "main" };
        tabs.Items.Add(Tab(text["Home"], "home", typeof(MainPage)));
        tabs.Items.Add(Tab(text["Search"], "search", typeof(SearchPage)));
        tabs.Items.Add(Tab(text["Cart"], "cart", typeof(CartPage)));
        tabs.Items.Add(Tab(text["Orders"], "orders", typeof(OrdersPage)));
        tabs.Items.Add(Tab(text["Profile"], "profile", typeof(ProfilePage)));
        Items.Add(tabs);
        Register(AppRoutes.Onboarding, typeof(OnboardingPage));
        Register(AppRoutes.Login, typeof(LoginPage));
        Register(AppRoutes.RegisterChoice, typeof(RegisterChoicePage));
        Register(AppRoutes.RegisterEmail, typeof(RegisterEmailPage));
        Register(AppRoutes.CompleteProfile, typeof(CompleteProfilePage));
        Register(AppRoutes.MerchantDetails, typeof(MerchantDetailsPage));
        Register(AppRoutes.Catalog, typeof(CatalogPage));
        Register(AppRoutes.ProductDetails, typeof(ProductDetailsPage));
        Register(AppRoutes.Addresses, typeof(AddressesPage));
        Register(AppRoutes.Checkout, typeof(CheckoutPage));
        Register(AppRoutes.OrderDetails, typeof(OrderDetailsPage));
        Register(AppRoutes.Tracking, typeof(TrackingPage));
        Register(AppRoutes.Notifications, typeof(NotificationsPage));
        Register(AppRoutes.Legal, typeof(LegalPage));
    }
    private static ShellContent Tab(string title, string route, Type page) => new() { Title = title, Route = route, ContentTemplate = new DataTemplate(page) };
    private static void Register(string route, Type page) => Routing.RegisterRoute(route, page);
}

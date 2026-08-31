using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp;

public sealed class AppShell : Shell
{
    public AppShell()
    {
        FlyoutBehavior = FlyoutBehavior.Disabled;
        Items.Add(new ShellContent { Route = AppRoutes.Splash, ContentTemplate = new DataTemplate(typeof(SplashPage)), FlyoutItemIsVisible = false });
        var tabs = new TabBar { Route = "main" };
        tabs.Items.Add(Tab("Home", "home", typeof(MainPage)));
        tabs.Items.Add(Tab("Search", "search", typeof(SearchPage)));
        tabs.Items.Add(Tab("Cart", "cart", typeof(CartPage)));
        tabs.Items.Add(Tab("Orders", "orders", typeof(OrdersPage)));
        tabs.Items.Add(Tab("Profile", "profile", typeof(ProfilePage)));
        Items.Add(tabs);
        Register(AppRoutes.Onboarding, typeof(OnboardingPage));
        Register(AppRoutes.Login, typeof(LoginPage));
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

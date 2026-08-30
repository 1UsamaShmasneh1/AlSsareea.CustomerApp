namespace AlSsareea.CustomerApp;

public partial class AppShell : Shell { public AppShell() { InitializeComponent(); Routing.RegisterRoute("login", typeof(LoginPage)); Routing.RegisterRoute("addresses", typeof(AddressesPage)); Routing.RegisterRoute("checkout", typeof(CheckoutPage)); Routing.RegisterRoute("notifications", typeof(NotificationsPage)); Routing.RegisterRoute("tracking", typeof(TrackingPage)); } }

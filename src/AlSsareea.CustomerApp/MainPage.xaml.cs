namespace AlSsareea.CustomerApp;

public partial class MainPage : ContentPage { public MainPage() => InitializeComponent(); private async void OpenNotifications(object? sender, EventArgs e) => await Shell.Current.GoToAsync("notifications"); }

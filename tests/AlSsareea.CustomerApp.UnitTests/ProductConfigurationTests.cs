using AlSsareea.CustomerApp.Core;

namespace AlSsareea.CustomerApp.UnitTests;

public sealed class ProductConfigurationTests
{
    [Fact] public async Task Load_consumes_branch_language_media_and_defaults() { Fixture f = new(); await f.ViewModel.LoadAsync(f.MerchantId, f.ProductId); Assert.Equal("he", f.Catalog.Language); Assert.Equal(f.BranchId, f.Catalog.BranchId); Assert.True(f.ViewModel.Media[0].IsPrimary); Assert.Equal(f.Catalog.DefaultVariant.Id, f.ViewModel.SelectedVariantId); Assert.True(f.ViewModel.IsSelected(f.Catalog.RequiredGroup, f.Catalog.RequiredGroup.Options[0])); }
    [Fact] public async Task Load_quotes_default_configuration_with_backend() { Fixture f = new(); await f.ViewModel.LoadAsync(f.MerchantId, f.ProductId); Assert.Equal(f.Catalog.DefaultVariant.Id, f.Catalog.LastPrice!.VariantId); Assert.Contains(f.Catalog.RequiredGroup.Options[0].Id, f.Catalog.LastPrice.OptionIds); }
    [Fact] public async Task Unavailable_variant_cannot_be_selected() { Fixture f = new(); await f.ViewModel.LoadAsync(f.MerchantId, f.ProductId); int calls = f.Catalog.PriceCalls; await f.ViewModel.SelectVariantAsync(f.Catalog.UnavailableVariant); Assert.Equal(f.Catalog.DefaultVariant.Id, f.ViewModel.SelectedVariantId); Assert.Equal(calls, f.Catalog.PriceCalls); }
    [Fact] public async Task Available_variant_selection_requotes() { Fixture f = new(); await f.ViewModel.LoadAsync(f.MerchantId, f.ProductId); await f.ViewModel.SelectVariantAsync(f.Catalog.AlternateVariant); Assert.Equal(f.Catalog.AlternateVariant.Id, f.ViewModel.SelectedVariantId); Assert.Equal(f.Catalog.AlternateVariant.Id, f.Catalog.LastPrice!.VariantId); }
    [Fact] public async Task Single_choice_replaces_previous_value() { Fixture f = new(); await f.ViewModel.LoadAsync(f.MerchantId, f.ProductId); await f.ViewModel.ToggleOptionAsync(f.Catalog.RequiredGroup, f.Catalog.RequiredGroup.Options[1]); Assert.False(f.ViewModel.IsSelected(f.Catalog.RequiredGroup, f.Catalog.RequiredGroup.Options[0])); Assert.True(f.ViewModel.IsSelected(f.Catalog.RequiredGroup, f.Catalog.RequiredGroup.Options[1])); }
    [Fact] public async Task Multiple_choice_enforces_maximum() { Fixture f = new(); await f.ViewModel.LoadAsync(f.MerchantId, f.ProductId); CustomerProductOptionGroupResponse group = f.Catalog.OptionalGroup; await f.ViewModel.ToggleOptionAsync(group, group.Options[0]); await f.ViewModel.ToggleOptionAsync(group, group.Options[1]); Assert.Single(f.ViewModel.SelectedOptions[group.Id]); Assert.Equal("OptionMaximumReached", f.ViewModel.SelectionError); }
    [Fact] public async Task Unavailable_option_remains_unselected() { Fixture f = new(); await f.ViewModel.LoadAsync(f.MerchantId, f.ProductId); CustomerProductOptionResponse unavailable = f.Catalog.OptionalGroup.Options[2]; await f.ViewModel.ToggleOptionAsync(f.Catalog.OptionalGroup, unavailable); Assert.False(f.ViewModel.IsSelected(f.Catalog.OptionalGroup, unavailable)); }
    [Fact] public async Task Required_group_must_meet_minimum() { Fixture f = new(); await f.ViewModel.LoadAsync(f.MerchantId, f.ProductId); await f.ViewModel.ToggleOptionAsync(f.Catalog.RequiredGroup, f.Catalog.RequiredGroup.Options[0]); Assert.False(f.ViewModel.ValidateSelections()); Assert.Equal("OptionMinimumRequired", f.ViewModel.SelectionError); }
    [Fact] public async Task Optional_zero_selection_is_valid() { Fixture f = new(); await f.ViewModel.LoadAsync(f.MerchantId, f.ProductId); Assert.True(f.ViewModel.ValidateSelections()); }
    [Fact] public async Task Add_to_cart_maps_catalog_ids_and_note_exactly() { Fixture f = new(); await f.ViewModel.LoadAsync(f.MerchantId, f.ProductId); f.ViewModel.CustomerNote = "  no onions  "; f.ViewModel.Quantity = 3; await f.ViewModel.AddToCartAsync(); AddCartItemRequest request = Assert.IsType<AddCartItemRequest>(f.Cart.Added); Assert.Equal(f.ProductId, request.ProductId); Assert.Equal(f.Catalog.DefaultVariant.Id, request.ProductVariantId); Assert.Equal(3, request.Quantity); Assert.Equal("no onions", request.CustomerNote); CartItemOptionRequest option = Assert.Single(request.SelectedOptions); Assert.Equal(f.Catalog.RequiredGroup.Id, option.OptionGroupId); Assert.Equal(f.Catalog.RequiredGroup.Options[0].Id, option.OptionItemId); }
    [Fact] public async Task Add_to_cart_revalidates_authoritative_price() { Fixture f = new(); await f.ViewModel.LoadAsync(f.MerchantId, f.ProductId); int calls = f.Catalog.PriceCalls; await f.ViewModel.AddToCartAsync(); Assert.Equal(calls + 1, f.Catalog.PriceCalls); }
    [Fact] public async Task Unavailable_product_is_not_added() { Fixture f = new(available: false); await f.ViewModel.LoadAsync(f.MerchantId, f.ProductId); await f.ViewModel.AddToCartAsync(); Assert.Null(f.Cart.Added); Assert.Equal("ProductUnavailable", f.ViewModel.SelectionError); }

    private sealed class Fixture
    {
        public Fixture(bool available = true) { Catalog = new CatalogStub(available); Cart = new CartStub(); State = new CustomerAppState { BranchId = BranchId }; ViewModel = new(Catalog, Cart, State, new TestPreferences { Language = "he" }, new OnlineConnectivity(), new TestText("he"), new TestNavigation()); }
        public Guid MerchantId { get; } = Guid.NewGuid(); public Guid ProductId => Catalog.Product.Id; public Guid BranchId { get; } = Guid.NewGuid(); public CatalogStub Catalog { get; }
        public CartStub Cart { get; }
        public CustomerAppState State { get; }
        public ProductViewModel ViewModel { get; }
    }

    private sealed class CatalogStub : ICatalogApi
    {
        public CatalogStub(bool available)
        {
            DefaultVariant = Variant(true, true, 1); AlternateVariant = Variant(true, false, 2); UnavailableVariant = Variant(false, false, 3);
            RequiredGroup = Group(true, 1, 1, 1, [Option(true, true, 1), Option(true, false, 2)]); OptionalGroup = Group(false, 0, 1, 2, [Option(true, false, 1), Option(true, false, 2), Option(false, false, 3)]);
            Product = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, 1000, "ILS", null, 1, 1, 1, true, true, 3, Text("Product"), DateTime.UtcNow, DateTime.UtcNow, Guid.NewGuid(), available, [new(Guid.NewGuid(), null, "https://example.test/secondary.jpg", "Side", 2, false), new(Guid.NewGuid(), null, "https://example.test/primary.jpg", "Front", 9, true)], [UnavailableVariant, AlternateVariant, DefaultVariant], [OptionalGroup, RequiredGroup]);
        }
        public CustomerProductDetailsResponse Product { get; }
        public CustomerProductVariantResponse DefaultVariant { get; }
        public CustomerProductVariantResponse AlternateVariant { get; }
        public CustomerProductVariantResponse UnavailableVariant { get; }
        public CustomerProductOptionGroupResponse RequiredGroup { get; }
        public CustomerProductOptionGroupResponse OptionalGroup { get; }
        public string? Language { get; private set; }
        public Guid? BranchId { get; private set; }
        public PriceRequest? LastPrice { get; private set; }
        public int PriceCalls { get; private set; }
        public Task<CustomerProductDetailsResponse> ProductAsync(Guid merchantId, Guid productId, string language, Guid? branchId, CancellationToken ct) { Language = language; BranchId = branchId; return Task.FromResult(Product); }
        public Task<CatalogPriceResponse> PriceAsync(Guid merchantId, Guid productId, PriceRequest request, CancellationToken ct) { LastPrice = request; PriceCalls++; return Task.FromResult(new CatalogPriceResponse(productId, 3, "ILS", 1000, 100, 25, 1125, null, [])); }
        public Task<IReadOnlyList<CategoryResponse>> CategoriesAsync(Guid merchantId, string language, CancellationToken ct) => throw new NotSupportedException(); public Task<IReadOnlyList<MenuSectionResponse>> SectionsAsync(Guid merchantId, string language, CancellationToken ct) => throw new NotSupportedException(); public Task<ProductListResponse> ProductsAsync(Guid merchantId, int page, int pageSize, string? query, Guid? categoryId, string language, CancellationToken ct) => throw new NotSupportedException();
        private static LocalizedTextResponse Text(string name) => new("en", name, null); private static CustomerProductVariantResponse Variant(bool available, bool isDefault, int sort) => new(Guid.NewGuid(), Text($"V{sort}"), sort * 10, 1, isDefault, available, sort); private static CustomerProductOptionResponse Option(bool available, bool isDefault, int sort) => new(Guid.NewGuid(), Text($"O{sort}"), -sort * 5, isDefault, available, sort); private static CustomerProductOptionGroupResponse Group(bool required, int min, int max, short type, IReadOnlyList<CustomerProductOptionResponse> options) => new(Guid.NewGuid(), Text("Group"), type, required, min, max, 1, options);
    }

    private sealed class CartStub : ICartApi
    {
        private readonly Guid cartId = Guid.NewGuid(); public AddCartItemRequest? Added { get; private set; }
        public Task<CartResponse> CreateAsync(GetOrCreateActiveCartRequest request, string key, CancellationToken ct) => Task.FromResult(Cart([])); public Task<CartResponse> AddAsync(Guid cartId, AddCartItemRequest request, string key, CancellationToken ct) { Added = request; return Task.FromResult(Cart([new(Guid.NewGuid(), request.ProductId, request.ProductVariantId, request.Quantity, request.CustomerNote, 3, request.SelectedOptions.Select(x => new CartItemOptionResponse(x.OptionGroupId, x.OptionItemId, x.Quantity, 3)).ToArray(), DateTime.UtcNow, DateTime.UtcNow)])); }
        private CartResponse Cart(IReadOnlyList<CartItemResponse> items) => new(cartId, Guid.NewGuid(), Guid.NewGuid(), null, 1, null, DateTime.UtcNow.AddHours(1), null, DateTime.UtcNow, DateTime.UtcNow, Guid.NewGuid(), items);
        public Task<CartResponse> ActiveAsync(Guid merchantId, Guid? branchId, CancellationToken ct) => throw new NotSupportedException(); public Task<CartResponse> UpdateQuantityAsync(Guid cartId, Guid itemId, UpdateCartItemQuantityRequest request, string key, CancellationToken ct) => throw new NotSupportedException(); public Task<CartResponse> RemoveAsync(Guid cartId, Guid itemId, Guid concurrencyStamp, string key, CancellationToken ct) => throw new NotSupportedException(); public Task<CartResponse> ApplyCouponAsync(Guid cartId, ApplyCartCouponRequest request, string key, CancellationToken ct) => throw new NotSupportedException(); public Task<CartResponse> RemoveCouponAsync(Guid cartId, Guid concurrencyStamp, string key, CancellationToken ct) => throw new NotSupportedException(); public Task<CartResponse> ClearAsync(Guid cartId, Guid concurrencyStamp, string key, CancellationToken ct) => throw new NotSupportedException(); public Task<CartCheckoutSummaryResponse> RepriceAsync(Guid cartId, CancellationToken ct) => throw new NotSupportedException(); public Task<CartCheckoutSummaryResponse> SummaryAsync(Guid cartId, CancellationToken ct) => throw new NotSupportedException();
    }
}

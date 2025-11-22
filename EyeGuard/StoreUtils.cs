using System;
using System.Threading.Tasks;
using Windows.Services.Store;
using Microsoft.UI.Xaml;

namespace EyeGuard
{
    /// <summary>
    /// Test mode for Store operations - allows testing different purchase scenarios.
    /// IMPORTANT: Must be set to STORE for production builds.
    /// </summary>
    public enum StoreTestMode
    {
        /// <summary>Simulate user has not purchased the add-on</summary>
        DEV_NOT_PURCHASED,

        /// <summary>Simulate user has purchased the add-on</summary>
        DEV_PURCHASED,

        /// <summary>Use actual Microsoft Store (production mode)</summary>
        STORE
    }

    /// <summary>
    /// Result of a purchase attempt
    /// </summary>
    public enum PurchaseResult
    {
        /// <summary>Purchase completed successfully</summary>
        Succeeded,

        /// <summary>Product was already purchased</summary>
        AlreadyPurchased,

        /// <summary>User cancelled the purchase</summary>
        Cancelled,

        /// <summary>Network error occurred</summary>
        NetworkError,

        /// <summary>Server error occurred</summary>
        ServerError,

        /// <summary>Product not found in Store</summary>
        NotFound,

        /// <summary>Unknown error occurred</summary>
        Unknown
    }

    /// <summary>
    /// Utility class for managing Microsoft Store operations including
    /// product purchases and license validation.
    /// </summary>
    public class StoreUtils
    {
        // TEST_MODE configuration:
        // - In Release builds: Always uses STORE (real Microsoft Store)
        // - In Debug builds: Can be changed to DEV_NOT_PURCHASED or DEV_PURCHASED for testing
#if DEBUG
        // Change this value for testing different purchase scenarios in Debug mode:
        // - StoreTestMode.DEV_NOT_PURCHASED: Simulate user without add-on
        // - StoreTestMode.DEV_PURCHASED: Simulate user with add-on
        // - StoreTestMode.STORE: Use actual Store (same as Release)
        public const StoreTestMode TEST_MODE = StoreTestMode.STORE;
#else
        // Release builds always use the real Store
        public const StoreTestMode TEST_MODE = StoreTestMode.STORE;
#endif

        private static StoreUtils _instance;
        private StoreContext _storeContext;
        private bool? _cachedLicenseStatus;
        private DateTime _cacheTimestamp;
        private const int CACHE_DURATION_MINUTES = 5;

        // Product ID for the settings add-on
        private const string SETTINGS_ADDON_PRODUCT_ID = "eyeguardsettingsaddon";

        /// <summary>
        /// Gets the singleton instance of StoreUtils.
        /// </summary>
        public static StoreUtils Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new StoreUtils();
                }
                return _instance;
            }
        }

        private StoreUtils()
        {
            // StoreContext initialization will be done lazily when needed
        }

        /// <summary>
        /// Ensures StoreContext is initialized with the current window handle.
        /// Must be called from the UI thread.
        /// </summary>
        private void EnsureStoreContext()
        {
            if (_storeContext == null)
            {
                try
                {
                    _storeContext = StoreContext.GetDefault();

                    // Get the current app's main window
                    var app = Application.Current as App;
                    if (app != null && app.SecretWindow != null)
                    {
                        IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(app.SecretWindow);
                        WinRT.Interop.InitializeWithWindow.Initialize(_storeContext, windowHandle);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to initialize StoreContext: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Checks if the EyeGuard Settings Add-on is purchased by the user.
        /// Uses a short-term cache to avoid excessive Store API calls.
        /// Falls back to cached settings if Store API fails.
        /// </summary>
        /// <returns>True if the add-on is purchased, false otherwise.</returns>
        public async Task<bool> IsSettingsAddonPurchasedAsync()
        {
#pragma warning disable CS0162 // Unreachable code detected
            // Handle test modes
            if (TEST_MODE == StoreTestMode.DEV_NOT_PURCHASED)
            {
                System.Diagnostics.Debug.WriteLine("TEST_MODE: Simulating NOT PURCHASED");
                return false;
            }
            else if (TEST_MODE == StoreTestMode.DEV_PURCHASED)
            {
                System.Diagnostics.Debug.WriteLine("TEST_MODE: Simulating PURCHASED");
                return true;
            }

            // Production mode - check actual Store license
            // Check in-memory cache first
            if (_cachedLicenseStatus.HasValue &&
                (DateTime.Now - _cacheTimestamp).TotalMinutes < CACHE_DURATION_MINUTES)
            {
                return _cachedLicenseStatus.Value;
            }

            try
            {
                // Retrieve fresh license status from Store
                bool isPurchased = await CheckLicenseStatusAsync();

                // Update in-memory cache
                _cachedLicenseStatus = isPurchased;
                _cacheTimestamp = DateTime.Now;

                // Update persistent cache in Settings
                SettingsService.Instance.CachedSettingsAddonPurchased = isPurchased;

                return isPurchased;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to check license status from Store: {ex.Message}");

                // Fall back to cached setting
                var cachedStatus = SettingsService.Instance.CachedSettingsAddonPurchased;
                if (cachedStatus.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"Using cached license status: {cachedStatus.Value}");
                    return cachedStatus.Value;
                }

                // No cached status available, assume not purchased
                System.Diagnostics.Debug.WriteLine("No cached license status available, assuming not purchased");
                return false;
            }
#pragma warning restore CS0162 // Unreachable code detected
        }

        /// <summary>
        /// Checks the license status directly from the Store API.
        /// </summary>
        private async Task<bool> CheckLicenseStatusAsync()
        {
            try
            {
                EnsureStoreContext();

                if (_storeContext == null)
                {
                    System.Diagnostics.Debug.WriteLine("StoreContext is null. Cannot check license.");
                    return false;
                }

                // Get app license information
                StoreAppLicense appLicense = await _storeContext.GetAppLicenseAsync();

                if (appLicense == null)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to retrieve app license.");
                    return false;
                }

                // Check if the add-on is in the license
                // AddOnLicenses is keyed by Store ID, so we need to search by InAppOfferToken
                foreach (var kvp in appLicense.AddOnLicenses)
                {
                    if (kvp.Value.InAppOfferToken == SETTINGS_ADDON_PRODUCT_ID)
                    {
                        // Check if license is active (not expired or revoked)
                        return kvp.Value.IsActive;
                    }
                }

                // Add-on not found in licenses
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking license status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Initiates the purchase flow for the Settings Add-on.
        /// </summary>
        /// <returns>Result of the purchase attempt.</returns>
        public async Task<PurchaseResult> PurchaseSettingsAddonAsync()
        {
#pragma warning disable CS0162 // Unreachable code detected
            // Handle test modes
            if (TEST_MODE == StoreTestMode.DEV_NOT_PURCHASED)
            {
                System.Diagnostics.Debug.WriteLine("TEST_MODE: Simulating purchase attempt (always fails in DEV_NOT_PURCHASED mode)");
                return PurchaseResult.Cancelled;
            }
            else if (TEST_MODE == StoreTestMode.DEV_PURCHASED)
            {
                System.Diagnostics.Debug.WriteLine("TEST_MODE: Simulating purchase (already purchased in DEV_PURCHASED mode)");
                return PurchaseResult.AlreadyPurchased;
            }

            // Production mode - actual Store purchase
            try
            {
                EnsureStoreContext();

                if (_storeContext == null)
                {
                    System.Diagnostics.Debug.WriteLine("StoreContext is null. Cannot initiate purchase.");
                    return PurchaseResult.Unknown;
                }

                // First, get the product to obtain its Store ID
                StoreProduct product = await GetSettingsAddonProductAsync();
                if (product == null)
                {
                    System.Diagnostics.Debug.WriteLine("Could not find the settings add-on product.");
                    return PurchaseResult.NotFound;
                }

                // Request purchase using the Store ID
                StorePurchaseResult result = await _storeContext.RequestPurchaseAsync(product.StoreId);

                // Check purchase result
                switch (result.Status)
                {
                    case StorePurchaseStatus.Succeeded:
                        System.Diagnostics.Debug.WriteLine("Purchase succeeded!");
                        InvalidateCache();
                        return PurchaseResult.Succeeded;

                    case StorePurchaseStatus.AlreadyPurchased:
                        System.Diagnostics.Debug.WriteLine("Product already purchased.");
                        InvalidateCache();
                        return PurchaseResult.AlreadyPurchased;

                    case StorePurchaseStatus.NotPurchased:
                        System.Diagnostics.Debug.WriteLine("User cancelled the purchase.");
                        return PurchaseResult.Cancelled;

                    case StorePurchaseStatus.NetworkError:
                        System.Diagnostics.Debug.WriteLine("Network error during purchase.");
                        return PurchaseResult.NetworkError;

                    case StorePurchaseStatus.ServerError:
                        System.Diagnostics.Debug.WriteLine("Server error during purchase.");
                        return PurchaseResult.ServerError;

                    default:
                        System.Diagnostics.Debug.WriteLine($"Unknown purchase status: {result.Status}");
                        return PurchaseResult.Unknown;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during purchase: {ex.Message}");
                return PurchaseResult.Unknown;
            }
#pragma warning restore CS0162 // Unreachable code detected
        }

        /// <summary>
        /// Gets the StoreProduct for the Settings Add-on.
        /// </summary>
        /// <returns>StoreProduct if found, null otherwise.</returns>
        private async Task<StoreProduct> GetSettingsAddonProductAsync()
        {
            try
            {
                EnsureStoreContext();

                if (_storeContext == null)
                {
                    System.Diagnostics.Debug.WriteLine("StoreContext is null. Cannot get product.");
                    return null;
                }

                // Get add-ons for the current app
                StoreProductQueryResult addonsResult = await _storeContext.GetAssociatedStoreProductsAsync(
                    new string[] { "Durable" });

                if (addonsResult.ExtendedError != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Error querying add-ons: {addonsResult.ExtendedError.Message}");
                    return null;
                }

                // Find the settings add-on by searching for matching InAppOfferToken
                foreach (var kvp in addonsResult.Products)
                {
                    if (kvp.Value.InAppOfferToken == SETTINGS_ADDON_PRODUCT_ID)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found product: {kvp.Value.Title} (Store ID: {kvp.Value.StoreId})");
                        return kvp.Value;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Product with InAppOfferToken '{SETTINGS_ADDON_PRODUCT_ID}' not found.");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting product: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets detailed information about the Settings Add-on product.
        /// </summary>
        /// <returns>StoreProduct if found, null otherwise.</returns>
        public async Task<StoreProduct> GetSettingsAddonProductInfoAsync()
        {
            try
            {
                EnsureStoreContext();

                if (_storeContext == null)
                {
                    System.Diagnostics.Debug.WriteLine("StoreContext is null. Cannot get product info.");
                    return null;
                }

                // Query for the specific product
                var queryResult = await _storeContext.GetStoreProductForCurrentAppAsync();

                if (queryResult.ExtendedError != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Error querying product: {queryResult.ExtendedError.Message}");
                    return null;
                }

                // Get add-ons for the current app
                StoreProductQueryResult addonsResult = await _storeContext.GetAssociatedStoreProductsAsync(
                    new string[] { "Durable" });

                if (addonsResult.ExtendedError != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Error querying add-ons: {addonsResult.ExtendedError.Message}");
                    return null;
                }

                // Find the settings add-on by InAppOfferToken
                // Products dictionary is keyed by Store ID, not Product ID
                foreach (var kvp in addonsResult.Products)
                {
                    if (kvp.Value.InAppOfferToken == SETTINGS_ADDON_PRODUCT_ID)
                    {
                        return kvp.Value;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Product with InAppOfferToken '{SETTINGS_ADDON_PRODUCT_ID}' not found.");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting product info: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Invalidates the license status cache, forcing a fresh check on next call.
        /// </summary>
        public void InvalidateCache()
        {
            _cachedLicenseStatus = null;
        }

        /// <summary>
        /// Checks if the Store API is available (useful for testing in development).
        /// </summary>
        /// <returns>True if Store API is available, false otherwise.</returns>
        public bool IsStoreAvailable()
        {
            EnsureStoreContext();
            return _storeContext != null;
        }
    }
}

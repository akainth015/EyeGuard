# EyeGuard - Claude Code Development Notes

This document contains important learnings and patterns discovered while developing EyeGuard with Claude Code.

## Microsoft Store Integration

### StoreContext Initialization
The `StoreContext` API requires proper initialization with a window handle to function correctly, especially for purchase dialogs.

**Key Requirements:**
- Must be called on the UI thread
- Needs a valid window handle via `WinRT.Interop.WindowNative.GetWindowHandle(window)`
- Must initialize with `WinRT.Interop.InitializeWithWindow.Initialize(storeContext, windowHandle)`

**Common Issues:**
- `RequestPurchaseAsync()` throws "Invalid window handle (0x80070578)" if not properly initialized
- Error message: "This function must be called from a UI thread"

**Solution Pattern:**
```csharp
private void EnsureStoreContext()
{
    if (_storeContext == null)
    {
        _storeContext = StoreContext.GetDefault();
        var app = Application.Current as App;
        if (app != null && app.SecretWindow != null)
        {
            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(app.SecretWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(_storeContext, windowHandle);
        }
    }
}
```

### Store ID vs Product ID
When working with Microsoft Store add-ons, there's an important distinction:

- **Product ID (InAppOfferToken)**: The identifier you configure in Partner Center (e.g., "eyeguardsettingsaddon")
- **Store ID**: The actual ID used in the Products dictionary (e.g., "9N6MPSMNK7QD")

When calling `RequestPurchaseAsync()`, you must use the **Store ID**, not the Product ID.

**Finding the Store ID at runtime:**
```csharp
StoreProductQueryResult addonsResult = await _storeContext.GetAssociatedStoreProductsAsync(
    new string[] { "Durable" });

// Iterate through products to find by InAppOfferToken
foreach (var kvp in addonsResult.Products)
{
    if (kvp.Value.InAppOfferToken == SETTINGS_ADDON_PRODUCT_ID)
    {
        return kvp.Value; // Contains StoreId property
    }
}
```

### License Caching Strategy
Implement a multi-layer caching strategy for license checks to handle network outages gracefully:

1. **In-memory cache**: Short-term (5 minutes) to reduce API calls
2. **Persistent cache**: Store in `ApplicationData.LocalSettings` for offline fallback
3. **Fallback logic**: On Store API failure, use cached status

**Benefits:**
- Users retain access during internet outages if previously verified
- Reduces unnecessary Store API calls
- Better user experience with offline support

**Pattern:**
```csharp
try
{
    bool isPurchased = await CheckLicenseStatusAsync();
    _cachedLicenseStatus = isPurchased;
    SettingsService.Instance.CachedSettingsAddonPurchased = isPurchased; // Persist
    return isPurchased;
}
catch (Exception ex)
{
    var cachedStatus = SettingsService.Instance.CachedSettingsAddonPurchased;
    return cachedStatus ?? false; // Default to not purchased if never verified
}
```

## WinUI 3 Patterns

### Avoiding Reentrancy Errors
WinUI 3 can throw reentrancy errors when doing async work during page load events.

**Error:**
```
WinRT originate error - 0x80004005: 'Reentrancy was detected in this XAML application'
```

**Solution - Use DispatcherQueue:**
```csharp
private void HomePage_Loaded(object sender, RoutedEventArgs e)
{
    // Defer async work to avoid reentrancy
    DispatcherQueue.TryEnqueue(async () =>
    {
        await CheckAndShowAddonPromotion();
    });
}
```

### DropDownButton with MenuFlyout
For creating button menus with quick-select options:

```xaml
<DropDownButton Content="Pause Breaks">
    <DropDownButton.Flyout>
        <MenuFlyout Placement="Bottom">
            <MenuFlyoutItem Text="30 minutes" Click="QuickPause30Minutes_Click"/>
            <MenuFlyoutItem Text="1 hour" Click="QuickPause1Hour_Click"/>
            <MenuFlyoutSeparator/>
            <MenuFlyoutItem Text="Custom..." Click="QuickPauseCustom_Click"/>
        </MenuFlyout>
    </DropDownButton.Flyout>
</DropDownButton>
```

## Development Testing

### Test Mode Pattern
Use conditional compilation to ensure production builds always use real Store API:

```csharp
public enum StoreTestMode
{
    DEV_NOT_PURCHASED,  // Simulate no license
    DEV_PURCHASED,      // Simulate has license
    STORE               // Real Store API
}

#if DEBUG
    // Can be changed for testing in Debug builds
    public const StoreTestMode TEST_MODE = StoreTestMode.STORE;
#else
    // Release builds always use real Store
    public const StoreTestMode TEST_MODE = StoreTestMode.STORE;
#endif
```

This prevents accidentally shipping debug/test modes in production while allowing easy testing during development.

## Git Configuration

### Line Endings
For Windows development, configure Git to use CRLF:

```bash
git config core.autocrlf true
```

This eliminates warnings about CRLF/LF conversion during commits.

## Architecture Patterns

### Settings Service Pattern
Centralize app settings in a singleton service with:
- Property getters/setters backed by `ApplicationData.LocalSettings`
- Events for settings changes (e.g., `BreakIntervalChanged`)
- Validation and clamping of values
- Default values for first run

### UI License Enforcement
Enforce licensing at the UI visibility level rather than in individual handlers:
- Hide/show controls based on license status on page load
- Avoids redundant license checks in every button handler
- Cleaner, more maintainable code
- Single source of truth for license enforcement

## Timezone Handling

When working with user-selected times:
- Always display pickers in **local timezone**
- Store internally as **UTC** (`DateTime.ToUniversalTime()`)
- Convert back to local for display (`DateTime.ToLocalTime()`)
- Add UI hints: "Select date and time in your local timezone"

**Pattern:**
```csharp
// User selects local time
var localDateTime = PauseDatePicker.Date + PauseTimePicker.Time;
var localDateTimeObj = localDateTime.DateTime;

// Convert to UTC for storage
var utcDateTime = localDateTimeObj.ToUniversalTime();
SettingsService.Instance.PauseUntil = utcDateTime;

// Convert back to local for display
var localTime = pauseUntil.Value.ToLocalTime();
PauseDatePicker.Date = new DateTimeOffset(localTime.Date);
```

## Useful Resources

- [WinUI 3 Documentation](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)
- [Microsoft Store Services](https://learn.microsoft.com/en-us/windows/uwp/monetize/in-app-purchases-and-trials)
- [ApplicationData API](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.windows.storage.applicationdata)

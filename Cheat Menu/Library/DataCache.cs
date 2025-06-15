using Il2CppScheduleOne;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Product;
using static Modern_Cheat_Menu.Core;
using Modern_Cheat_Menu.Commands;
using Modern_Cheat_Menu.ModGUI;

namespace Modern_Cheat_Menu.Library
{
    public static class ModData
    {
        // Add dictionary for text fields
        public static Dictionary<string, CustomTextField> _textFields = new Dictionary<string, CustomTextField>();
        // Categories and commands
        public static List<CommandCore.CommandCategory> _categories = new List<CommandCore.CommandCategory>();
        public static Dictionary<string, string> _itemDictionary = new Dictionary<string, string>();
        public static Dictionary<string, List<string>> _vehicleCache = new Dictionary<string, List<string>>();
        public static Dictionary<string, List<string>> _itemCache = new Dictionary<string, List<string>>();

        public static Dictionary<string, bool> _qualitySupportCache = new Dictionary<string, bool>();
        public static Dictionary<string, List<string>> _itemQualityCache = new Dictionary<string, List<string>>();

        // Player network interaction category
        public class NetworkPlayerCategory
        {
            public string Name { get; set; }
            public List<CommandCore.Command> Commands { get; set; } = new List<CommandCore.Command>();
        }

        public static unsafe void CacheGameItems()
        {
            try
            {
                // Create dictionaries to store discovered items
                var discoveredItems = new Dictionary<string, string>();
                var qualitySupportCache = new Dictionary<string, bool>();
                var qualityItemCache = new Dictionary<string, List<string>>();

                // Get the registry
                var registry = Registry.Instance;
                if (registry == null)
                {
                    ModLogger.Error("Registry instance is NULL!");
                    return;
                }

                // Get quality names for reference
                var qualityNames = Enum.GetNames(typeof(EQuality)).ToList();

                // Get ProductManager instance to access drug product definitions
                var productManager = ProductManager.Instance;
                if (productManager == null)
                {
                    ModLogger.Error("ProductManager instance is NULL!");
                }

                // Create a managed list to store all products
                var allProducts = new List<ProductDefinition>();
                if (productManager != null)
                {
                    // Add all products from different sources to our local list
                    // Handle Il2Cpp collections properly
                    if (productManager.AllProducts != null)
                    {
                        for (int i = 0; i < productManager.AllProducts.Count; i++)
                        {
                            allProducts.Add(productManager.AllProducts[i]);
                        }
                    }

                    if (ProductManager.DiscoveredProducts != null)
                    {
                        for (int i = 0; i < ProductManager.DiscoveredProducts.Count; i++)
                        {
                            allProducts.Add(ProductManager.DiscoveredProducts[i]);
                        }
                    }

                    if (productManager.DefaultKnownProducts != null)
                    {
                        for (int i = 0; i < productManager.DefaultKnownProducts.Count; i++)
                        {
                            allProducts.Add(productManager.DefaultKnownProducts[i]);
                        }
                    }

                    // Remove duplicates - using a dictionary to track unique items by ID
                    var uniqueProducts = new Dictionary<string, ProductDefinition>();
                    foreach (var product in allProducts)
                    {
                        if (!uniqueProducts.ContainsKey(product.ID))
                        {
                            uniqueProducts[product.ID] = product;
                        }
                    }

                    allProducts = uniqueProducts.Values.ToList();
                    ModLogger.Info($"Found {allProducts.Count} product definitions from ProductManager");
                }

                // Enumerate all items in the registry
                foreach (var entry in registry.ItemDictionary)
                {
                    try
                    {
                        var itemDefinition = entry.Value.Definition;

                        // Skip null definitions
                        if (itemDefinition == null) continue;

                        // Try to get item ID and name
                        string itemId = itemDefinition.ID;
                        string itemName = itemDefinition.Name;

                        // Determine if thQis is a quality item
                        bool isQualityItem = false;
                        List<string> supportedQualities = null;

                        // Check 1: Explicit QualityItemDefinition type
                        var qualityDef = itemDefinition as QualityItemDefinition;
                        if (qualityDef != null)
                        {
                            isQualityItem = true;
                            supportedQualities = qualityNames;
                        }

                        // Check 2: Check if this is a drug product by comparing with ProductManager items
                        if (!isQualityItem && productManager != null)
                        {
                            // Try to find matching product in the product list
                            ProductDefinition matchingProduct = null;
                            foreach (var product in allProducts)
                            {
                                if (product.ID.Equals(itemId, StringComparison.OrdinalIgnoreCase) ||
                                    product.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase))
                                {
                                    matchingProduct = product;
                                    break;
                                }
                            }

                            if (matchingProduct != null)
                            {
                                isQualityItem = true;

                                // Determine drug type and available qualities based on product type
                                string drugType = matchingProduct.DrugType.ToString();

                                // Log product properties if available
                                if (matchingProduct.Properties != null && matchingProduct.Properties.Count > 0)
                                {
                                    // Handle Il2Cpp List without using LINQ
                                    var propertyNames = new List<string>();
                                    for (int i = 0; i < matchingProduct.Properties.Count; i++)
                                    {
                                        var property = matchingProduct.Properties[i];
                                        propertyNames.Add(property.Name);
                                    }

                                }

                                // For now, we'll use all quality levels, but this could be refined based on the product type
                                supportedQualities = qualityNames;
                            }
                        }

                        if (!string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(itemName))
                        {
                            // Add to main dictionary (display name => ID)
                            discoveredItems[itemName] = itemId;

                            // Track quality support
                            qualitySupportCache[itemId] = isQualityItem;

                            // If it's a quality item, cache quality levels
                            if (isQualityItem)
                            {
                                qualityItemCache[itemId] = supportedQualities ?? qualityNames;
                            }
                        }
                    }
                    catch (Exception innerEx)
                    {
                        ModLogger.Error($"Error processing item entry: {innerEx.Message}");
                    }
                }

                // Update class-level caches
                ModData._itemDictionary = discoveredItems;
                ModData._qualitySupportCache = qualitySupportCache;
                ModData._itemQualityCache = qualityItemCache;

                // Prepare standard caches
                ModData._itemCache["qualities"] = qualityNames;
                ModData._itemCache["items"] = discoveredItems.Keys.OrderBy(k => k).ToList();
                ModData._itemCache["slots"] = Enumerable.Range(1, 9).Select(x => x.ToString()).ToList();

                // Count quality items using standard .NET method
                int qualityItemCount = 0;
                foreach (var kvp in qualitySupportCache)
                {
                    if (kvp.Value) qualityItemCount++;
                }

                ModLogger.Info($"Item Discovery Complete:");
                ModLogger.Info($"- Total Items: {discoveredItems.Count}");
                ModLogger.Info($"- Quality Items: {qualityItemCount}");

            }
            catch (Exception ex)
            {
                ModLogger.Error($"Critical error in item discovery: {ex}");
                Notifier.ShowNotification("Error", "Failed to discover game items", NotificationSystem.NotificationType.Error);
            }
        }
    }
}

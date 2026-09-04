using ThriftHub.Models;

namespace ThriftHub.Services;

public sealed record MarketplaceCategoryDefinition(
    string Name,
    string Icon,
    string Description,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> Subcategories);

public static class MarketplaceCategoryCatalog
{
    private static readonly MarketplaceCategoryDefinition[] AllDefinitions =
    [
        Def(
            "Women's Fashion",
            "👗",
            "Dresses, tops, skirts, jeans and more",
            ["Women"],
            [
                "Dresses", "Tops", "Trousers", "Skirts", "Jeans", "Jackets",
                "Jumpsuits", "Joggers", "Socks", "Sportswear", "Traditional Wear", "Other"
            ]),

        Def(
            "Men's Fashion",
            "👔",
            "Shirts, trousers, jeans, jackets and more",
            ["Men"],
            [
                "Shirts", "T-Shirts", "Trousers", "Jeans", "Shorts", "Jackets",
                "Suits", "Joggers", "Socks", "Sportswear", "Traditional Wear", "Other"
            ]),

        Def("Kids", "🧸", "Fashion for babies and children", [],
            [
                "Boys Clothing", "Girls Clothing", "Baby Clothing",
                "School Wear", "Other"
            ]),

        Def("Shoes", "👟", "Sneakers, heels, sandals, boots and more", [],
            [
                "Sneakers", "Trainers", "Boots", "Formal Shoes", "Sandals",
                "Slippers", "Heels", "Other"
            ]),

        Def("Bags", "👜", "Handbags, backpacks, school bags and more", [],
            [
                "Backpacks", "Handbags", "Laptop Bags", "Travel Bags",
                "School Bags", "Other"
            ]),

        Def("Accessories", "💍", "Watches, belts, caps, jewellery and more", [],
            [
                "Watches", "Belts", "Jewellery", "Sunglasses", "Caps", "Hats", "Other"
            ]),

        Def("Sportswear & Activewear", "🏃", "Gym wear, sports kits, running and training gear", [],
            [
                "Sports Wear", "Gym Wear", "Running Wear", "Football Kits",
                "Basketball Kits", "Tracksuits", "Compression Wear", "Other"
            ]),

        Def("Joggers & Tracksuits", "👖", "Joggers, track pants, and matching sets", [],
            [
                "Joggers", "Track Pants", "Track Suits", "Sweatpants", "Cargo Joggers", "Other"
            ]),

        Def("Hoodies & Sweatshirts", "🧥", "Hoodies, pullovers, and sweatshirts", [],
            [
                "Hoodies", "Zip Hoodies", "Crewneck Sweatshirts", "Oversized Hoodies", "Other"
            ]),

        Def("Socks & Hosiery", "🧦", "Socks, tights, stockings, and legwear", [],
            [
                "Ankle Socks", "Crew Socks", "Sports Socks", "Dress Socks",
                "Tights", "Stockings", "Other"
            ]),

        Def("T-Shirts & Polos", "👕", "T-shirts, polos, tank tops, and casual tops", [],
            [
                "T-Shirts", "Polo Shirts", "Tank Tops", "Long Sleeve Tees",
                "Graphic Tees", "Crop Tops", "Other"
            ]),

        Def("Coats & Outerwear", "🧣", "Jackets, coats, blazers, and outer layers", [],
            [
                "Jackets", "Coats", "Blazers", "Puffer Jackets",
                "Denim Jackets", "Leather Jackets", "Other"
            ]),

        Def("Swimwear", "🩱", "Swimsuits, trunks, bikinis, and beach wear", [],
            [
                "One-Piece", "Bikini", "Swim Trunks", "Board Shorts", "Cover Ups", "Other"
            ]),

        Def("Underwear & Loungewear", "🛌", "Underwear, sleepwear, and loungewear", [],
            [
                "Underwear", "Boxers", "Briefs", "Bras", "Pyjamas", "Loungewear", "Other"
            ]),

        Def("Traditional & Cultural Wear", "🌍", "Kente, African prints, and cultural outfits", [],
            [
                "Kente", "African Print", "Kaftan", "Agbada", "Traditional Dress", "Other"
            ]),

        Def("Vintage & Thrift Fashion", "♻️", "Vintage pieces and unique thrift finds", [],
            [
                "Vintage Clothing", "Retro Fashion", "Designer Thrift", "Rare Finds", "Other"
            ]),

        Def("Books & Textbooks", "📚", "Textbooks, novels, notes and study materials", [],
            [
                "Engineering", "Computer Science", "Business", "Accounting", "Law",
                "Mathematics", "Science", "Research Materials", "Novels", "Other"
            ]),

        Def("Laptops & Computers", "💻", "Laptops, desktops, monitors and computers", [],
            [
                "Laptop", "Desktop Computer", "Monitor", "Keyboard", "Mouse",
                "Computer Parts", "Other"
            ]),

        Def("Phones & Tablets", "📱", "Smartphones, iPhones, Androids and tablets", [],
            [
                "Smartphone", "iPhone", "Android Phone", "Tablet", "iPad", "Other"
            ]),

        Def("Smartwatches", "⌚", "Apple Watch, Samsung, Fitbit, Garmin and more",
            ["Smart Watches", "Smart Watch"],
            [
                "Apple Watch", "Samsung Galaxy Watch", "Fitbit", "Garmin",
                "Xiaomi / Mi Band", "Huawei Watch", "Other Smartwatches"
            ]),

        Def("Electronics", "🔌", "Speakers, headphones, TVs, cameras and more", [],
            [
                "Headphones", "Earbuds", "Speakers", "Television", "Camera", "Other"
            ]),

        Def("Computer Accessories", "🖱️", "Keyboards, mice, drives, webcams and more", [],
            [
                "Keyboard", "Mouse", "Laptop Stand", "USB Hub", "Webcam",
                "Cooling Pad", "External Hard Drive", "Flash Drive", "Other"
            ]),

        Def("Chargers & Cables", "🔋", "Phone chargers, laptop chargers and cables", [],
            [
                "Phone Charger", "Laptop Charger", "USB Cable", "Type-C Cable",
                "Lightning Cable", "HDMI Cable", "Extension Cable", "Other"
            ]),

        Def("Power Banks", "⚡", "Portable power banks and charging devices", [],
            ["10000mAh", "20000mAh", "30000mAh", "Other"]),

        Def("Stationery & School Supplies", "✏️", "Notebooks, pens, files and school supplies",
            ["Stationery"],
            [
                "Notebooks", "Pens", "Pencils", "Markers", "Files & Folders",
                "Drawing Materials", "School Supplies", "Other"
            ]),

        Def("Calculators", "🧮", "Scientific, financial and engineering calculators", [],
            [
                "Scientific Calculator", "Financial Calculator",
                "Graphing Calculator", "Other"
            ]),

        Def("Backpacks & School Bags", "🎒", "Backpacks, laptop bags and school bags",
            ["Backpacks"],
            [
                "Laptop Backpack", "School Backpack", "Travel Backpack", "Other"
            ]),

        Def("Hostel Essentials", "🏠", "Beds, tables, chairs, kitchen items and more", [],
            [
                "Mattress", "Pillow", "Bedsheets", "Blanket", "Mosquito Net",
                "Curtains", "Storage Box", "Other"
            ]),

        Def("Furniture", "🪑", "Chairs, desks, beds, wardrobes and more", [],
            ["Chair", "Table", "Desk", "Bed", "Wardrobe", "Shelf", "Other"]),

        Def("Kitchen Appliances", "🍳", "Microwaves, blenders, kettles and more", [],
            [
                "Microwave", "Blender", "Rice Cooker", "Electric Cooker",
                "Kettle", "Fridge", "Hot Plate", "Other"
            ]),

        Def("Printers & Accessories", "🖨️", "Printers, ink, toner and accessories", [],
            ["Printer", "Ink", "Toner", "Printer Cable", "Other"]),

        Def("Laboratory Equipment", "🔬", "Lab coats, goggles and measuring equipment", [],
            [
                "Lab Coat", "Safety Goggles", "Calculator", "Measuring Equipment",
                "Laboratory Materials", "Other"
            ]),

        Def("Sports & Fitness", "⚽", "Sports gear, gym equipment and activewear", [],
            [
                "Football", "Basketball", "Running Shoes", "Gym Equipment",
                "Sports Wear", "Other"
            ]),

        Def("Bicycles & Transportation", "🚲", "Bicycles, parts and helmets", [],
            ["Bicycle", "Bicycle Parts", "Helmet", "Other"]),

        Def("Gaming", "🎮", "Consoles, controllers, headsets and accessories", [],
            [
                "PlayStation", "Xbox", "Nintendo", "Gaming Controller",
                "Gaming Headset", "Gaming Accessories", "Other"
            ]),

        Def("Beauty & Personal Care", "💄", "Hair, skincare and personal care items", [],
            [
                "Hair Products", "Skincare", "Hair Dryer", "Trimmer",
                "Personal Care", "Other"
            ]),

        Def("Cleaning & Laundry", "🧺", "Irons, laundry baskets and cleaning supplies", [],
            [
                "Iron", "Ironing Board", "Bucket", "Laundry Basket",
                "Cleaning Equipment", "Other"
            ]),

        Def("Other Student Items", "📦", "Miscellaneous student essentials", [],
            ["Student Equipment", "Hostel Item", "School Item", "Other"])
    ];

    private static readonly Dictionary<string, MarketplaceCategoryDefinition> Lookup =
        BuildLookup();

    public static IReadOnlyList<MarketplaceCategoryDefinition> All =>
        AllDefinitions;

    public static IReadOnlyList<string> CategoryNames =>
        AllDefinitions.Select(d => d.Name).ToList();

    public static string? Normalize(string? rawCategory)
    {
        if (string.IsNullOrWhiteSpace(rawCategory))
        {
            return null;
        }

        var trimmed = rawCategory.Trim();

        if (Lookup.TryGetValue(trimmed, out var definition))
        {
            return definition.Name;
        }

        return trimmed;
    }

    public static MarketplaceCategoryDefinition? Find(string? nameOrAlias)
    {
        if (string.IsNullOrWhiteSpace(nameOrAlias))
        {
            return null;
        }

        Lookup.TryGetValue(nameOrAlias.Trim(), out var definition);
        return definition;
    }

    public static IReadOnlyList<string> GetMatchValues(string? nameOrAlias)
    {
        var definition = Find(nameOrAlias);

        if (definition == null)
        {
            return string.IsNullOrWhiteSpace(nameOrAlias)
                ? Array.Empty<string>()
                : [nameOrAlias.Trim().ToLowerInvariant()];
        }

        return definition.Aliases
            .Select(a => a.ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    public static bool ProductMatchesCategory(
        string? productCategory,
        string? filterCategory)
    {
        if (string.IsNullOrWhiteSpace(productCategory) ||
            string.IsNullOrWhiteSpace(filterCategory))
        {
            return false;
        }

        var definition = Find(filterCategory);

        if (definition == null)
        {
            return string.Equals(
                productCategory,
                filterCategory,
                StringComparison.OrdinalIgnoreCase);
        }

        return definition.Aliases.Any(alias =>
            string.Equals(alias, productCategory, StringComparison.OrdinalIgnoreCase));
    }

    public static List<string> GetSubcategories(string? category)
    {
        var definition = Find(category);
        return definition?.Subcategories.ToList() ?? [];
    }

    public static IReadOnlyList<string> GetUnlistedProductCategories(
        IEnumerable<Product> products)
    {
        return products
            .Select(p => p.Category)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(c => Find(c) == null)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static MarketplaceCategoryDefinition Def(
        string name,
        string icon,
        string description,
        string[] extraAliases,
        string[] subcategories)
    {
        var aliases = new List<string> { name };
        aliases.AddRange(extraAliases);

        return new MarketplaceCategoryDefinition(
            name,
            icon,
            description,
            aliases
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            subcategories);
    }

    private static Dictionary<string, MarketplaceCategoryDefinition> BuildLookup()
    {
        var lookup =
            new Dictionary<string, MarketplaceCategoryDefinition>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var definition in AllDefinitions)
        {
            foreach (var alias in definition.Aliases)
            {
                lookup[alias] = definition;
            }
        }

        return lookup;
    }
}

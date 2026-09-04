using System.Text.RegularExpressions;

namespace ThriftHub.Services;

public sealed class SiteHelpReply
{
    public string Answer { get; init; } = string.Empty;

    public IReadOnlyList<string> Suggestions { get; init; } =
        Array.Empty<string>();
}

public static class SiteHelpChatService
{
    private sealed record HelpRule(
        Regex Pattern,
        string Answer,
        IReadOnlyList<string> Suggestions);

    private static readonly HelpRule[] Rules =
    [
        Rule(
            @"\b(buy|purchase|order|shop|find item)\b",
            "Browse the Marketplace, open any listing, and tap Message Seller to ask about price, size, and pickup or delivery. When you agree on terms, arrange payment and collection directly with the seller.",
            ["How do I sell?", "Is ThriftHub safe?", "What categories are available?"]),

        Rule(
            @"\b(sell|post|list|upload|become seller|seller subscription)\b",
            "Go to Dashboard, become a seller, and activate a seller subscription. Then open Sell an Item, choose a category, add at least 5 photos, set your price, and post. Buyers will message you from your listing.",
            ["How much is seller subscription?", "What photos do I need?", "How do reviews work?"]),

        Rule(
            @"\b(subscription|subscribe|fee|cost|price.*sell)\b",
            "Sellers need an active subscription before posting items. Open Subscription from your dashboard to see current plans and payment options for your account.",
            ["How do I sell?", "How do I message a seller?", "Help with categories"]),

        Rule(
            @"\b(category|categories|fashion|sportswear|jogger|sock|shoe|bag)\b",
            "ThriftHub includes fashion categories such as Women's Fashion, Men's Fashion, Sportswear & Activewear, Joggers & Tracksuits, Socks & Hosiery, Hoodies, Shoes, Bags, and more — plus student essentials like books, laptops, and hostel items.",
            ["How do I filter by category?", "How do I sell clothes?", "What sizes can I add?"]),

        Rule(
            @"\b(review|rating|star|feedback)\b",
            "On any product page, signed-in buyers can leave a star rating and short comment. One review per account per listing; you can update your review later.",
            ["How do I buy safely?", "How do I report a seller?", "How do I message a seller?"]),

        Rule(
            @"\b(photo|picture|image|upload.*5|five photo)\b",
            "When posting an item, upload between 5 and 8 clear photos of the actual product. Buyers can swipe through all photos on the listing page.",
            ["How do I sell?", "What categories are available?", "What condition options exist?"]),

        Rule(
            @"\b(message|chat|contact seller|talk to seller)\b",
            "Open a product and tap Message Seller, or go to Messages in the menu. You can send text, images, and voice notes. Typing indicators show when the other person is replying.",
            ["How do I buy?", "How do I block someone?", "Is my chat private?"]),

        Rule(
            @"\b(safe|safety|scam|trust|verify|verified)\b",
            "Meet in safe public places, inspect items before paying, and use Message Seller to keep conversation on ThriftHub. You can block or report users from a listing or chat. Verified sellers have completed identity checks.",
            ["How do I report?", "How do I block?", "How do reviews work?"]),

        Rule(
            @"\b(report|block|abuse|harass)\b",
            "Use Report Seller on a product page or the safety options in chat if something feels wrong. Block Seller stops further messages from that account.",
            ["Is ThriftHub safe?", "How do I message support?", "Privacy policy"]),

        Rule(
            @"\b(wishlist|favorite|save item|heart)\b",
            "Tap the heart on a marketplace listing to save it to your wishlist. Sign in to keep favorites synced to your account.",
            ["How do I buy?", "How do I find fashion items?", "How do categories work?"]),

        Rule(
            @"\b(view|views|how many people)\b",
            "Each listing shows how many people have viewed it. View counts update on the product details page.",
            ["How do I sell?", "How do reviews work?", "What is ThriftHub?"]),

        Rule(
            @"\b(privacy|data|personal information)\b",
            "Read our Privacy Policy at /Home/Privacy. It explains what we collect, how messages and listings work, and your choices.",
            ["Is ThriftHub safe?", "How do I delete my account?", "How do I sell?"]),

        Rule(
            @"\b(account|register|sign up|login|log in)\b",
            "Tap Register to create an account, or Login if you already have one. Sellers and buyers use the same account; switch to seller mode from the dashboard when ready.",
            ["How do I sell?", "How do I reset password?", "What is ThriftHub?"]),

        Rule(
            @"\b(what is|about|thrifthub|help)\b",
            "ThriftHub is a student-friendly marketplace for fashion, electronics, books, hostel essentials, and more. Browse listings, message sellers, save favorites, and leave reviews.",
            ["How do I buy?", "How do I sell?", "What categories are available?"])
    ];

    public static SiteHelpReply GetReply(string? question)
    {
        var text = (question ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            return new SiteHelpReply
            {
                Answer =
                    "Hi! I can help with buying, selling, categories, photos, reviews, messaging, and safety on ThriftHub. What would you like to know?",
                Suggestions =
                [
                    "How do I buy an item?",
                    "How do I sell on ThriftHub?",
                    "What fashion categories are there?",
                    "How do reviews work?"
                ]
            };
        }

        var normalized = text.ToLowerInvariant();

        foreach (var rule in Rules)
        {
            if (rule.Pattern.IsMatch(normalized))
            {
                return new SiteHelpReply
                {
                    Answer = rule.Answer,
                    Suggestions = rule.Suggestions
                };
            }
        }

        return new SiteHelpReply
        {
            Answer =
                "I'm not sure about that yet. Try asking about buying, selling, categories, uploading photos, reviews, messaging, or account safety — or browse the Marketplace and message a seller directly.",
            Suggestions =
            [
                "How do I buy?",
                "How do I sell?",
                "What categories are available?",
                "How do I message a seller?"
            ]
        };
    }

    private static HelpRule Rule(
        string pattern,
        string answer,
        IReadOnlyList<string> suggestions)
    {
        return new HelpRule(
            new Regex(
                pattern,
                RegexOptions.IgnoreCase
                | RegexOptions.CultureInvariant
                | RegexOptions.Compiled),
            answer,
            suggestions);
    }
}

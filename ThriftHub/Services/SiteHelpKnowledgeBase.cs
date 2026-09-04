namespace ThriftHub.Services;

public sealed record SiteHelpTopic(
    string Id,
    string[] Triggers,
    string Answer,
    string[] Suggestions,
    int Priority = 0);

public static class SiteHelpKnowledgeBase
{
    public const string SiteContext =
        """
        ThriftHub (thrifthubgh.com) is a Ghana student-friendly marketplace for fashion, electronics, books, hostel items, and more.
        Currency: Ghana Cedis (GH₵). Support: thrifthub372@gmail.com, phone 0533768469, WhatsApp 0503173863.
        Key routes: Marketplace /MarketPlace/Index, Sell /Seller/Create, Messages /Messages/Index, Wishlist /Favorites/Index,
        Dashboard /Dashboard/Index, Subscription /Subscription/Index, Privacy /Home/Privacy, Contact /ContactAdmin/Index.
        Buying is mainly: browse → open listing → Message Seller → agree offline payment & pickup.
        Selling requires: seller account → ID verification → active subscription → post with 5-8 photos.
        Subscription plans (4 months via Paystack): Welcome Trial free 1 month, Basic GH₵40, Standard GH₵80, Premium GH₵150.
        """;

    public static IReadOnlyList<SiteHelpTopic> Topics { get; } =
    [
        Topic(
            "about",
            ["what is thrifthub", "about thrifthub", "tell me about", "what do you do", "who are you"],
            "ThriftHub is a student-friendly marketplace in Ghana for thrift fashion, electronics, textbooks, hostel essentials, and more. Browse listings, save favorites, message sellers directly, and leave reviews — all in one place built for university life.",
            ["How do I buy?", "How do I sell?", "What categories exist?", "Is it safe?"],
            2),

        Topic(
            "buy",
            ["how do i buy", "how to buy", "purchase", "order item", "get an item", "shop for", "find item", "looking to buy"],
            "Browse the Marketplace, open a listing you like, and tap Message Seller. Ask about size, condition, price, and pickup or delivery. When you agree, pay and collect directly with the seller. You can also save items to your wishlist with the heart icon while you decide.",
            ["How do I message a seller?", "How do wishlists work?", "Is buying safe?", "What are reviews?"],
            3),

        Topic(
            "sell",
            ["how do i sell", "how to sell", "post item", "list item", "become seller", "start selling", "sell on thrifthub"],
            "Register or log in → Dashboard → Become Seller → submit ID verification → wait for admin approval → activate a subscription (Welcome Trial or paid plan) → open Sell an Item. Add 5–8 clear photos, pick category & subcategory, set price, condition, and sizes, then post.",
            ["What subscription do I need?", "How does verification work?", "How many photos?", "What categories?"],
            3),

        Topic(
            "subscription",
            ["subscription", "subscribe", "seller plan", "sell fee", "cost to sell", "pricing plan", "paystack", "how much to sell", "basic plan", "premium plan", "standard plan", "welcome trial"],
            "Sellers need an active subscription to post. Plans (billed every 4 months via Paystack): Welcome Trial — free for your first month; Basic — GH₵40; Standard — GH₵80 (verification badge, better visibility); Premium — GH₵150 (featured listings, premium visibility). Open Subscription from your dashboard after seller approval.",
            ["How do I sell?", "How does verification work?", "What payment methods?", "Contact support"],
            3),

        Topic(
            "verification",
            ["verification", "verify", "verified", "id card", "identity", "ghana card", "seller approval", "pending verification", "verification rejected", "submit id"],
            "To sell, submit a government ID (Ghana Card, Passport, Driver's License, or Voter ID) at Seller Verification. Admin reviews it privately — your ID number and documents are never public. Once approved, you can subscribe and post items. Verified sellers show a badge on their profile.",
            ["How do I sell?", "How long does approval take?", "Contact support", "What if rejected?"],
            2),

        Topic(
            "categories",
            ["categor", "fashion", "sportswear", "activewear", "jogger", "tracksuit", "sock", "hosiery", "hoodie", "sweatshirt", "shoe", "bag", "swimwear", "vintage", "traditional wear", "kente", "textbook", "laptop", "phone", "hostel", "electronics"],
            "ThriftHub has 38+ categories including Women's/Men's/Kids Fashion, Sportswear & Activewear, Joggers & Tracksuits, Socks & Hosiery, Hoodies, Shoes, Bags, T-Shirts & Polos, Coats, Swimwear, Vintage & Thrift Fashion, Books & Textbooks, Laptops, Phones, Hostel Essentials, and more. Filter by category and subcategory on the Marketplace, or pick the exact category when posting.",
            ["How do I sell clothes?", "How do I find joggers?", "How do filters work?", "How do I buy?"],
            2),

        Topic(
            "filter-search",
            ["search", "filter", "find cheap", "price range", "min price", "max price", "sort", "looking for"],
            "On the Marketplace, use the search bar for names and descriptions, pick a category and subcategory from the filters, and set min/max price in GH₵. Great for finding specific items like joggers, textbooks, or laptops within your budget.",
            ["What categories exist?", "How do I buy?", "How do wishlists work?", "How do I sell?"],
            1),

        Topic(
            "photos",
            ["photo", "picture", "image", "upload", "5 photo", "five photo", "gallery", "multiple photo"],
            "Every new listing needs 5–8 clear photos of the actual item (JPG, PNG, or WEBP, max 10 MB each). The first photo is the cover. Buyers can swipe through all photos on the product page. Good lighting and multiple angles help items sell faster.",
            ["How do I sell?", "What condition options?", "How do reviews work?", "Tips for selling"],
            2),

        Topic(
            "reviews",
            ["review", "rating", "star", "feedback", "rate seller", "rate product", "comment on"],
            "Signed-in buyers can leave 1–5 stars and an optional comment on any product page. One review per account per listing — you can update it later. You cannot review your own item. Average rating and all reviews show on the listing for other buyers.",
            ["How do I buy safely?", "How do I report a seller?", "What is verified?", "How do I sell?"],
            2),

        Topic(
            "messaging",
            ["message", "chat", "contact seller", "talk to seller", "inbox", "conversation", "dm", "direct message"],
            "Tap Message Seller on any listing, or open Messages from the menu. Send text, images, files, and voice notes. Typing indicators show when someone is replying. Keep deals on ThriftHub chat so you have a record if something goes wrong.",
            ["How do I buy?", "Voice notes?", "How do I block?", "Notifications?"],
            3),

        Topic(
            "voice",
            ["voice note", "voice message", "audio message", "record message", "microphone", "voice call", "call seller"],
            "In Messages, tap the microphone to record and send voice notes — duration shows on the bubble. You can also start a voice call in chat for quick questions about an item. Allow microphone access when your browser asks.",
            ["How do I message?", "How do I buy?", "Is chat private?", "How do notifications work?"],
            2),

        Topic(
            "wishlist",
            ["wishlist", "favorite", "favourites", "heart", "save item", "saved items"],
            "Tap the heart on any marketplace listing to save it. Open Wishlist from the menu to see everything you saved. Sold items are hidden automatically. Sign in so favorites sync across devices.",
            ["How do I buy?", "How do I find fashion?", "How do categories work?", "How do I sell?"],
            2),

        Topic(
            "views",
            ["view count", "views", "how many people viewed", "popular listing", "traffic"],
            "Each listing shows how many people viewed it on the product details page and cards. View counts update automatically. Sellers can use this to see which listings get the most interest.",
            ["How do I sell better?", "How do reviews work?", "How do I post?", "Subscription plans"],
            1),

        Topic(
            "safety",
            ["safe", "safety", "scam", "trust", "secure", "fraud", "meet up", "pickup", "payment safe"],
            "Stay safe by: meeting in busy public places on campus or in town, inspecting the item before paying, keeping chat on ThriftHub, checking seller reviews, and preferring verified sellers. Never send money before you are confident. Block or report anyone suspicious.",
            ["How do I report?", "How do I block?", "What is verified?", "Contact support"],
            3),

        Topic(
            "report-block",
            ["report", "block", "abuse", "harass", "inappropriate", "spam seller", "fake listing"],
            "Report Seller is on every product page; you can also report from Safety in the menu. Choose a reason (scam, fake product, harassment, spam, etc.). Block Seller stops them from messaging you. View your reports at Safety → My Reports.",
            ["Is ThriftHub safe?", "Contact support", "Privacy policy", "How do I message?"],
            2),

        Topic(
            "account",
            ["register", "sign up", "create account", "login", "log in", "sign in", "password", "forgot password", "reset password", "logout", "log out"],
            "Tap Register to create an account with email and password, then verify your email with the 6-digit code. Login anytime from the menu. Forgot password sends a reset link to your email. One account works for both buying and selling.",
            ["How do I sell?", "How do I verify email?", "Apple sign in?", "Contact support"],
            2),

        Topic(
            "profile",
            ["profile", "display name", "profile photo", "avatar", "edit profile", "my account"],
            "Open Profile from the menu to view your account. From Dashboard you can update your display name (shown on listings and chat) and upload a profile photo. Sellers also have a public seller profile page buyers can visit.",
            ["How do I sell?", "How do I become seller?", "How do I message?", "Privacy policy"],
            1),

        Topic(
            "notifications",
            ["notification", "alert", "badge", "unread", "notify"],
            "Notifications appear in the bell/menu center for new messages and important updates. The badge count updates in real time. Open Notifications to mark items read or clear them.",
            ["How do messages work?", "How do I buy?", "Voice notes?", "Dashboard"],
            1),

        Topic(
            "orders",
            ["checkout", "order", "place order", "my orders", "delivery", "commission", "cancel order"],
            "Some listings support in-app checkout at Order → Checkout with delivery details. ThriftHub takes a 5% commission on those orders. You can view order history at My Orders and cancel pending orders. Many buyers still prefer messaging the seller directly — both paths exist.",
            ["How do I buy?", "How do I message seller?", "Contact support", "Payment methods"],
            1),

        Topic(
            "privacy",
            ["privacy", "data", "personal information", "what do you collect", "cookies", "delete account", "gdpr"],
            "Read the full Privacy Policy at /Home/Privacy. Summary: we collect account info, listings, messages, and seller ID docs for verification. Public: seller display name, photo, and listings. Private: email, password, and ID documents. To delete your account, email thrifthub372@gmail.com.",
            ["Is it safe?", "How does verification work?", "Contact support", "What is public?"],
            2),

        Topic(
            "contact",
            ["contact", "support", "help me", "customer service", "admin", "email support", "phone", "whatsapp", "thrifthub372"],
            "For account-specific issues, verification delays, payments, or safety concerns, contact support: Email thrifthub372@gmail.com, Phone 0533768469, WhatsApp 0503173863. You can also visit Contact Admin from the menu.",
            ["How do I sell?", "Verification pending?", "Report a user", "Privacy policy"],
            2),

        Topic(
            "condition-sizes",
            ["condition", "brand new", "like new", "used", "size", "sizes", "xl", "uk size", "one size"],
            "When posting, pick a condition: Brand New, Like New, Very Good, Good, Fair, or Used. For fashion and shoes, select available sizes from presets or add custom sizes (e.g. 32, 2XL). Non-clothing items can use One Size where it fits.",
            ["How do I sell?", "What categories?", "How many photos?", "How do I buy?"],
            1),

        Topic(
            "manage-listings",
            ["my products", "my listings", "mark sold", "sold out", "relist", "edit listing", "delete listing", "manage product"],
            "Sellers manage listings at My Products. Mark as Sold when an item sells; you can edit price and sizes then mark available again, or delete the listing entirely. View counts and messages help you track interest.",
            ["How do I sell?", "How do messages work?", "How do photos work?", "Subscription"],
            1),

        Topic(
            "dashboard",
            ["dashboard", "home page", "my dashboard", "where do i start"],
            "After login, Dashboard is your hub: browse shortcuts, become a seller, manage subscription, open Messages, Notifications, Profile, and Safety tools. Customers and sellers see different quick actions based on account type.",
            ["How do I sell?", "How do I buy?", "Subscription", "Profile"],
            1),

        Topic(
            "pwa-install",
            ["install app", "add to home", "mobile app", "pwa", "home screen", "iphone app", "android app"],
            "ThriftHub works as a web app — no app store needed. On iPhone Safari or Android Chrome, use Add to Home Screen for quick access like a native app. Visit Install App from the menu for step-by-step instructions.",
            ["How do I buy?", "How do notifications work?", "What is ThriftHub?", "Contact support"],
            1),

        Topic(
            "suspended",
            ["suspended", "banned", "locked out", "access denied", "account disabled"],
            "If your account is suspended, you will see an Access Denied message with the reason. Contact thrifthub372@gmail.com to appeal or clarify. Suspensions usually follow safety reports or policy violations.",
            ["Contact support", "How do I report?", "Privacy policy", "Safety tips"],
            1),

        Topic(
            "apple-login",
            ["apple sign", "sign in with apple", "apple login", "external login"],
            "Sign in with Apple may be available if configured. Email and password registration always works. After Apple login, complete your profile from Dashboard if prompted.",
            ["How do I register?", "Forgot password?", "Profile", "Contact support"],
            0),

        Topic(
            "sell-tips",
            ["sell faster", "tips for selling", "more buyers", "better listing", "pricing advice", "how to price"],
            "Great listings use 5–8 clear photos, honest descriptions (brand, flaws, size, location), fair pricing, and the right category. Reply quickly in Messages. Standard or Premium plans give more visibility. Mark items sold promptly so buyers trust your profile.",
            ["How do I sell?", "How many photos?", "Subscription plans", "How do reviews work?"],
            1),

        Topic(
            "buy-tips",
            ["negotiate", "bargain", "ask for discount", "best deal", "student budget", "cheap items"],
            "Message the seller politely to ask about price, bundle deals, or campus pickup to save delivery cost. Check reviews and view counts. Save items to your wishlist and compare before buying. Meet in person to inspect quality before paying.",
            ["How do I buy?", "Is it safe?", "How do reviews work?", "How do I message?"],
            1),

        Topic(
            "campus",
            ["campus", "university", "student", "hostel", "dorm", "legon", "knust", "campus pickup"],
            "ThriftHub is built for students — textbooks, laptops, hostel mattresses, fashion, and more. Mention your campus or hostel in messages for easier pickup. Many sellers are on the same campus as buyers.",
            ["What categories?", "How do I buy?", "How do I sell?", "Hostel essentials"],
            1),

        Topic(
            "greeting",
            ["hello", "hi ", "hey", "good morning", "good afternoon", "good evening", "what's up", "whats up"],
            "Hello! I'm the ThriftHub assistant. I can help with buying, selling, categories, photos, reviews, messaging, subscriptions, verification, safety, and anything else about the site. What would you like to know?",
            ["How do I buy?", "How do I sell?", "What categories exist?", "Is it safe?"],
            0),

        Topic(
            "thanks",
            ["thank you", "thanks", "appreciate", "helpful", "great help"],
            "You're welcome! Happy to help anytime. If you need hands-on support, contact thrifthub372@gmail.com or use Contact Admin in the menu.",
            ["How do I sell?", "How do I buy?", "Contact support", "What is ThriftHub?"],
            0)
    ];

    private static SiteHelpTopic Topic(
        string id,
        string[] triggers,
        string answer,
        string[] suggestions,
        int priority = 0)
    {
        return new SiteHelpTopic(
            id,
            triggers,
            answer,
            suggestions,
            priority);
    }
}

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ThriftHub.Services;

public sealed class SiteHelpReply
{
    public string Answer { get; init; } = string.Empty;

    public IReadOnlyList<string> Suggestions { get; init; } =
        Array.Empty<string>();
}

public class SiteHelpChatService
{
    private const int StrongMatchScore = 12;
    private const int MediumMatchScore = 6;

    private static readonly Dictionary<string, string[]> Synonyms =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["buy"] = ["purchase", "shop", "order", "get"],
            ["sell"] = ["post", "list", "upload", "listing"],
            ["message"] = ["chat", "dm", "contact", "text"],
            ["photo"] = ["picture", "image", "pic"],
            ["sock"] = ["socks", "hosiery"],
            ["jogger"] = ["joggers", "track pants", "tracksuit"],
            ["verify"] = ["verification", "verified", "identity"],
            ["safe"] = ["safety", "scam", "trust", "secure"],
            ["cheap"] = ["budget", "affordable", "low price"],
            ["fashion"] = ["clothes", "clothing", "wear", "outfit"],
            ["textbook"] = ["books", "text book", "course material"],
            ["hostel"] = ["dorm", "dormitory", "room"],
            ["subscription"] = ["plan", "fee", "paystack"],
            ["review"] = ["rating", "stars", "feedback"]
        };

    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SiteHelpChatService> _logger;

    public SiteHelpChatService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<SiteHelpChatService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SiteHelpReply> GetReplyAsync(
        string? question,
        CancellationToken cancellationToken = default)
    {
        var text = (question ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            return EmptyPromptReply();
        }

        var normalized = Normalize(text);
        var localReply = BuildLocalReply(text, normalized);

        if (localReply.Score >= StrongMatchScore)
        {
            return localReply.Reply;
        }

        var geminiKey = GetGeminiApiKey();

        if (!string.IsNullOrWhiteSpace(geminiKey))
        {
            try
            {
                var aiAnswer =
                    await AskGeminiAsync(
                        text,
                        geminiKey,
                        cancellationToken);

                if (!string.IsNullOrWhiteSpace(aiAnswer))
                {
                    return new SiteHelpReply
                    {
                        Answer = aiAnswer.Trim(),
                        Suggestions = localReply.Reply.Suggestions
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Gemini chatbot request failed; using local knowledge.");
            }
        }

        if (localReply.Score >= MediumMatchScore)
        {
            return localReply.Reply;
        }

        if (localReply.Score > 0)
        {
            return new SiteHelpReply
            {
                Answer =
                    "Here's the closest I found on ThriftHub: "
                    + localReply.Reply.Answer,
                Suggestions = localReply.Reply.Suggestions
            };
        }

        return BuildCreativeFallback(text, normalized);
    }

    private static SiteHelpReply EmptyPromptReply()
    {
        return new SiteHelpReply
        {
            Answer =
                "Hi! I'm the ThriftHub assistant. Ask me anything about buying, selling, categories, photos, reviews, messaging, subscriptions, verification, safety, or using the site.",
            Suggestions =
            [
                "How do I buy an item?",
                "How do I sell on ThriftHub?",
                "What fashion categories are there?",
                "How do I stay safe when buying?"
            ]
        };
    }

    private static ScoredReply BuildLocalReply(
        string originalQuestion,
        string normalizedQuestion)
    {
        var expandedQuestion =
            ExpandWithSynonyms(normalizedQuestion);

        var ranked =
            SiteHelpKnowledgeBase.Topics
                .Select(topic =>
                    new RankedTopic(
                        topic,
                        ScoreTopic(
                            topic,
                            normalizedQuestion,
                            expandedQuestion)))
                .Where(entry => entry.Score > 0)
                .OrderByDescending(entry => entry.Score)
                .ThenByDescending(entry => entry.Topic.Priority)
                .ToList();

        if (ranked.Count == 0)
        {
            var shoppingReply =
                TryShoppingIntentReply(originalQuestion, normalizedQuestion);

            if (shoppingReply != null)
            {
                return new ScoredReply(shoppingReply, MediumMatchScore);
            }

            var problemReply =
                TryProblemIntentReply(normalizedQuestion);

            if (problemReply != null)
            {
                return new ScoredReply(problemReply, MediumMatchScore);
            }

            return new ScoredReply(
                new SiteHelpReply
                {
                    Answer = string.Empty,
                    Suggestions = DefaultSuggestions()
                },
                0);
        }

        var best = ranked[0];

        if (ranked.Count >= 2 &&
            ranked[1].Score >= best.Score * 0.75)
        {
            var combinedAnswer =
                best.Topic.Answer
                + " "
                + ranked[1].Topic.Answer;

            return new ScoredReply(
                new SiteHelpReply
                {
                    Answer = combinedAnswer.Trim(),
                    Suggestions = MergeSuggestions(
                        best.Topic.Suggestions,
                        ranked[1].Topic.Suggestions)
                },
                best.Score + ranked[1].Score);
        }

        return new ScoredReply(
            new SiteHelpReply
            {
                Answer = best.Topic.Answer,
                Suggestions = best.Topic.Suggestions
            },
            best.Score);
    }

    private static SiteHelpReply BuildCreativeFallback(
        string originalQuestion,
        string normalizedQuestion)
    {
        if (IsOffSiteQuestion(normalizedQuestion))
        {
            return new SiteHelpReply
            {
                Answer =
                    "I'm focused on ThriftHub — buying, selling, and using our marketplace. I can't help with that topic, but I can guide you through listings, messages, subscriptions, safety, and more on thrifthubgh.com.",
                Suggestions = DefaultSuggestions()
            };
        }

        if (ContainsAny(
            normalizedQuestion,
            "should i",
            "is it okay",
            "what if",
            "can i",
            "would you recommend",
            "do you think"))
        {
            return new SiteHelpReply
            {
                Answer =
                    "Good question. On ThriftHub, the smart move is usually: check the listing photos and description, read seller reviews, message the seller with any questions, meet safely if buying in person, and inspect before you pay. For selling, use honest photos (5–8), fair pricing, and reply quickly in chat. Want specifics on buying, selling, or safety?",
                Suggestions =
                [
                    "How do I buy safely?",
                    "Tips for selling faster",
                    "How do reviews work?",
                    "Contact support"
                ]
            };
        }

        return new SiteHelpReply
        {
            Answer =
                "I may not have an exact answer to \""
                + TrimForDisplay(originalQuestion)
                + "\", but I know ThriftHub inside out — marketplace, selling, subscriptions, verification, messages, voice notes, wishlists, reviews, and safety. Try rephrasing, or pick a topic below. For account-specific help, email thrifthub372@gmail.com.",
            Suggestions = DefaultSuggestions()
        };
    }

    private static SiteHelpReply? TryShoppingIntentReply(
        string originalQuestion,
        string normalizedQuestion)
    {
        if (!ContainsAny(
            normalizedQuestion,
            "looking for",
            "need a",
            "need some",
            "where can i find",
            "where do i find",
            "want to buy",
            "searching for"))
        {
            return null;
        }

        var categoryHint = DetectCategoryHint(normalizedQuestion);

        var categoryText =
            string.IsNullOrWhiteSpace(categoryHint)
                ? "Open the Marketplace and use search plus category filters to narrow results."
                : $"Try Marketplace, filter by {categoryHint}, or search for \"{ExtractSearchTerm(originalQuestion)}\".";

        return new SiteHelpReply
        {
            Answer =
                "Sounds like you're shopping — "
                + categoryText
                + " Open listings you like, tap the heart to save them, and Message Seller to confirm size, price, and pickup before you pay.",
            Suggestions =
            [
                "How do I buy?",
                "What categories exist?",
                "How do wishlists work?",
                "How do I message a seller?"
            ]
        };
    }

    private static SiteHelpReply? TryProblemIntentReply(
        string normalizedQuestion)
    {
        if (ContainsAny(
            normalizedQuestion,
            "not working",
            "doesn't work",
            "cant ",
            "can't ",
            "unable",
            "problem",
            "issue",
            "error",
            "failed",
            "stuck"))
        {
            if (ContainsAny(
                normalizedQuestion,
                "login",
                "password",
                "sign in"))
            {
                return new SiteHelpReply
                {
                    Answer =
                        "Login trouble? Use Forgot Password on the login page for a reset link. Make sure your email is verified. If you're suspended, you'll see Access Denied — email thrifthub372@gmail.com for help.",
                    Suggestions =
                    [
                        "Forgot password?",
                        "Contact support",
                        "Account suspended?",
                        "How do I register?"
                    ]
                };
            }

            if (ContainsAny(
                normalizedQuestion,
                "verification",
                "verify",
                "id",
                "seller"))
            {
                return new SiteHelpReply
                {
                    Answer =
                        "Verification issues usually mean your ID is still pending review or needs clearer photos. Check Seller Verification in your dashboard. If rejected, resubmit a clear Ghana Card, Passport, Driver's License, or Voter ID. Contact thrifthub372@gmail.com if it's been waiting long.",
                    Suggestions =
                    [
                        "How does verification work?",
                        "How do I sell?",
                        "Contact support",
                        "Subscription plans"
                    ]
                };
            }

            if (ContainsAny(
                normalizedQuestion,
                "pay",
                "subscription",
                "paystack"))
            {
                return new SiteHelpReply
                {
                    Answer =
                        "Payment problem? Make sure you're an approved seller before subscribing. Open Subscription from Dashboard and complete Paystack checkout. If payment succeeded but access didn't update, contact thrifthub372@gmail.com with your account email.",
                    Suggestions =
                    [
                        "Subscription plans",
                        "How do I sell?",
                        "Contact support",
                        "Seller verification"
                    ]
                };
            }

            if (ContainsAny(
                normalizedQuestion,
                "message",
                "chat",
                "voice"))
            {
                return new SiteHelpReply
                {
                    Answer =
                        "Chat issues? Refresh the page, check your internet, and allow microphone permission for voice notes. Open Messages from the menu and try again. If a user blocked you or was suspended, messages won't go through.",
                    Suggestions =
                    [
                        "How do messages work?",
                        "Voice notes?",
                        "How do I block someone?",
                        "Contact support"
                    ]
                };
            }

            return new SiteHelpReply
            {
                Answer =
                    "Sorry you're having trouble. Refresh the page, log out and back in, and try again. For account, payment, or verification issues, email thrifthub372@gmail.com or WhatsApp 0503173863 with your account email and a screenshot if possible.",
                Suggestions =
                [
                    "Contact support",
                    "How do I sell?",
                    "Login help",
                    "Verification help"
                ]
            };
        }

        return null;
    }

    private async Task<string?> AskGeminiAsync(
        string question,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var client =
            _httpClientFactory.CreateClient("Gemini");

        var model =
            _configuration["Chatbot:GeminiModel"]
            ?? "gemini-2.0-flash";

        var url =
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(apiKey)}";

        var systemPrompt =
            """
            You are ThriftHub's friendly on-site assistant for thrifthubgh.com — a Ghana student marketplace.
            Answer ONLY about ThriftHub: buying, selling, categories, listings, photos, reviews, messaging, voice notes, wishlists, subscriptions, Paystack payments, seller verification, safety, privacy, notifications, dashboard, and support contact.
            Be conversational, practical, and willing to think creatively — give advice, tips, and step-by-step guidance even when the question is unusual, as long as it relates to using the site.
            If asked something unrelated to ThriftHub, politely redirect to site topics.
            Keep answers concise (2-5 sentences). Use GH₵ for prices. Do not invent features that don't exist.
            Support email: thrifthub372@gmail.com | Phone: 0533768469 | WhatsApp: 0503173863.

            Site knowledge:
            """
            + SiteHelpKnowledgeBase.SiteContext;

        var payload = new
        {
            systemInstruction = new
            {
                parts = new[]
                {
                    new { text = systemPrompt }
                }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = question }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.65,
                maxOutputTokens = 512
            }
        };

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                url);

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        request.Content =
            new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

        using var response =
            await client.SendAsync(
                request,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            _logger.LogWarning(
                "Gemini API returned {StatusCode}: {Body}",
                (int)response.StatusCode,
                errorBody);

            return null;
        }

        await using var stream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken);

        using var document =
            await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);

        var candidates =
            document.RootElement.GetProperty("candidates");

        if (candidates.GetArrayLength() == 0)
        {
            return null;
        }

        var parts =
            candidates[0]
                .GetProperty("content")
                .GetProperty("parts");

        if (parts.GetArrayLength() == 0)
        {
            return null;
        }

        return parts[0]
            .GetProperty("text")
            .GetString();
    }

    private string? GetGeminiApiKey()
    {
        return _configuration["Chatbot:GeminiApiKey"]
            ?? Environment.GetEnvironmentVariable(
                "ThriftHub__Chatbot__GeminiApiKey");
    }

    private static int ScoreTopic(
        SiteHelpTopic topic,
        string normalizedQuestion,
        string expandedQuestion)
    {
        var score = 0;

        foreach (var trigger in topic.Triggers)
        {
            var normalizedTrigger =
                Normalize(trigger);

            if (string.IsNullOrWhiteSpace(normalizedTrigger))
            {
                continue;
            }

            if (normalizedQuestion.Contains(
                normalizedTrigger,
                StringComparison.Ordinal))
            {
                score += Math.Max(4, normalizedTrigger.Length);

                if (normalizedQuestion.StartsWith(
                    normalizedTrigger,
                    StringComparison.Ordinal))
                {
                    score += 3;
                }
            }
            else if (expandedQuestion.Contains(
                normalizedTrigger,
                StringComparison.Ordinal))
            {
                score += Math.Max(2, normalizedTrigger.Length / 2);
            }
            else if (FuzzyContains(
                normalizedQuestion,
                normalizedTrigger))
            {
                score += 2;
            }
        }

        foreach (var word in Tokenize(normalizedQuestion))
        {
            if (topic.Triggers.Any(trigger =>
                Normalize(trigger)
                    .Contains(word, StringComparison.Ordinal)))
            {
                score += 1;
            }
        }

        score += topic.Priority;

        return score;
    }

    private static bool FuzzyContains(
        string text,
        string trigger)
    {
        if (trigger.Length < 4)
        {
            return false;
        }

        var tokens = Tokenize(text);

        return tokens.Any(token =>
            token.Length >= 4 &&
            (trigger.Contains(token, StringComparison.Ordinal)
             || token.Contains(trigger, StringComparison.Ordinal)));
    }

    private static string ExpandWithSynonyms(string text)
    {
        var builder = new StringBuilder(text);

        foreach (var pair in Synonyms)
        {
            if (!text.Contains(pair.Key, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var synonym in pair.Value)
            {
                builder.Append(' ');
                builder.Append(synonym);
            }
        }

        return builder.ToString();
    }

    private static string Normalize(string text)
    {
        var lowered =
            text.ToLowerInvariant();

        lowered =
            Regex.Replace(
                lowered,
                @"[^\p{L}\p{N}\s&'-]",
                " ");

        return Regex.Replace(
            lowered,
            @"\s+",
            " ").Trim();
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        return text
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length > 2);
    }

    private static bool ContainsAny(
        string text,
        params string[] values)
    {
        return values.Any(value =>
            text.Contains(
                value,
                StringComparison.Ordinal));
    }

    private static bool IsOffSiteQuestion(string text)
    {
        return ContainsAny(
            text,
            "weather",
            "bitcoin",
            "crypto",
            "politics",
            "who is president",
            "write me an essay",
            "homework help",
            "recipe",
            "football score");
    }

    private static string DetectCategoryHint(string text)
    {
        var hints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["jogger"] = "Joggers & Tracksuits",
            ["track"] = "Joggers & Tracksuits",
            ["sock"] = "Socks & Hosiery",
            ["sport"] = "Sportswear & Activewear",
            ["gym"] = "Sportswear & Activewear",
            ["hoodie"] = "Hoodies & Sweatshirts",
            ["sneaker"] = "Shoes",
            ["shoe"] = "Shoes",
            ["bag"] = "Bags",
            ["dress"] = "Women's Fashion",
            ["textbook"] = "Books & Textbooks",
            ["laptop"] = "Laptops & Computers",
            ["phone"] = "Phones & Tablets",
            ["hostel"] = "Hostel Essentials",
            ["mattress"] = "Hostel Essentials"
        };

        foreach (var pair in hints)
        {
            if (text.Contains(pair.Key, StringComparison.Ordinal))
            {
                return pair.Value;
            }
        }

        return string.Empty;
    }

    private static string ExtractSearchTerm(string text)
    {
        var trimmed = text.Trim();

        return trimmed.Length <= 40
            ? trimmed
            : trimmed[..40] + "...";
    }

    private static string TrimForDisplay(string text)
    {
        var singleLine =
            text
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();

        return singleLine.Length <= 80
            ? singleLine
            : singleLine[..80] + "...";
    }

    private static string[] DefaultSuggestions()
    {
        return
        [
            "How do I buy?",
            "How do I sell?",
            "What categories are available?",
            "Contact support"
        ];
    }

    private static string[] MergeSuggestions(
        IEnumerable<string> first,
        IEnumerable<string> second)
    {
        return first
            .Concat(second)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToArray();
    }

    private sealed record RankedTopic(
        SiteHelpTopic Topic,
        int Score);

    private sealed record ScoredReply(
        SiteHelpReply Reply,
        int Score);
}

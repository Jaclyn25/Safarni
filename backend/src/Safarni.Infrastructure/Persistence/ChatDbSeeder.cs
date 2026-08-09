using System;
using System.Linq;
using System.Threading.Tasks;
using ChatBotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Safarni.Infrastructure.Persistence;

namespace ChatBotApi.Infrastructure.Persistence;

public class ChatDbSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ChatDbSeeder> _logger;

    public ChatDbSeeder(ApplicationDbContext context, ILogger<ChatDbSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Checking database seeding status in TravelKnowledges...");

        try
        {
            if (!await _context.TravelKnowledges.AnyAsync())
            {
                _logger.LogInformation("Seeding TravelKnowledges database...");
                _context.TravelKnowledges.AddRange(
                    new TravelKnowledge
                    {
                        Category = "City",
                        Title = "Paris / باريس",
                        Country = "France / فرنسا",
                        City = "Paris / باريس",
                        Content = "The capital of France, globally renowned for art, fashion, gastronomy, culture, historic monuments like the Eiffel Tower and Louvre Museum, and romantic Seine cruises. باريس عاصمة فرنسا المشهورة بالفن والموضة والأطعمة ومعالم مثل برج إيفل ومتحف اللوفر.",
                        MetadataJson = "{\"type\":\"capital\",\"continent\":\"Europe\"}"
                    },
                    new TravelKnowledge
                    {
                        Category = "City",
                        Title = "Dubai / دبي",
                        Country = "United Arab Emirates / الإمارات العربية المتحدة",
                        City = "Dubai / دبي",
                        Content = "A futuristic global metropolis famous for luxury shopping, ultramodern architecture, skyscrapers like Burj Khalifa, desert safaris, and Palm Jumeirah. دبي مدينة سياحية عالمية معروفة بالتسوق والتطور المعماري وبرج خليفة ورحلات الصحراء والنخلة.",
                        MetadataJson = "{\"type\":\"metropolis\",\"continent\":\"Asia\"}"
                    },
                    new TravelKnowledge
                    {
                        Category = "City",
                        Title = "Tokyo / طوكيو",
                        Country = "Japan / اليابان",
                        City = "Tokyo / طوكيو",
                        Content = "The bustling Japanese capital blending futuristic neon skyscrapers, anime culture, high-speed bullet trains, and traditional historic Shinto shrines and temples. طوكيو عاصمة اليابان وتجمع بين التكنولوجيا والمعابد التاريخية والأنمي والقطارات السريعة.",
                        MetadataJson = "{\"type\":\"capital\",\"continent\":\"Asia\"}"
                    },
                    new TravelKnowledge
                    {
                        Category = "City",
                        Title = "Rome / روما",
                        Country = "Italy / إيطاليا",
                        City = "Rome / روما",
                        Content = "The Eternal City and capital of Italy, famous for nearly 3,000 years of globally influential architecture, ancient Roman ruins like the Colosseum, Vatican City, and Italian food. روما عاصمة إيطاليا وتشتهر بالمعالم التاريخية العريقة مثل الكولوسيوم والفاتيكان والمطبخ الإيطالي.",
                        MetadataJson = "{\"type\":\"capital\",\"continent\":\"Europe\"}"
                    },
                    new TravelKnowledge
                    {
                        Category = "City",
                        Title = "Istanbul / إسطنبول",
                        Country = "Turkey / تركيا",
                        City = "Istanbul / إسطنبول",
                        Content = "A vibrant transcontinental city straddling Europe and Asia across the Bosphorus Strait, known for Ottoman mosques, Hagia Sophia, colorful bazaars, and imperial history. إسطنبول مدينة ساحرة في تركيا تقع بين أوروبا وآسيا وتشتهر بآيا صوفيا والبوسفور والبازارات.",
                        MetadataJson = "{\"type\":\"transcontinental\",\"continent\":\"Europe/Asia\"}"
                    },
                    new TravelKnowledge
                    {
                        Category = "Hotel",
                        Title = "The Ritz Paris / فندق الريتز باريس",
                        Country = "France / فرنسا",
                        City = "Paris / باريس",
                        Content = "A historic 5-star luxury hotel in Place Vendôme, Paris, offering opulent Belle Époque rooms, Michelin-starred dining, and prestige. فندق فاخر 5 نجوم في باريس يقع في ميدان فاندوم ويقدم تجربة إقامة ملكية ومطاعم فاخرة. Rating: 4.9⭐",
                        MetadataJson = "{\"rating\":4.9,\"stars\":5}"
                    },
                    new TravelKnowledge
                    {
                        Category = "Hotel",
                        Title = "Burj Al Arab Jumeirah / فندق برج العرب دبي",
                        Country = "United Arab Emirates / الإمارات",
                        City = "Dubai / دبي",
                        Content = "An iconic 7-star sail-shaped luxury hotel built on an artificial island off Jumeirah Beach in Dubai, featuring suite accommodations and butler service. فندق برج العرب في دبي مصمم على شكل شراع 7 نجوم على جزيرة اصطناعية بشاطئ جميرا. Rating: 4.9⭐",
                        MetadataJson = "{\"rating\":4.9,\"stars\":7}"
                    },
                    new TravelKnowledge
                    {
                        Category = "Hotel",
                        Title = "Hotel Eden Rome / فندق إيدن روما",
                        Country = "Italy / إيطاليا",
                        City = "Rome / روما",
                        Content = "A refined 5-star luxury hotel near Spanish Steps in Rome, offering elegant Italian decor, rooftop spa, and panoramic vistas over Rome. فندق 5 نجوم في روما بالقرب من السلالم الإسبانية ويتميز بإطلالات بانورامية على روما. Rating: 4.8⭐",
                        MetadataJson = "{\"rating\":4.8,\"stars\":5}"
                    },
                    new TravelKnowledge
                    {
                        Category = "Restaurant",
                        Title = "Le Jules Verne / مطعم لو جول فيرن",
                        Country = "France / فرنسا",
                        City = "Paris / باريس",
                        Content = "A Michelin-starred gourmet French restaurant on the second floor of Eiffel Tower in Paris, offering modern French gastronomy and views. مطعم فرنسي فاخر حاصل على نجمة ميشلان يقع في الطابق الثاني داخل برج إيفل في باريس.",
                        MetadataJson = "{\"cuisine\":\"French Fine Dining\",\"michelin\":true}"
                    },
                    new TravelKnowledge
                    {
                        Category = "Restaurant",
                        Title = "Da Enzo al 29 / مطعم دا إنزو روما",
                        Country = "Italy / إيطاليا",
                        City = "Rome / روما",
                        Content = "A beloved Roman trattoria in Trastevere, Rome, serving traditional handmade Cacio e Pepe, Carbonara, and Tiramisu. مطعم إيطالي تقليدي في روما يشتهر بأطباق الباستا الإيطالية الأصلية مثل الكاتشيو إي بيب والتيراميسو.",
                        MetadataJson = "{\"cuisine\":\"Italian\"}"
                    },
                    new TravelKnowledge
                    {
                        Category = "Attraction",
                        Title = "Eiffel Tower / برج إيفل",
                        Country = "France / فرنسا",
                        City = "Paris / باريس",
                        Content = "The world-famous wrought-iron lattice tower on Champ de Mars in Paris, constructed by Gustave Eiffel. Features observation decks. Entry Fee: 30 USD. برج إيفل الحديدي الشهير في باريس فرنسا، من أشهر المعالم السياحية في العالم ويحتوي على منصات مراقبة إطلالات باريس.",
                        MetadataJson = "{\"fee_usd\":30}"
                    },
                    new TravelKnowledge
                    {
                        Category = "Attraction",
                        Title = "Colosseum of Rome / الكولوسيوم روما",
                        Country = "Italy / إيطاليا",
                        City = "Rome / روما",
                        Content = "The largest ancient amphitheatre built in 80 AD in Rome, Italy, hosting gladiatorial contests and history. Entry Fee: 18 USD. مدرج الكولوسيوم التاريخي في روما إيطاليا وهو أكبر مدرج روماني أثري كان يستضيف صراعات المصارعين.",
                        MetadataJson = "{\"fee_usd\":18}"
                    },
                    new TravelKnowledge
                    {
                        Category = "Destination",
                        Title = "Amalfi Coast / ساحل أمالفي إيطاليا",
                        Country = "Italy / إيطاليا",
                        City = "",
                        Content = "A coast in Southern Italy featuring cliffside villages like Positano and Ravello. Best Time to Visit: May to October. ساحل أمالفي في جنوب إيطاليا يتميز بالقرى الجبلية والجلوس على البحر. أفضل وقت من مايو إلى أكتوبر.",
                        MetadataJson = "{\"best_months\":\"May-Oct\"}"
                    }
                );
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Database seeding completed successfully for TravelKnowledges.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding TravelKnowledges database.");
        }
    }
}

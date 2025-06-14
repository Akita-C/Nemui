using Microsoft.Extensions.Logging;
using Nemui.Infrastructure.Data.Context;
using Nemui.Shared.Entities;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Nemui.Infrastructure.Data.Seeds;

public class QuizSeeder(AppDbContext context, ILogger<BaseSeeder> logger) : BaseSeeder(context, logger)
{
    private readonly AppDbContext _context = context;
    private readonly ILogger<BaseSeeder> _logger = logger;
    public override int Order => 2;
    public override string Name => "Quiz Seeder";

    public override async Task SeedAsync()
    {
        LogSeedingStart();

        if (await HasDataAsync<Quiz>())
        {
            LogSeedingSkipped();
            return;
        }

        // Get first user as creator
        var creator = await _context.Users.FirstOrDefaultAsync();
        if (creator == null)
        {
            _logger.LogWarning("No users found. Cannot seed quizzes without a creator.");
            return;
        }

        var quizzes = GenerateQuizzes(creator.Id, 1000); // Generate 1000 quizzes for performance testing
        
        // Use bulk insert for better performance
        const int batchSize = 100;
        var batches = quizzes.Chunk(batchSize);
        
        var totalInserted = 0;
        foreach (var batch in batches)
        {
            await _context.Quizzes.AddRangeAsync(batch);
            await _context.SaveChangesAsync();
            totalInserted += batch.Length;
            
            if (totalInserted % 500 == 0)
                _logger.LogInformation("Inserted {Count} quizzes so far...", totalInserted);
        }

        LogSeedingComplete(totalInserted);
    }

    private List<Quiz> GenerateQuizzes(Guid creatorId, int count)
    {
        var currentTime = DateTime.UtcNow;
        var random = new Random();
        var quizzes = new List<Quiz>(count);

        var categories = new[] { "Programming", "Web Development", "Database", "Frontend", "Backend", "DevOps", "Mobile", "AI/ML", "Security", "Testing" };
        var programmingTopics = new[] { "C#", "JavaScript", "Python", "Java", "TypeScript", "Go", "Rust", "PHP", "Ruby", "Swift" };
        var frameworks = new[] { "ASP.NET Core", "React", "Vue.js", "Angular", "Node.js", "Django", "Spring Boot", "Laravel", "Express.js", "Next.js" };
        var databases = new[] { "SQL Server", "PostgreSQL", "MySQL", "MongoDB", "Redis", "SQLite", "Oracle", "Cassandra", "DynamoDB", "Firebase" };

        for (int i = 1; i <= count; i++)
        {
            var category = categories[random.Next(categories.Length)];
            var topic = category switch
            {
                "Programming" => programmingTopics[random.Next(programmingTopics.Length)],
                "Web Development" or "Frontend" or "Backend" => frameworks[random.Next(frameworks.Length)],
                "Database" => databases[random.Next(databases.Length)],
                _ => programmingTopics[random.Next(programmingTopics.Length)]
            };

            var tags = GenerateRandomTags(category, topic, random);
            var isPublic = random.NextDouble() > 0.3; // 70% public
            var duration = random.Next(5, 61); // 5-60 minutes

            quizzes.Add(new Quiz
            {
                Id = Guid.NewGuid(),
                Title = GenerateQuizTitle(topic, i),
                Description = GenerateQuizDescription(topic, category),
                Category = category,
                Tags = JsonSerializer.Serialize(tags),
                IsPublic = isPublic,
                EstimatedDurationMinutes = duration,
                CreatorId = creatorId,
                CreatedAt = currentTime.AddMinutes(-random.Next(0, 43200)), // Random time within last 30 days
                CreatedBy = creatorId.ToString()
            });
        }

        return quizzes;
    }

    private string GenerateQuizTitle(string topic, int index) =>
        $"{topic} {GetRandomTitleSuffix()} #{index:D4}";

    private string GetRandomTitleSuffix()
    {
        var suffixes = new[] 
        { 
            "Fundamentals", "Advanced Concepts", "Best Practices", "Deep Dive", 
            "Mastery Test", "Quick Assessment", "Comprehensive Review", "Skills Check",
            "Professional Level", "Expert Challenge", "Practical Application", "Core Concepts"
        };
        return suffixes[new Random().Next(suffixes.Length)];
    }

    private static string GenerateQuizDescription(string topic, string category) =>
        $"Comprehensive {category.ToLower()} quiz focusing on {topic} concepts, best practices, and real-world applications. Test your knowledge and improve your skills.";

    private string[] GenerateRandomTags(string category, string topic, Random random)
    {
        var baseTags = new List<string> { topic, category };
        
        var additionalTags = new[] 
        { 
            "Beginner", "Intermediate", "Advanced", "Professional", "Certification",
            "Interview Prep", "Hands-on", "Theory", "Practice", "Real-world"
        };

        var tagCount = random.Next(3, 6); // 3-5 tags total
        var selectedTags = additionalTags.OrderBy(x => random.Next()).Take(tagCount - 2).ToList();
        baseTags.AddRange(selectedTags);

        return baseTags.ToArray();
    }
}
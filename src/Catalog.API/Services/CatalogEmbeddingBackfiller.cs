using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using eShop.Catalog.API.Infrastructure;
using eShop.Catalog.API.Model;

namespace eShop.Catalog.API.Services;

public class CatalogEmbeddingBackfiller : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CatalogEmbeddingBackfiller> _logger;

    public CatalogEmbeddingBackfiller(
        IServiceProvider serviceProvider,
        ILogger<CatalogEmbeddingBackfiller> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CatalogEmbeddingBackfiller is starting.");

        // Wait 5 seconds initially to let other containers/services start up
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
                var catalogAI = scope.ServiceProvider.GetRequiredService<ICatalogAI>();

                if (!catalogAI.IsEnabled)
                {
                    _logger.LogInformation("Catalog AI is not enabled yet. Checking again in 10 seconds.");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    continue;
                }

                // Get items missing embedding
                var itemsMissingEmbedding = await context.CatalogItems
                    .Where(c => c.Embedding == null)
                    .ToListAsync(stoppingToken);

                if (itemsMissingEmbedding.Count == 0)
                {
                    _logger.LogInformation("All catalog items already have embeddings populated. Backfiller finishing.");
                    break; // All done!
                }

                _logger.LogInformation("Found {Count} catalog items missing embeddings. Generating them now...", itemsMissingEmbedding.Count);

                var embeddings = await catalogAI.GetEmbeddingsAsync(itemsMissingEmbedding);
                if (embeddings != null)
                {
                    for (int i = 0; i < itemsMissingEmbedding.Count; i++)
                    {
                        itemsMissingEmbedding[i].Embedding = embeddings[i];
                    }

                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Successfully backfilled embeddings for {Count} catalog items.", itemsMissingEmbedding.Count);
                    break; // Successfully finished
                }
                else
                {
                    _logger.LogWarning("Catalog AI returned null embeddings. Retrying in 15 seconds.");
                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to backfill catalog embeddings. Retrying in 15 seconds.");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
    }
}

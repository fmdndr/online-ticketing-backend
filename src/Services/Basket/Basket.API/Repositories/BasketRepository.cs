using System.Text.Json;
using Shared.Common.Models;
using StackExchange.Redis;

namespace Basket.API.Repositories;

public interface IBasketRepository
{
    Task<ShoppingCart?> GetBasket(string userId);
    Task<ShoppingCart?> UpdateBasket(ShoppingCart basket);
    Task DeleteBasket(string userId);
    Task<bool> AcquireLock(string eventId, string ticketTypeName, string userId, TimeSpan expiry);
    Task ReleaseLock(string eventId, string ticketTypeName, string userId);
}

public class BasketRepository : IBasketRepository
{
    private readonly IDatabase _redisDb;
    private readonly ILogger<BasketRepository> _logger;

    public BasketRepository(IConnectionMultiplexer redis, ILogger<BasketRepository> logger)
    {
        _redisDb = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<ShoppingCart?> GetBasket(string userId)
    {
        var basket = await _redisDb.StringGetAsync($"basket:{userId}");
        if (basket.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<ShoppingCart>(basket!);
    }

    public async Task<ShoppingCart?> UpdateBasket(ShoppingCart basket)
    {
        var serialized = JsonSerializer.Serialize(basket);
        var created = await _redisDb.StringSetAsync(
            $"basket:{basket.UserId}",
            serialized,
            TimeSpan.FromMinutes(10) // 10-minute TTL
        );

        if (!created)
        {
            _logger.LogError("Failed to update basket for user {UserId}", basket.UserId);
            return null;
        }

        _logger.LogInformation("Basket updated for user {UserId} with TTL of 10 minutes", basket.UserId);
        return await GetBasket(basket.UserId);
    }

    public async Task DeleteBasket(string userId)
    {
        await _redisDb.KeyDeleteAsync($"basket:{userId}");
        _logger.LogInformation("Basket deleted for user {UserId}", userId);
    }

    /// <summary>
    /// Acquires a distributed lock using Redis SETNX to prevent double-booking.
    /// The lock key is based on eventId + ticketTypeName, and the value is the userId.
    /// </summary>
    public async Task<bool> AcquireLock(string eventId, string ticketTypeName, string userId, TimeSpan expiry)
    {
        var lockKey = $"lock:ticket:{eventId}:{ticketTypeName}";
        var acquired = await _redisDb.StringSetAsync(lockKey, userId, expiry, When.NotExists);

        if (!acquired)
        {
            var currentHolder = await _redisDb.StringGetAsync(lockKey);
            if (currentHolder == userId)
            {
                await _redisDb.KeyExpireAsync(lockKey, expiry);
                _logger.LogInformation("Lock renewed for {EventId}/{TicketType} by user {UserId}", eventId, ticketTypeName, userId);
                return true;
            }
        }

        if (acquired)
            _logger.LogInformation("Lock acquired for {EventId}/{TicketType} by user {UserId}", eventId, ticketTypeName, userId);
        else
            _logger.LogWarning("Lock NOT acquired for {EventId}/{TicketType} by user {UserId} — already held by another user", eventId, ticketTypeName, userId);

        return acquired;
    }

    /// <summary>
    /// Releases the distributed lock only if the current holder matches the userId (safe release).
    /// </summary>
    public async Task ReleaseLock(string eventId, string ticketTypeName, string userId)
    {
        var lockKey = $"lock:ticket:{eventId}:{ticketTypeName}";
        var currentHolder = await _redisDb.StringGetAsync(lockKey);

        if (currentHolder == userId)
        {
            await _redisDb.KeyDeleteAsync(lockKey);
            _logger.LogInformation("Lock released for {EventId}/{TicketType} by user {UserId}", eventId, ticketTypeName, userId);
        }
        else
        {
            _logger.LogWarning("Lock release skipped for {EventId}/{TicketType} — holder mismatch (expected {UserId}, got {Holder})",
                eventId, ticketTypeName, userId, currentHolder.ToString());
        }
    }
}

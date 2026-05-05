namespace MinimalApi.Template.Api.Extensions;

public static class DistributedCacheExtensions
{
	public static Task SetAsync<T>(this IDistributedCache cache, string key, T value, DistributedCacheEntryOptions options,
		CancellationToken cancellationToken = default)
	{
		var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
		return cache.SetAsync(key, bytes, options, cancellationToken);
	}

	public static bool TryGetValue<T>(this IDistributedCache cache, string key, out T? value)
	{
		var val = cache.Get(key);
		value = default;

		if (val is null)
		{
			return false;
		}

		value = JsonSerializer.Deserialize<T>(val);
		return true;
	}

	public static async Task<T?> GetOrSetAsync<T>(this IDistributedCache cache, string key, Func<Task<T>> factory,
		DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
	{
		if (cache.TryGetValue(key, out T? value) && value is not null)
		{
			return value;
		}

		value = await factory();

		if (value is not null)
		{
			options ??= new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(30)).SetAbsoluteExpiration(TimeSpan.FromHours(1));

			await cache.SetAsync(key, value, options, cancellationToken);
		}

		return value;
	}
}
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var redis = ConnectionMultiplexer.Connect("localhost");
        var db = redis.GetDatabase();

        string key = "absolute-key";
        string value = "This is an absolute expiration item.";

        // Store the cache item with an expiration of 1 minute
        await db.StringSetAsync(key, value);
        db.KeyExpire(key, TimeSpan.FromMinutes(1));

        // Retrieve and display the cached item
        Console.WriteLine($"Cached Value: {await db.StringGetAsync(key)}");

        // Delay for 1 minute to check expiration
        await Task.Delay(61000); // Wait 1 minute and 1 second

        // Check if the item is still in the cache
        Console.WriteLine($"Cached Value after expiration: {await db.StringGetAsync(key)}"); // Should be null

        // Sliding expiration logic
        string slidingkey = "sliding-key";
        string slidingvalue = "Sliding expiration data";

        // Store the cache item with an initial expiration of 30 seconds
        await db.StringSetAsync(slidingkey, slidingvalue, TimeSpan.FromSeconds(30));

        for (int i = 0; i < 3; i++)
        {
            // Get the cached item as RedisValue and check for null
            RedisValue cachedValue = await db.StringGetAsync(slidingkey);

            if (!cachedValue.IsNullOrEmpty)
            {
                Console.WriteLine($"Access {i + 1}: Cached Value: {cachedValue}");

                // Reset the expiration timer to 30 seconds
                db.KeyExpire(slidingkey, TimeSpan.FromSeconds(30));
            }
            else
            {
                Console.WriteLine($"Access {i + 1}: Key '{slidingkey}' does not exist.");
                break; // Exit loop if the key no longer exists
            }

            // Wait 10 seconds before the next access
            await Task.Delay(10000);
        }

        // Final delay to allow the item to expire
        await Task.Delay(31000); // Wait 31 seconds to exceed expiration window

        // Verify the item has expired
        RedisValue finalValue = await db.StringGetAsync(slidingkey);
        Console.WriteLine($"Cached Value after expiration: {await db.StringGetAsync(slidingkey)}");

        // Dependent cache entry logic
        string parentKey = "product";
        string childKey = "inventory";

        // Set the parent and child keys
        await db.StringSetAsync(parentKey, "Product data");
        await db.StringSetAsync(childKey, "Inventory data");

        Console.WriteLine("\nInitial Cache State:");
        Console.WriteLine($"Parent Key: {await db.StringGetAsync(parentKey)}");
        Console.WriteLine($"Child Key: {await db.StringGetAsync(childKey)}");

        // Simulate parent update
        Console.WriteLine("\nUpdating parent entry...");
        await db.StringSetAsync(parentKey, "Updated product data");

        // Invalidate the child entry if the parent is updated
        if (await db.StringGetAsync(parentKey) == "Updated product data")
        {
            Console.WriteLine("Parent updated. Expiring dependent entry...");
            await db.KeyDeleteAsync(childKey);
        }

        // Display the final state of the cache
        Console.WriteLine("\nFinal Cache State:");
        Console.WriteLine($"Parent Key: {await db.StringGetAsync(parentKey)}");
        Console.WriteLine($"Child Key: {await db.StringGetAsync(childKey)}"); // Should be null
    }
}
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonOomExample
{
    internal class Program
    {
        const string jsonFilename = "test.json";

        static async Task Main(string[] args)
        {
            if (!File.Exists(jsonFilename))
                await CreateTestFile();

            await ReadTestFile();
        }

        // Setup the test file - a large json array
        static async Task CreateTestFile()
        {
            using var stream = File.Create(jsonFilename);

            var items = Enumerable.Range(0, 4_000_000)
                .Select(c => new TestModel(Guid.Empty, DateTimeOffset.MinValue));

            await JsonSerializer.SerializeAsync(stream, items);
        }

        static async Task ReadTestFile()
        {
            using var stream = File.OpenRead(jsonFilename);

            long count = 0;

            // Memory leak occurs during this loop
            // See: System.Text.Json.Arguments<Guid, DateTimeOffset, object, object>
            await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<TestModel>(stream))
            {
                count++;
            }

            Console.WriteLine($"Count: {count}");
        }
    }

    class TestModel
    {
        public Guid Id { get; set; }

        public DateTimeOffset Date { get; set; }

        // If the deserialiser uses a constructor, JsonSerializer.DeserializeAsyncEnumerable() will leak memory
        // Comment this constructor out, and no OutOfMemoryException will occur
        [JsonConstructor]
        public TestModel(Guid id, DateTimeOffset date)
        {
            this.Id = id;
            this.Date = date;
        }
    }
}
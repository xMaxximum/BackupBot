using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;
using System.Runtime.CompilerServices;

var builder = WebApplication
    .CreateBuilder(args);

builder.Configuration.AddBotConfiguration();

builder.Services.AddOptions();
builder.Services.AddLogging(logger =>
{
    LoggerConfiguration loggerConfiguration = new();
    loggerConfiguration.WriteTo.Console(outputTemplate: "[{Timestamp:dd-mm, HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");
    Log.Logger = loggerConfiguration.CreateLogger();
    logger.AddSerilog(Log.Logger);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "first",
        policy =>
        {
            policy.AllowAnyOrigin();
        });
});


builder.Services.AddBotServices();

var app = builder.Build();
app.UseCors("first");
ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");

var httpClient = new HttpClient();


StackExchange.Redis.IDatabase db = redis.GetDatabase();

int guildCount = 0;

app.MapGet("/guildcount", () => guildCount);

app.MapGet("/updateguildcount/{id}", (int id) =>
{
    guildCount = id;
});

IBot bot = (IBot)app.Services.GetService(typeof(IBot))!;

app.MapGet("/", () => "Hello World!");

app.MapGet("/getguilds/{token}", async (string token) =>
{
    if (db.StringGetAsync(new RedisKey(token)).Result.IsNull)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/v10/users/@me/guilds")
        {
            Headers =
    {
        Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token)
    }
        };

        var response = await httpClient.SendAsync(requestMessage);


        var res = await response.Content.ReadAsStringAsync();

        var guilds = await bot.GetGuilds(res);

        var serialized = JsonConvert.SerializeObject(guilds);
        await db.StringSetAsync(new RedisKey(token), new RedisValue(serialized), TimeSpan.FromMinutes(5));

        return serialized;
    }
    else
    {
        return db.StringGetAsync(new RedisKey(token)).Result.ToString();
    }
});

app.MapGet("/getguild/{guildId}&id={userId}", async (ulong guildId, ulong userId) =>
{
    var guild = await bot.GetGuild(guildId, userId);
    if (guild.Id != string.Empty)
        return JsonConvert.SerializeObject(guild);
    else return "{ Result: 'Unauthorized' }";
});

app.MapGet("/checkguild/{guildId}&id={userId}", (ulong guildId, ulong userId) =>
{
    return Task.FromResult(bot.CheckGuild(guildId, Convert.ToUInt64(userId)).Result.ToString());
});

app.Run("http://localhost:5116");

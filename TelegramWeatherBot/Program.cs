using System;
using System.Net.Http;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using DotNetEnv; 

class Program
{
    private static readonly string TelegramToken;
    private static readonly string WeatherApiKey;

    static Program()
    {
        Env.Load();
        TelegramToken = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN");
        WeatherApiKey = Environment.GetEnvironmentVariable("WEATHER_API_KEY");
    }

    private static ITelegramBotClient _botClient;

    static async Task Main(string[] args)
    {
        _botClient = new TelegramBotClient(TelegramToken);

        var me = await _botClient.GetMeAsync();
        Console.WriteLine($"Bot started: {me.Username}");

        _botClient.StartReceiving(UpdateHandler, ErrorHandler);
        Console.ReadLine(); // Ожидаем, чтобы бот не завершил работу
    }

    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message.Type != MessageType.Text)
            return;

        var chatId = update.Message.Chat.Id;
        var text = update.Message.Text;

        switch (text)
        {
            case "/start":
                await botClient.SendTextMessageAsync(
                    chatId,
                    "Привет! Я бот, который покажет тебе погоду. Напиши /weather <город>, чтобы узнать погоду.",
                    cancellationToken: cancellationToken);
                break;

            case string cmd when cmd.StartsWith("/weather"):
                var city = cmd.Replace("/weather", "").Trim();
                if (string.IsNullOrEmpty(city))
                {
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "Укажите город, например: /weather Moscow",
                        cancellationToken: cancellationToken);
                    return;
                }

                var weatherInfo = await GetWeatherAsync(city);
                await botClient.SendTextMessageAsync(
                    chatId,
                    weatherInfo,
                    cancellationToken: cancellationToken);
                break;

            default:
                await botClient.SendTextMessageAsync(
                    chatId,
                    "Неизвестная команда. Используйте /start для списка команд.",
                    cancellationToken: cancellationToken);
                break;
        }
    }

    private static Task ErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }

    private static async Task<string> GetWeatherAsync(string city)
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync($"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={WeatherApiKey}&units=metric&lang=ru");
        dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(response);
        return $"Погода в {city}:\n" +
               $"Температура: {data.main.temp}°C\n" +
               $"Ощущается как: {data.main.feels_like}°C\n" +
               $"Погода: {data.weather[0].description}";
    }
}
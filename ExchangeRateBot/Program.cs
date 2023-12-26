using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using IBWT.Framework;
using Newtonsoft.Json;
using Microsoft.VisualBasic;


public enum State
{
    NoState = 0,
    Course = 1,
    Converter = 2,
    Statistics = 3,
    GetSecondCurrency = 4,
    GetAmount = 5
};

namespace BotExchangeRate
{
    internal class Program1
    {
        private static HttpClient httpClient = new HttpClient(); // Объект, через который мы будем получать данные от сайта с курсами валют через API
        private static string currency1; // Переменная, в которой мы будем хранить первую валюту
        private static string currency2; // Вторую валюту (нужно при конвертации)
        private static double amounToConvert = 0; // Сумма, которую нужно конвертировать

        public static string ConvertCurrency(string str_in)
        {
            if (str_in == "🇷🇺 Рубль")
                return "RUB";
            if (str_in == "🇪🇺 Евро")
                return "EUR";
            if (str_in == "🇬🇧 Фунт стерлингов")
                return "GBP";
            if (str_in == "🇨🇳 Юань")
                return "CNY";
            if (str_in == "🇦🇪 Дирхам")
                return "AED";
            return "USD";
        }

        public static State state = State.NoState;

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken) // Обработчик обновлений (сообщений от пользователя, нажатия на кнопки)
        {
            if (update.Message is not { } message && update.CallbackQuery is not { } callbackQuery)
                return;
            var chatId = update.GetChatId();

            // Создаем Reply клавиатуру (панель снизу), главная клавиатура с функциями
            ReplyKeyboardMarkup mainMarkup = new(new[]
            {
            new KeyboardButton[] { "💸 Курс" }, // Кнопка 1
            new KeyboardButton[] { "🔃 Конвертор" }, // Кнопка 2
            new KeyboardButton[] { "💹 Статистика валюты" }, // Кнопка 3
            })
            {
                ResizeKeyboard = true // Нужно, чтобы размер кнопки подстраивался под размер текста, иначе кнопка будет слишком большой
            };

            // Создаем Inline клавиатуру (панель прикрепленная к сооюбщению) для конвертации валют
            InlineKeyboardMarkup currencyConvertMarkup = new(new[]
            {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "🇷🇺 Рубль", callbackData: "🇷🇺 Рубль"), // Кнопки
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "🇺🇸 Доллар", callbackData: "🇺🇸 Доллар"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "🇪🇺 Евро", callbackData: "🇪🇺 Евро"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "🇬🇧 Фунт стерлингов", callbackData: "🇬🇧 Фунт стерлингов"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "🇨🇳 Юань", callbackData: "🇨🇳 Юань"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "🇦🇪 Дирхам", callbackData: "🇦🇪 Дирхам"),
            }
            });

            // Создаем вторую Inline клавиатуру, такая же как первая но без доллара. Потому что api, из которого мы берем данные показывает курсы только по отношению к доллару. 
            //Поэтому в этой клавиатуре не должно быть доллара, чтобы мы не показвали курс доллара по отношению к доллару.
            InlineKeyboardMarkup currencyExchangeMarkup = new(new[]
            {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "🇷🇺 Рубль", callbackData: "🇷🇺 Рубль"), // Кнопки
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "🇪🇺 Евро", callbackData: "🇪🇺 Евро"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "🇬🇧 Фунт стерлингов", callbackData: "🇬🇧 Фунт стерлингов"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "🇨🇳 Юань", callbackData: "🇨🇳 Юань"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "🇦🇪 Дирхам", callbackData: "🇦🇪 Дирхам"),
            }
            });

            // Обработка действий пользователя
            switch (update.Type)
            {
                case UpdateType.Message:  // Если пользователь отправил сообщение
                    string messageText = update.Message.Text;
                    switch (state)
                    {
                        case State.GetAmount:
                            amounToConvert = Double.Parse((update.Message.Text));
                            string apiUrl = $"http://api.currencylayer.com/convert?access_key=45ee332a151419fa2f5627b646aed229&from={currency1}&to={currency2}&amount={amounToConvert}&format=1";
                            string json = httpClient.GetStringAsync(apiUrl).Result;
                            CurrencyConvertResponse response = JsonConvert.DeserializeObject<CurrencyConvertResponse>(json);

                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: $"{amounToConvert} {currency1} = {response.Result} {currency2}",
                                replyMarkup: mainMarkup,
                                cancellationToken: cancellationToken);
                            state = State.NoState;
                            break;
                        default:
                            switch (messageText)
                            {
                                case "/start":
                                    await botClient.SendTextMessageAsync(
                                        chatId: chatId,
                                        text: "Добро пожаловать в бот курса валют.\nЗдесь вы можете отслеживать актульные курсы валют и конвертировать деньги из одной валюты в другую.\nЧто хотите сделать?",
                                        replyMarkup: mainMarkup,
                                        cancellationToken: cancellationToken);
                                    break;
                                case "💸 Курс":
                                    await botClient.SendTextMessageAsync(
                                        chatId: chatId,
                                        text: "Курс, какой валюты хотите увидеть?",
                                        replyMarkup: currencyExchangeMarkup,
                                        cancellationToken: cancellationToken);
                                    state = State.Course;
                                    break;
                                case "🔃 Конвертор":
                                    await botClient.SendTextMessageAsync(
                                        chatId: chatId,
                                        text: "Из какой валюты конвертировать?",
                                        replyMarkup: currencyConvertMarkup,
                                        cancellationToken: cancellationToken);
                                    state = State.Converter;
                                    break;
                                case "💹 Статистика валюты":
                                    await botClient.SendTextMessageAsync(
                                        chatId: chatId,
                                        text: "Статистику какой валюты вы хотите увидеть?",
                                        replyMarkup: currencyExchangeMarkup,
                                        cancellationToken: cancellationToken);
                                    state = State.Statistics;
                                    break;
                            }
                            break;
                    }
                    break;

                case UpdateType.CallbackQuery: // Если пользователь нажал на inline кнопку
                    switch (state)
                    {
                        case State.Course:
                            currency1 = ConvertCurrency(update.CallbackQuery.Data);
                            string apiUrl = $"http://api.currencylayer.com/live?access_key=45ee332a151419fa2f5627b646aed229&source=USD&currencies={currency1}&format=1";
                            string json = httpClient.GetStringAsync(apiUrl).Result;
                            CurrencyLiveResponse liveResponse = JsonConvert.DeserializeObject<CurrencyLiveResponse>(json);

                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: $"Курс {currency1} на сегодня:\n1 USD = {liveResponse.Quotes[$"USD{currency1}"]} {currency1}",
                                replyMarkup: mainMarkup
                                );
                            break;
                        case State.Converter:
                            currency1 = ConvertCurrency(update.CallbackQuery.Data);
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "В какую валюту конвертировать?",
                                replyMarkup: currencyConvertMarkup
                                );
                            state = State.GetSecondCurrency;
                            break;
                        case State.Statistics:
                            currency1 = ConvertCurrency(update.CallbackQuery.Data);
                            apiUrl = $"http://api.currencylayer.com/timeframe?access_key=45ee332a151419fa2f5627b646aed229&start_date=2023-11-27&end_date=2023-12-27&source={currency1}&currencies=USD&format=1";
                            json = httpClient.GetStringAsync(apiUrl).Result;
                            HistoricalExchangeResponse exchangeResponse = JsonConvert.DeserializeObject<HistoricalExchangeResponse>(json);
                            Console.WriteLine(json);
                            string statistics = $"Статистика курса {update.CallbackQuery.Data} за последний месяц:\n";
                            foreach (KeyValuePair<DateTime, Dictionary<string, decimal>> entry in exchangeResponse.Quotes)
                            {
                                statistics += $"{entry.Key}:\n1 {currency1} = {entry.Value[$"{currency1}USD"]} USD\n\n";
                            }
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: statistics,
                                replyMarkup: mainMarkup
                                );
                            break;
                        case State.GetSecondCurrency:
                            currency2 = ConvertCurrency(update.CallbackQuery.Data);
                            await botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Какую сумму конвертировать?",
                                replyMarkup: mainMarkup
                                );
                            state = State.GetAmount;
                            break;
                    }
                    break;
            }
        }

        public static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken) // Обработчик исключений(ошибок)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        static void Main(string[] args)
        {

            TelegramBotClient botClient = new TelegramBotClient("token");
            ReceiverOptions receiverOptions = new() // Объект настроек бота, используется, чтобы указать отлавливаемые изменения(здесь: сообщения, нажатия на inline кнопки)
            {
                AllowedUpdates = new[]
                {
                    UpdateType.Message,
                    UpdateType.CallbackQuery,
                }
            };

            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions
            ); // Запуск бота, куда мы передаем обработчик обновлений(сообщений), обработчик ошибок(стандартный из документации, без него не работает), настроки бота

            var me = botClient.GetMeAsync().Result;
            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();
        }

        public class CurrencyLiveResponse
        {
            public bool Success { get; set; }
            public string Terms { get; set; }
            public string Privacy { get; set; }
            public long Timestamp { get; set; }
            public string Source { get; set; }
            public Dictionary<string, decimal> Quotes { get; set; }
        }

        public class CurrencyConvertResponse
        {
            public bool Success { get; set; }
            public string Terms { get; set; }
            public string Privacy { get; set; }
            public QueryInfo Query { get; set; }
            public ConvertInfo Info { get; set; }
            public decimal Result { get; set; }

            public class QueryInfo
            {
                public string From { get; set; }
                public string To { get; set; }
                public decimal Amount { get; set; }
            }

            public class ConvertInfo
            {
                public long Timestamp { get; set; }
                public decimal Quote { get; set; }
            }
        }
        public class HistoricalExchangeResponse
        {
            public bool Success { get; set; }
            public string Terms { get; set; }
            public string Privacy { get; set; }
            public bool Timeframe { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Source { get; set; }
            public Dictionary<DateTime, Dictionary<string, decimal>> Quotes { get; set; }
        }
    }
}

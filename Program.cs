using System.Xml.Linq;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

const string telegram_Token = "6636952824:AAFEb4ll4H57i9V73CIGOvKPW62IAZ3tik0";
const long my_Chat_Id = -1001950473925;
bool sentToMeMode = false;

var botClient = new TelegramBotClient(telegram_Token);
using var cts = new CancellationTokenSource();
const string rssChannel = "https://asterios.tm/index.php?cmd=rss&serv=-1&filter=keyboss&out=xml";
var processedItemDescription = new HashSet<string>();

var receiverOptions = new ReceiverOptions
{
AllowedUpdates = { }
};
botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();
await Task.Delay(int.MaxValue);
cts.Cancel();



async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
    if (update.Type == UpdateType.Message && update.Message.Type == MessageType.Text)
    {
        var messageText = update.Message.Text;

        if (messageText == "/start")
        {
            FetchRssFeedItemsAsync("https://asterios.tm/index.php?cmd=rss&serv=-1&filter=keyboss&out=xml");
            FetchAndSendRssUpdatesAsync();
        }
    }
}
Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };
    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}
async Task FetchAndSendRssUpdatesAsync()
{
    while (true)
    {
        try
        {
            var feedItems = await FetchRssFeedItemsAsync(rssChannel);
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            foreach (var item in feedItems)
            {
                if (DateTime.TryParse(item.Date, out var itemDate))
                {
                    if (itemDate.Date == today || itemDate.Date == yesterday)
                    {
                        if (!processedItemDescription.Contains(item.Description))
                        {
                            await botClient.SendTextMessageAsync(
                            chatId: my_Chat_Id,
                            text: $"☠️:\n{item.Title}\n\n⚔️\nКто убил:\n{item.Description}\n\n⌚️\nВремя смерти РБ:\n{item.Date}",
                            cancellationToken: cts.Token);
                            processedItemDescription.Add(item.Description);
                        }
                    }
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching RSS feed: {ex.Message}");
        }
    }
}
async Task<List<RssFeedItem>> FetchRssFeedItemsAsync(string feedUrl)
{
    using (var httpClient = new HttpClient())
    {
        var rssContent = await httpClient.GetStringAsync(feedUrl);
        var xDocument = XDocument.Parse(rssContent);

        var items = xDocument.Descendants("item")
            .Select(item => new RssFeedItem
            {
                Title = item.Element("title")?.Value,
                Description = item.Element("description")?.Value,
                Date = item.Element("pubDate")?.Value
            })
            .ToList();

        return items;
    }
}
class RssFeedItem
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Date { get; set; }

}
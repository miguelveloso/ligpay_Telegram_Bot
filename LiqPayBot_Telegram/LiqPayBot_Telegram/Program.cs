using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;
using LiqPayBot_Telegram.Structures;

namespace LiqPayBot_Telegram
{
    class Program
    {        
        private static readonly TelegramBotClient Bot = new TelegramBotClient("11111111111:AAAAAAAAAAAAAAAAAAAAAAAA-AAAAAAAAA");        
        private static readonly ILogger _logger = new LoggerConfiguration().WriteTo.Console().WriteTo.RollingFile(pathFormat: "Log.log").CreateLogger();
        
        public static List<Item> avaibleItems = new List<Item> {
            new Item { Name = "Монета", Amount = 1}, //🥭          
        };
       
        private static List<Cart> userCarts = new List<Cart>();

        static void Main(string[] args)
        {
            _logger.Information("Program Started!");            
            Bot.OnMessage += Bot_OnMessage;           
            Bot.OnCallbackQuery += Bot_OnCallbackQuery;
            Bot.OnUpdate += Bot_OnUpdate;
            _logger.Information("Starting Bot");
            Bot.StartReceiving();
            Console.ReadLine();
            _logger.Information("Stopping Bot");
            Bot.StopReceiving();
        }

        private static async void Bot_OnUpdate(object sender, UpdateEventArgs e)
        {
            switch (e.Update.Type)
            {
                case UpdateType.PreCheckoutQuery:
                    if (e.Update.PreCheckoutQuery.InvoicePayload == "data")
                        await Bot.AnswerPreCheckoutQueryAsync(e.Update.PreCheckoutQuery.Id);
                    else await Bot.AnswerPreCheckoutQueryAsync(e.Update.PreCheckoutQuery.Id, "Произошла ошибка");
                    break;
            }
        }

        private static async void Bot_OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            //Смотрим что за коллбэк пришёл
            switch (e.CallbackQuery.Data)
            {
                case "Payment":
                    await Program.HandlePayment(e.CallbackQuery.From.Id);
                    break;
                case "More":
                    await Program.HandleProductList(e.CallbackQuery.From.Id);
                    break;
            }
            //Обязательно отвечать на коллбэк
            await Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id);
        }

        private static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Type == MessageType.Text)
            {
                var userId = e.Message.From.Id;
                var username = e.Message.From.Username;
                var firstName = e.Message.From.FirstName;
                var lastName = e.Message.From.LastName;                

                _logger.Information($"Получили сообщение из чата({e.Message.Chat.Id}) от пользователя {firstName} {lastName} с ID:{e.Message.From.Id}:{e.Message.Text}");

                //Если у юзверя еще нет корзинки, создадим
                if (!userCarts.Any(q => q.userId == e.Message.From.Id))
                    userCarts.Add(new Cart { userId = userId });

                //Для обычных команд кидаем стандартный ответ
                switch (e.Message.Text.ToLower())
                {
                    /*-------------------------SendBasicMarkup----------------------------*/
                    case "/start":
                    case "/action":
                    case "/start@MrEnglishStudyBot":
                        await Program.HandleHello(userId, username, firstName, lastName, e);
                        break;
                    /*--------------------------HandlePayment---------------------------*/
                    case "/payment":                    
                    case "оплата":
                    case "купить": 
                    case "заплатить":                    
                    case "корзина":
                        await Program.HandlePayment(userId);
                        break;
                    /*------------------------HandleProductList-----------------------------*/                   
                    case "донат":                    
                    case "donate":
                    case "/donate":
                    case "/donate@MrEnglishStudyBot":                    
                    case "поддержка":
                        await Program.HandleProductList(userId);
                        break;
                    /*-------------------------HandleAboutme----------------------------*/
                    case "/about":
                    case "/about@MrEnglishStudyBot":                    
                        await Program.HandleAboutme(userId, firstName, lastName, e);
                        break;                            
                    /*-------------------------HandleContact----------------------------*/
                    case "/contact":
                    case "/contact@MrEnglishStudyBot":                    
                    case "контакты":
                    case "телефон":
                    case "позвонить":
                        await Program.HandleContact(userId, username, firstName, lastName, e);
                        break;
                    /*-------------------------HandleCartClear----------------------------*/
                    case "очистить корзину":
                    case "очистка":                    
                    case "сlrscr":
                    case "сlear":
                    case "/clearcart":
                    case "/clearcart@MrEnglishStudyBot":
                        await Program.HandleCartClear(userId, username, firstName, lastName, e);
                        break;

                    default:
                        await Program.HandleProductMessage(userId, e);
                        break;
                }
            }
        }

        public static async Task<string> HandleHello(int userId, string username, string firstName, string lastName, MessageEventArgs e)
        {
            StringBuilder stringBuilder = new StringBuilder();
            await Bot.SendTextMessageAsync(e.Message.Chat.Id, $"✨Привет, {firstName}, рад тебя видеть❗️ \n Можешь написать мне любой неправильный глагол в любой форме и я расскажу тебе о нем чуть чуть✨");
            _logger.Information($"Юхууу, у нас новый пользователь:{firstName} {lastName}({username}) c ID: {e.Message.Chat.Id}");          
            return stringBuilder.ToString();
        }

        /*----------отлавливатель сообщений от пользователей, если есть соответствие входящего ссообщения с List<Item> из Carts то товар будет добавлен в корзину-------------*/
        public static async Task HandleProductMessage(long userId, MessageEventArgs e)        
        {
            try
            {
                //Парсим сообщение на предмет товара               
                string[] msg = e.Message.Text.Trim().Split(' ');
                if (msg.Length > 1)
                    if (avaibleItems.Any(q => q.Name.ToUpper() == msg[0].ToUpper()))
                    {
                        if (userCarts.Any(q => q.items.Any(z => z.item.Name.ToUpper() == msg[0].ToUpper()) && q.userId == userId))
                        {
                            var item = userCarts.FirstOrDefault(q => q.items.Any(z => z.item.Name.ToUpper() == msg[0].ToUpper()) && q.userId == userId).items.FirstOrDefault(z => z.item.Name.ToUpper() == msg[0].ToUpper());
                            item.Count += int.Parse(msg[1]);
                        }
                        else
                        {
                            userCarts.FirstOrDefault(q => q.userId == userId).items.Add(new CartItem { Count = int.Parse(msg[1]), item = avaibleItems.FirstOrDefault(q => q.Name.ToUpper() == msg[0].ToUpper()) });
                        }
                        await Bot.SendTextMessageAsync(e.Message.Chat.Id, $"Добавили донат {msg[0]} в сумме {msg[1]}!");
                        _logger.Information($"Успешно добавили донат для оплаты ({e.Message.Chat.Id})");
                    }
                    else
                    {
                        await Bot.SendTextMessageAsync(e.Message.Chat.Id, $"😞Прости..но я тебя не понимаю😢 \n Я попробую уточнить у создателя что это может значить: {msg[0]}");
                        _logger.Information($"Отправили отказ (несуществующий товар) в чат ({e.Message.Chat.Id})");
                    }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Пробовали получить товар, не смогли😪");
                await Bot.SendTextMessageAsync(e.Message.Chat.Id, "Не понял что Вы хотите 🤔");
            }
        }
        /*рисовать 2 кнопки: 1 для перехода к оплате, 2 для просмотра товаров, работают через ►OnCallbackQuery◄  который закоментирован)*/
        public static async void SendBasicMarkup(MessageEventArgs e, long userId)
        {
            await Bot.SendTextMessageAsync(e.Message.Chat.Id, GetCartInfo(userId),
                        replyMarkup: new InlineKeyboardMarkup(new[] { InlineKeyboardButton.WithCallbackData("💰Перейти к оплате💰", "Payment"),
                            InlineKeyboardButton.WithCallbackData("🍌Добавить товаров🍓", "More") }));
            _logger.Information($"Пользователь {e.Message.Chat.FirstName}{e.Message.Chat.LastName} нажимает тут всякое ");
        }
        /*медод для просмотра корзины, если корзина будет пустой вернется пустота*/
        public static async Task<string> HandleCart(int chatId, string username, string firstName, string lastName, long userId, MessageEventArgs e)
        {
            await Bot.SendTextMessageAsync(chatId, GetCartInfo(userId));
            _logger.Information($"Показали содержимое корзинки пользовател: {firstName}{lastName} с ID: {chatId}");

            return "Информация о компании";
        }
        private static string GetCartInfo(long userId)
        {
            if (!userCarts.Any(q => q.userId == userId && q.items.Count != 0))
                return "Простите, Ваша корзина пуста🧺";

            var userCart = userCarts.FirstOrDefault(q => q.userId == userId);

            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"Количество товаров в корзине: {userCart.TotalCount} штучек, на сумму: {userCart.TotalAmount} грн😮");
            stringBuilder.AppendLine("💰В мешочке у Вас:💰");            

            foreach (var cartItem in userCart.items)
            {
                stringBuilder.AppendLine($"{cartItem.Count} чеканая {cartItem.item.Name} на сумму: {cartItem.AmountForCount} грн");
                stringBuilder.AppendLine($"{cartItem.Count} чеканая {cartItem.item.Name}");
            }
            return stringBuilder.ToString();
        }
        /*вывод содержимого корзины, берется из структуры Cart и списка List<item> */
        private static string GetItemsInfo()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("🔥Можете поддержать автора бота в этом нелегком деле чеканой монетой, деньги пойдут на обучение и добавление новых плюшечек🔥");
            stringBuilder.AppendLine("🐶Если хочешь подкинуть 10 грн, пишешь Монета 10🐶:");
            foreach (var singleItem in avaibleItems)
            {
                stringBuilder.AppendLine($"🔥{singleItem.Name} - {singleItem.Amount} грн🔥"); //по цене
            }
            return stringBuilder.ToString();
        }
        /*метод для проведения оплаты, если в корзину добавлен хоть один элемент*/

        private static async Task HandlePayment(int chatId)
        {
            try
            {
                if (!userCarts.Any(q => q.userId == chatId && q.items.Count != 0))
                {
                    await Bot.SendTextMessageAsync(chatId, "⭐️Извините, что бы сделать донат его сначала нужно добавить через /donate⭐️");
                    _logger.Information($"Отправили отказ (нет товаров) в чат ({chatId})");
                    return;
                }
                await Bot.SendInvoiceAsync(chatId,
               "поддержку автора",
               GetCartInfo(chatId),
               "data",
               "1215151115:LIVE:i1515515151551",
               "parameter",
               "UAH",
               userCarts.FirstOrDefault(q => q.userId == chatId).items.Select(q => new LabeledPrice(q.item.Name, (int)(q.AmountForCount * 100))),
               replyMarkup: new InlineKeyboardMarkup(new InlineKeyboardButton
               {
                   Text = "Поддержать",
                   Pay = true
               }));
                //Оплатили - удалили
                userCarts.FirstOrDefault(q => q.userId == chatId).items.Clear();
                _logger.Information($"Отправили инвойс в чат пользователю ({chatId})");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Не смогли провести платёж😭");
            }
        }
        /*показать содержимое товаров в магазине*/
        private static async Task HandleProductList(int chatId)
        {
            await Bot.SendTextMessageAsync(chatId, GetItemsInfo());
            _logger.Information($"Отправили данные по товарам в чат ({chatId})");
        }
        /*если будет нажата кнопка /about*/
        private static async Task<string> HandleAboutme(int chatId, string firstName, string lastName, MessageEventArgs e)
        {
            StringBuilder stringBuilder = new StringBuilder();
            await Bot.SendTextMessageAsync(e.Message.Chat.Id, $"❤️Привет {firstName}❤️ \n 🤖Меня зовут Сергей, я создатель этого бота🤖 \n 🏴󠁧󠁢󠁥󠁮󠁧󠁿Я изучаю английский и хочу помочь в этом нелегком деле и тебе🇺🇸");
            _logger.Information($"Отправили данные о компании пользователю {firstName}{lastName} в чат ({chatId})");
            return stringBuilder.ToString();
        }
        /*если будет нажата кнопка /contact*/
        private static async Task<string> HandleContact(int chatId, string username, string firstName, string lastName, MessageEventArgs e)
        {
            StringBuilder stringBuilder = new StringBuilder();
            await Bot.SendTextMessageAsync(e.Message.Chat.Id, "📩Почта для связи: sergey.krugluy1810@gmail.com📩 \n 📱Связь в telegram: t.me/miguelveloso📱 \n 📲Номер телефона: +380994322735📲");
            _logger.Information($"Отправили контактные данные по запросу пользователя {firstName}{lastName} данные в чат ({chatId})");
            return stringBuilder.ToString();
        }
        /*если будет нажата кнопка /clearcart*/

        private static async Task<string> HandleCartClear(int chatId, string username, string firstName, string lastName, MessageEventArgs e)
        {
            StringBuilder stringBuilder = new StringBuilder();
            userCarts.FirstOrDefault(q => q.userId == chatId).items.Clear();
            await Bot.SendTextMessageAsync(e.Message.Chat.Id, "🧺Все добавленные донаты удалены🧺");
            _logger.Information($"Очистили корзину пользователю {firstName} {lastName} в чате:{chatId}");
            return stringBuilder.ToString();
        }          
    }
}

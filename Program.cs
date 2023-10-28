using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TG_bot
{
    internal class Program
    {
        static bool isInnExpected = false;
        static async Task Main(string[] args)
        {
            TelegramBotClient botClient = null;
            var configuration = LoadBotConfigurationFromXmlFile("C:\\Users\\Пользователь\\Desktop\\Visual_Studio\\Projects_C#\\TG_bot\\BotConfiguration.xml");
            if (configuration != null)
            {
                string botToken = configuration.TelegramBotToken;

                botClient = new TelegramBotClient(botToken);
            }

            ReceiverOptions receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[]
                {
                    UpdateType.Message,
                    UpdateType.CallbackQuery
                },
                ThrowPendingUpdates = true
            };
            using (var cts = new CancellationTokenSource())
            {
                botClient.StartReceiving(UpdateHandler, ErrorHandler, receiverOptions, cts.Token);
                var me = await botClient.GetMeAsync();
                await Console.Out.WriteLineAsync($"{me.FirstName} запущен!");
                await Task.Delay(-1);
            }
        }
        static string GetNameAndAddressOfACompanyByINN(string inn)
        {
            string result = "";
            string[] innArray = inn.Split(','); 
            if (isInnExpected == true)
            {
                foreach (string i in innArray)
                {
                    try
                    {
                        var chromeOptions = new ChromeOptions();
                        chromeOptions.AddArgument("--headless");
                        IWebDriver driver = new ChromeDriver(chromeOptions);
                        driver.Navigate().GoToUrl("https://egrul.nalog.ru/index.html");

                        IWebElement element1 = driver.FindElement(By.Id("query"));
                        element1.SendKeys(i);

                        IWebElement searchButton = driver.FindElement(By.Id("btnSearch"));
                        searchButton.Click();
                        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                        IWebElement searchResult = wait.Until(d =>
                        {
                            var element = d.FindElement(By.CssSelector(".res-text"));
                            return element.Displayed ? element : null;
                        });

                        string searchResultText = searchResult.Text;
                        if (searchResultText == null)
                        {
                            throw new InvalidEnumArgumentException();
                        }
                        string[] splittedResult = searchResultText.Split(',');

                        string addressResult = searchResult.Text.Substring(0, searchResult.Text.IndexOf(", ОГРН")).Trim();
                        string nameResult = searchResult.Text.Substring(searchResult.Text.IndexOf("КПП") + 15).Trim();
                        result += i + ": " + addressResult + "\n" + nameResult + "\n\n";
                    }
                    catch (Exception ex)
                    {
                        result += i + ": Нет такого ИНН\n\n";
                    }
                }
            }
            isInnExpected = false;
            return result;
        }
        static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            try
            {
                //1 / start – начать общение с ботом.
                //2 / help – вывести справку о доступных командах.
                //3 / hello – вывести ваше имя и фамилию, ваш email, и дату получения задания.
                //4 / inn – получить наименования и адреса компаний по ИНН.Предусмотреть возможность
                //указания нескольких ИНН за одно обращение к боту.
                if (isInnExpected)
                {
                    await botClient.SendTextMessageAsync(update.Message.Chat, GetNameAndAddressOfACompanyByINN(update.Message.Text));
                }

                try
                {
                    if (update.Type == UpdateType.Message)
                    {
                        var message = update.Message;
                        var user = message.From;
                        if (message.Text == "/start")
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Введите /help для получения функционала");
                        }
                        if (message.Text == "/help")
                        {
                            var chat = message.Chat;
                            await botClient.SendTextMessageAsync(chat,
                                "/start - начать общение с ботом" +
                                "\n/help - вывести справку о доступных командах" +
                                "\n/hello - вывод данных исполнителя" +
                                "\n/inn - получить наименования и адреса компаний по ИНН");
                        }

                        if (message.Text == "/hello")
                        {
                            var chat = message.Chat;
                            await botClient.SendTextMessageAsync(chat,
                                "Дмитрий Махин, maxin.11.11.1997@gmail.com, 2023.10.27");
                        }
                        if (message.Text == "/inn")
                        {
                            var chat = message.Chat;
                            await botClient.SendTextMessageAsync(chat, "Введите ИНН, интересующей Вас компании( в случае ввода нескольких ИНН ввод через запятую): ");
                            isInnExpected = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync(ex.Message);
                }
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
            }
        }
        static Task ErrorHandler(ITelegramBotClient botClient, Exception e, CancellationToken token)
        {
            ApiRequestException exception = e as ApiRequestException;
            Console.WriteLine(e.Message);
            return Task.CompletedTask;
        }

        public static BotConfiguration LoadBotConfigurationFromXmlFile(string filePath)
        {
            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    var serializer = new XmlSerializer(typeof(BotConfiguration));
                    return (BotConfiguration)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при загрузке конфигурации: " + ex.Message);
                return null;
            }
        }
    }
}

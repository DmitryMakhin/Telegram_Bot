using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TG_bot
{
    [XmlRoot("BotConfiguration")]
    public class BotConfiguration
    {
        [XmlElement("TelegramBotToken")]
        public string TelegramBotToken { get; set; }
    }
}

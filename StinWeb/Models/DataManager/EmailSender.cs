using StinWeb.Models.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;

namespace StinWeb.Models.DataManager
{
    public class EmailSender: IEmailSender
    {
        public async Task SendEmailAsync(string Адрес, string Subject, string Message)
        {
            var Email = Startup.sConfiguration["Settings:Email"];
            var smtpHost = Startup.sConfiguration["Settings:EmailSmtpHost"];
            int smtpPort;
            if (!Int32.TryParse(Startup.sConfiguration["Settings:EmailSmtpPort"], out smtpPort))
                smtpPort = 25;
            var smtpEnableSsl = Startup.sConfiguration["Settings:EmailSmtpEnableSsl"] == "1";
            var userName = Startup.sConfiguration["Settings:EmailUser"];
            var password = Startup.sConfiguration["Settings:EmailPsw"];

            using MailMessage message = new MailMessage();
            using SmtpClient smtp = new SmtpClient();
            message.From = new MailAddress(Email);
            message.To.Add(new MailAddress(Адрес));
            message.Subject = Subject;
            message.IsBodyHtml = true; 
            message.Body = Message;
            smtp.Port = smtpPort; //25;
            smtp.Host = smtpHost; //"smtp.yandex.ru";
            smtp.EnableSsl = smtpEnableSsl;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(userName, password);
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            await smtp.SendMailAsync(message);
        }
    }
}

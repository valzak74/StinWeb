using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StinWeb.Models.Repository
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string Адрес, string Subject, string Message);
    }
}

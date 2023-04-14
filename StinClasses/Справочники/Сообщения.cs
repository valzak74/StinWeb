using StinClasses.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StinClasses.Справочники
{
    public interface IСообщения : IDisposable
    {
        Task SendMessage(string Subject, string MessageText, string ReceiverId, string UserId);
    }
    public class СообщенияEntity : IСообщения
    {
        private StinDbContext _context;
        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            this.disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public СообщенияEntity(StinDbContext context)
        {
            _context = context;
        }
        public async Task SendMessage(string Subject, string MessageText, string ReceiverId, string UserId)
        {
            Sc10814 Message = new Sc10814
            {
                Id = _context.GenerateId(10814),
                Descr = Subject.StringLimit(50),
                Parentext = ReceiverId,
                Ismark = false,
                Verstamp = 0,
                Sp10809 = UserId,
                Sp10811 = DateTime.Today,
                Sp10812 = 0,
                Sp10860 = Common.ПустоеЗначениеИд13,
                Sp10810 = MessageText
            };
            await _context.Sc10814s.AddAsync(Message);
            await _context.SaveChangesAsync();
            _context.РегистрацияИзмененийРаспределеннойИБ(10814, Message.Id);
        }
    }
}

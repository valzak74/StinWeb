using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StinClasses.Models;
using StinWeb.Models;
using StinWeb.Models.DataManager;

namespace StinWeb.Controllers
{
    public class Rg9972Controller : Controller
    {
        private readonly StinDbContext _context;

        public Rg9972Controller(StinDbContext context)
        {
            _context = context;
        }

        // GET: Rg9972
        public async Task<IActionResult> Index()
        {
            return View(await _context.Rg9972s.ToListAsync());
        }

        [HttpGet]
        public ViewResult IndexDiagnostics()
        {
            var table = 
                from rg9972 in _context.Rg9972s
                join sc172 in _context.Sc172s on rg9972.Sp9964 equals sc172.Id into _sc172
                from sc172 in _sc172.DefaultIfEmpty()
                join sc55 in _context.Sc55s on rg9972.Sp10083 equals sc55.Id into _sc55
                from sc55 in _sc55.DefaultIfEmpty()
                join sc84 in _context.Sc84s on rg9972.Sp9960 equals sc84.Id
                join sc8840 in _context.Sc8840s on sc84.Sp8842 equals sc8840.Id into _sc8840
                from sc8840 in _sc8840.DefaultIfEmpty()
                where rg9972.Period == Common.GetRegTA(_context) && rg9972.Sp9970 > 0 && rg9972.Sp9963 == "   8IM   "
                let _партииМастерской = new ПартииМастерской {
                    Квитанция = rg9972.Sp9969 + "-" + rg9972.Sp10084.ToString(),
                    Гарантия = rg9972.Sp9958,
                    AttentionId = (rg9972.Sp9958 == 4 ? 0 : (rg9972.Sp9958 == 1 ? 1 : (rg9972.Sp9958 == 0 ? 2 : 3))),
                    КвитанцияГод = rg9972.Sp10084,
                    КвитанцияНомер = rg9972.Sp9969,
                    ТипРемонта = Common.ПолучитьТипРемонта(Convert.ToInt32(rg9972.Sp9958)) ?? "не распознан",
                    ИзделиеId = rg9972.Sp9960,
                    Изделие = sc84.Descr.Trim(),
                    Производитель = sc8840.Descr.Trim() ?? "<не указан>",
                    Заказчик = sc172.Descr.Trim() ?? sc55.Descr.Trim()
                } 
                orderby _партииМастерской.AttentionId, _партииМастерской.КвитанцияГод, _партииМастерской.КвитанцияНомер
                select _партииМастерской
                ;
            return View(table.AsNoTracking());
            //return View(_context.Set<Rg9972>());
        }
        //public async Task<IActionResult> IndexDiagnostics()
        //{
        //    //return View(await _context.Rg9972.Where(x => x.Period == Common.GetRegTA(_context) && x.Sp9970 > 0 && x.Sp9963 == "   8IM   ").ToListAsync());
        //    return View(await _context.Set<Rg9972>());
        //}

        // GET: Rg9972/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rg9972 = await _context.Rg9972s
                .FirstOrDefaultAsync(m => m.Sp9969 == id);
            if (rg9972 == null)
            {
                return NotFound();
            }

            return View(rg9972);
        }

        // GET: Rg9972/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Rg9972/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Period,Sp9958,Sp9960,Sp9961,Sp9963,Sp9964,Sp10083,Sp9967,Sp9969,Sp10084,Sp9970")] Rg9972 rg9972)
        {
            if (ModelState.IsValid)
            {
                _context.Add(rg9972);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(rg9972);
        }

        // GET: Rg9972/Edit/5
        public async Task<IActionResult> Edit(DateTime? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rg9972 = await _context.Rg9972s.FindAsync(id);
            if (rg9972 == null)
            {
                return NotFound();
            }
            return View(rg9972);
        }

        // POST: Rg9972/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DateTime id, [Bind("Period,Sp9958,Sp9960,Sp9961,Sp9963,Sp9964,Sp10083,Sp9967,Sp9969,Sp10084,Sp9970")] Rg9972 rg9972)
        {
            if (id != rg9972.Period)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(rg9972);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!Rg9972Exists(rg9972.Period))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(rg9972);
        }

        // GET: Rg9972/Delete/5
        public async Task<IActionResult> Delete(DateTime? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rg9972 = await _context.Rg9972s
                .FirstOrDefaultAsync(m => m.Period == id);
            if (rg9972 == null)
            {
                return NotFound();
            }

            return View(rg9972);
        }

        // POST: Rg9972/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(DateTime id)
        {
            var rg9972 = await _context.Rg9972s.FindAsync(id);
            _context.Rg9972s.Remove(rg9972);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool Rg9972Exists(DateTime id)
        {
            return _context.Rg9972s.Any(e => e.Period == id);
        }
    }
}

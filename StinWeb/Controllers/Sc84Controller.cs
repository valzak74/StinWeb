using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StinWeb.Classes;
using NonFactors.Mvc.Lookup;
using StinWeb.Lookups;
using StinClasses.Models;

namespace StinWeb.Controllers
{
    public class Sc84Controller : Controller
    {
        private readonly StinDbContext _context;

        public Sc84Controller(StinDbContext context)
        {
            _context = context;
        }

        // GET: Sc84
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }
            ViewData["CurrentFilter"] = searchString;
            var sc84 = from s in _context.Sc84s
                       select s;
            if (!string.IsNullOrEmpty(searchString))
            {
                sc84 = sc84.Where(s => s.Descr.Contains(searchString)
                                       || s.Sp85.Contains(searchString));
            }
            switch (sortOrder)
            {
                case "name_desc":
                    sc84 = sc84.OrderByDescending(s => s.Descr);
                    break;
                default:
                    sc84 = sc84.OrderBy(s => s.Descr);
                    break;
            }
            int pageSize = 3;
            return View(await PaginatedList<Sc84>.CreateAsync(sc84.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Sc84/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sc84 = await _context.Sc84s
                .FirstOrDefaultAsync(m => m.RowId == id);
            if (sc84 == null)
            {
                return NotFound();
            }

            return View(sc84);
        }

        // GET: Sc84/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Sc84/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RowId,Id,Parentid,Code,Descr,Isfolder,Ismark,Verstamp,Sp85,Sp86,Sp208,Sp2417,Sp97,Sp5066,Sp94,Sp4427,Sp103,Sp104,Sp8842,Sp8845,Sp8848,Sp8849,Sp8899,Sp9304,Sp9305,Sp10091,Sp10366,Sp10397,Sp10406,Sp10479,Sp10480,Sp10481,Sp10535,Sp10784,Sp11534,Sp12309,Sp12643,Sp12992,Sp13277,Sp13501,Sp95,Sp101,Sp12310")] Sc84 sc84)
        {
            if (ModelState.IsValid)
            {
                _context.Add(sc84);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(sc84);
        }

        // GET: Sc84/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sc84 = await _context.Sc84s.FindAsync(id);
            if (sc84 == null)
            {
                return NotFound();
            }
            return View(sc84);
        }

        // POST: Sc84/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RowId,Id,Parentid,Code,Descr,Isfolder,Ismark,Verstamp,Sp85,Sp86,Sp208,Sp2417,Sp97,Sp5066,Sp94,Sp4427,Sp103,Sp104,Sp8842,Sp8845,Sp8848,Sp8849,Sp8899,Sp9304,Sp9305,Sp10091,Sp10366,Sp10397,Sp10406,Sp10479,Sp10480,Sp10481,Sp10535,Sp10784,Sp11534,Sp12309,Sp12643,Sp12992,Sp13277,Sp13501,Sp95,Sp101,Sp12310")] Sc84 sc84)
        {
            if (id != sc84.RowId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(sc84);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!Sc84Exists(sc84.RowId))
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
            return View(sc84);
        }

        // GET: Sc84/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sc84 = await _context.Sc84s
                .FirstOrDefaultAsync(m => m.RowId == id);
            if (sc84 == null)
            {
                return NotFound();
            }

            return View(sc84);
        }

        // POST: Sc84/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sc84 = await _context.Sc84s.FindAsync(id);
            _context.Sc84s.Remove(sc84);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool Sc84Exists(int id)
        {
            return _context.Sc84s.Any(e => e.RowId == id);
        }

        [HttpGet("Nomenk")]
        public JsonResult Sc84(LookupFilter filter)
        {
            //filter.AdditionalFilters[nameof(Person.Income)] = income;
            //filter.AdditionalFilters[nameof(Person.IsWorking)] = isWorking;

            return Json(new Sc84Lookup(_context) { Filter = filter }.GetData());
        }

        [HttpGet("Nomenk-ru")]
        public JsonResult LocalizedSc84(LookupFilter filter)
        {
            //filter.AdditionalFilters[nameof(Person.Income)] = income;
            //filter.AdditionalFilters[nameof(Person.IsWorking)] = isWorking;

            return Json(new Sc84Lookup(_context) { Filter = filter }.GetData());
        }
    }
}

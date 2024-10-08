using Budget_Tracker.Data;
using Budget_Tracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Budget_Tracker.Controllers
{
    public class TransactionController : Controller
    {
        private readonly AppDbContext _appDbContext;

        public TransactionController(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        // GET: Transaction
        public async Task<IActionResult> Index()
        {
            var appDbContext = _appDbContext.Transactions.Include(t => t.Category);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Transaction/Details/3
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _appDbContext.Transactions
                .Include(t => t.Category)
                .FirstOrDefaultAsync(m => m.TransactionId == id);

            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        // GET: Transaction/AddOrEdit
        [HttpGet]
        public IActionResult AddOrEdit(int id = 0)
        {
            PopulateCategories();
            if (id == 0)
            {
                return View(new Transaction());
            } else
            {
                return View(_appDbContext.Transactions.Find(id));
            }
        }

        // POST: Transaction/AddOrEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([Bind("TransactionId, CategoryId, Amount, Note, Date")] Transaction transaction)
        {
            if (ModelState.IsValid)
            {
                // Logga datumet för att säkerställa att det är korrekt innan sparandet
                Console.WriteLine("Datum för transaktion innan sparandet: " + transaction.Date.ToString("yyyy-MM-dd"));

                // Säkerställ att datumet har rätt Kind (local time)
                if (transaction.Date.Kind == DateTimeKind.Unspecified)
                {
                    transaction.Date = DateTime.SpecifyKind(transaction.Date, DateTimeKind.Local);
                }

                if (transaction.TransactionId == 0)
                {
                    _appDbContext.Add(transaction);
                } else
                {
                    _appDbContext.Update(transaction);
                }

                await _appDbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PopulateCategories();
            return View(transaction);
        }

        // GET: Transaction/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _appDbContext.Transactions
                .Include(t => t.Category)
                .FirstOrDefaultAsync(m => m.TransactionId == id);

            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        // POST: Transaction/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transaction = await _appDbContext.Transactions.FindAsync(id);
            if (transaction != null)
            {
                _appDbContext.Transactions.Remove(transaction);
            }

            await _appDbContext.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // Helper method to populate categories
        public void PopulateCategories()
        {
            var categoryCollection = _appDbContext.Categories.ToList();
            Category defaultCategory = new Category() { CategoryId = 0, Title = "Choose a category" };
            categoryCollection.Insert(0, defaultCategory);
            ViewBag.Categories = categoryCollection;
        }
    }
}

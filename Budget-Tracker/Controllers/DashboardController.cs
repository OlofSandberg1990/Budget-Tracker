using Budget_Tracker.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Transactions;
using Transaction = Budget_Tracker.Models.Transaction;

namespace Budget_Tracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }



        public async Task<ActionResult> Index()
        {
            //Last 7 days transactions
            DateTime StartDate = DateTime.Now.AddDays(-6);
            DateTime EndDate = DateTime.Now;

            List<Transaction> SelectedTransactions = await _context.Transactions.Include(c => c.Category).
                Where(t => t.Date >= StartDate && t.Date <= EndDate).
                ToListAsync();

            //Total Income
            int TotalIncome = SelectedTransactions.
                Where(i => i.Category.Type == "Income").
                Sum(a => a.Amount);

            ViewBag.TotalIncome = TotalIncome.ToString("C0");

            //Total Expense
            int TotalExpense = SelectedTransactions.
                Where(i => i.Category.Type == "Expense").
                Sum(a => a.Amount);

            ViewBag.TotalExpense = TotalExpense.ToString("C0");

            //Balance   
            int Balance = TotalIncome - TotalExpense;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");

            culture.NumberFormat.CurrencyPositivePattern = 1;
            ViewBag.Balance = String.Format(culture, "{0:C0}", Balance);

            //Doughnut Chart - Expense by Category
            ViewBag.DoughnutChartData = SelectedTransactions.Where(i => i.Category.Type == "Expense")
                .GroupBy(c => c.Category.CategoryId)
                .Select(k => new
                {
                    CategoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(i => i.Amount),
                    formatedAmount = k.Sum(i => i.Amount).ToString("C0"),
                }).OrderByDescending(l => l.amount).ToList();

            //SpLine Chart - Income vs Expense


            //Income
            List<SplineChartData> IncomeSummary = SelectedTransactions.
                Where(i => i.Category.Type == "Income")
                .GroupBy(j => j.Date).Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    income = k.Sum(l => l.Amount)
                }).ToList();


            //Expense
            List<SplineChartData> ExpenseSummary = SelectedTransactions.
                Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Date).Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    expense = k.Sum(l => l.Amount)
                }).ToList();


            //Combine Income and Expense
            string[] Last7Day = Enumerable.Range(0, 7).Select(i => StartDate.AddDays(i).ToString("dd-MMM")).ToArray();

            ViewBag.SpLineChartData = from day in Last7Day
                                      join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into dayExpenseJoined
                                      from expense in dayExpenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense,
                                      };

            //Recent Transactions
            ViewBag.RecentTransactions = await _context.Transactions.
                Include(i => i.Category)
                .OrderByDescending(j => j.Date).Take(5).ToListAsync();

            return View();
        }
    }

    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;
    }
}

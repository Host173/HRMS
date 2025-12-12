using HRMS.Data;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace HRMS.Controllers
{
    public class TestController : Controller
    {
        private readonly HrmsDbContext _context;

        public TestController(HrmsDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Change Employee to any DbSet name you actually have in HrmsDbContext
            var count = _context.Employee.Count();
            return Content($"OK ✅ HRMS DB connected. Employee rows = {count}");
        }
    }
}
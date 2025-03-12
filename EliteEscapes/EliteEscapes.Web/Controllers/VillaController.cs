using EliteEscapes.Domain.Entities;
using EliteEscapes.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace EliteEscapes.Web.Controllers
{
    public class VillaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VillaController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var Villas = _context.Villas.ToList();
            return View(Villas);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(Villa obj)
        {
            if (obj.Name == obj.Description)
            {
                ModelState.AddModelError("Description", "Name and Description cannot be the same");
            }
            if (ModelState.IsValid)
            {
                _context.Villas.Add(obj);
                _context.SaveChanges();
                TempData["success"] = "Villa Created Successfully";
                return RedirectToAction("Index");
            }
            return View();
        }

        public IActionResult Update(int villaId)
        {
            var obj = _context.Villas.FirstOrDefault(v => v.Id == villaId);
            if (obj == null)
            {
                return RedirectToAction("Error","Home");
            }
            return View(obj);
        }

        [HttpPost]
        public IActionResult Update(Villa obj)
        {
            if(ModelState.IsValid)
            {
                _context.Villas.Update(obj);
                _context.SaveChanges();
                TempData["success"] = "Villa Updated Successfully";
                return RedirectToAction("Index");
            }
          
            return View(obj);
        }

        public IActionResult Delete(int villaId)
        {
            var obj = _context.Villas.FirstOrDefault(v => v.Id == villaId);
            if (obj == null)
            {
                return RedirectToAction("Error", "Home");
            }
            return View(obj);
        }

        [HttpPost]
        public IActionResult Delete(Villa obj)
        {
           var del = _context.Villas.FirstOrDefault(v => v.Id == obj.Id);
            if (del == null)
            {
                TempData["error"] = "Villa Not Deleted Successfully";
                return RedirectToAction("Error", "Home");
                
            }
            _context.Villas.Remove(del);
            _context.SaveChanges();
            TempData["success"] = "Villa Deleted Successfully";
           
            return RedirectToAction("Index");
        }
    }
}

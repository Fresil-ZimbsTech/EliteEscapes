using EliteEscapes.Application.Common.Interfaces;
using EliteEscapes.Application.Services.Interface;
using EliteEscapes.Domain.Entities;
using EliteEscapes.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EliteEscapes.Web.Controllers
{
    [Authorize]
    public class VillaController : Controller
    {
        private readonly IVillaService _villaService;

        public VillaController( IVillaService villaService)
        {
            _villaService = villaService;
        }
        public IActionResult Index()
        {
            var Villas = _villaService.GetAllVillas();
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
                _villaService.CreateVilla(obj);
                TempData["success"] = "Villa Created Successfully";
                return RedirectToAction("Index");
            }
            return View();
        }

        public IActionResult Update(int villaId)
        {
            Villa? obj = _villaService.GetVillaById(villaId);
            if (obj == null)
            {
                return RedirectToAction("Error","Home");
            }
            return View(obj);
        }

        [HttpPost]
        public IActionResult Update(Villa obj)
        {
            if(ModelState.IsValid && obj.Id > 0)
            {
               _villaService.UpdateVilla(obj);
                TempData["success"] = "Villa Updated Successfully";
                return RedirectToAction("Index");
            }
          
            return View(obj);
        }

        public IActionResult Delete(int villaId)
        {
            Villa? obj = _villaService.GetVillaById(villaId);
            if (obj == null)
            {
                return RedirectToAction("Error", "Home");
            }
            return View(obj);
        }

        [HttpPost]
        public IActionResult Delete(Villa obj)
        {
           bool deleted = _villaService.DeleteVilla(obj.Id);
            if(deleted)
            {

                TempData["success"] = "Villa Deleted Successfully";
                return RedirectToAction(nameof(Index));

            }
           else
            {
                TempData["error"] = "Failed To Delete The Villa";
            }
           return View();
        }
    }
}

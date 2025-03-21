using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EliteEscapes.Web.Models;
using EliteEscapes.Application.Common.Interfaces;
using EliteEscapes.Web.ViewModels;
using EliteEscapes.Application.Common.Utility;
using EliteEscapes.Domain.Entities;

namespace EliteEscapes.Web.Controllers;

public class HomeController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public HomeController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        HomeVM homeVM = new()
        {
            VillaList = _unitOfWork.Villa.GetAll(includeProperties: "VillaAmenity"),
            Nights = 1,
            CheckInDate = DateOnly.FromDateTime(DateTime.Now),

        };
        return View(homeVM);
    }

    [HttpPost]

    public IActionResult GetVillasByDate(int nights, DateOnly checkInDate)
    {
        var villaList = _unitOfWork.Villa.GetAll(includeProperties: "VillaAmenity").ToList();
        var villNumberList = _unitOfWork.VillaNumber.GetAll().ToList();
        var bookedVilla = _unitOfWork.Booking.GetAll(x => x.Status == SD.StatusApproved || x.Status == SD.StatusCheckedIn).ToList();

        foreach (var villa in villaList)
        {
            int roomAvailable = SD.VillaRoomsAvailable_Count(villa.Id, villNumberList, checkInDate, nights, bookedVilla);

            villa.IsAvailable = roomAvailable > 0 ? true : false;
        }
        HomeVM homeVM = new()
        {
            VillaList = villaList,
            Nights = nights,
            CheckInDate = checkInDate
        };
        return PartialView("_VillaList", homeVM);
    }


    public IActionResult Privacy()
    {
        return View();

    }


    public IActionResult Error()
    {
        return View();
    }
}

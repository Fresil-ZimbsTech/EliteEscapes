using System.Security.Claims;
using EliteEscapes.Application.Common.Interfaces;
using EliteEscapes.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EliteEscapes.Web.Controllers
{
    public class BookingController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public BookingController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [Authorize]
        public IActionResult FinalizeBooking(int villaId,DateOnly checkInDate, int nights)
        {

            var cliamIdentity = (ClaimsIdentity)User.Identity;
            var userId = cliamIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ApplicationUser user = _unitOfWork.User.Get(x => x.Id == userId);

            Booking booking = new ()
            {
                VillaId = villaId,
                CheckInDate = checkInDate,
                Nights = nights,
                CheckOutDate = checkInDate.AddDays(nights),
                Villa= _unitOfWork.Villa.Get(x=> x.Id == villaId, includeProperties: "VillaAmenity"),
                UserId = userId,
                Phone = user.PhoneNumber,
                Email = user.Email,
                Name = user.Name
            };
            booking.TotalCost = booking.Villa.Price * nights;
            return View(booking);
        }
    }
}

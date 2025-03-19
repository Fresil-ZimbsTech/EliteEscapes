using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EliteEscapes.Application.Common.Interfaces;
using EliteEscapes.Application.Common.Utility;
using EliteEscapes.Domain.Entities;
using EliteEscapes.Infrastructure.Data;

namespace EliteEscapes.Infrastructure.Repository
{
    public class BookinRepository : Repository<Booking>, IBookingRepository
    {
        private readonly ApplicationDbContext _db;

        public BookinRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }
        public void Update(Booking entity)
        {
            _db.Bookings.Update(entity);
        }

        public void UpdateStatus(int bookingId, string bookingStatus)
        {
           var bookingFromDb = _db.Bookings.FirstOrDefault(x=>x.Id == bookingId);

            if(bookingStatus != null)
            {
                bookingFromDb.Status = bookingStatus;

                if(bookingStatus == SD.StatusCheckedIn)
                {
                    bookingFromDb.ActualCheckInDate = DateTime.Now;
                }
                if (bookingStatus == SD.StatusCompleted)
                {
                    bookingFromDb.ActualCheckOutDate = DateTime.Now;
                }

            }

        }

        public void UpdateStripePaymentID(int bookingId, string sessionId, string paymentIntentId)
        {
            var bookingFromDb = _db.Bookings.FirstOrDefault(x => x.Id == bookingId);
            if(bookingFromDb != null)
            {
                if(!string.IsNullOrEmpty(sessionId))
                {
                    bookingFromDb.StripeSessionId = sessionId;
                }
                if(!string.IsNullOrEmpty(paymentIntentId))
                {
                    bookingFromDb.StripePaymentIntentId = paymentIntentId;
                    bookingFromDb.PaymentDate = DateTime.Now;
                    bookingFromDb.IsPaymentSuccessful = true;
                }
            }

        }
    }

}


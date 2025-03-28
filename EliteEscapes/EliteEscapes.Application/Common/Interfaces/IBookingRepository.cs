using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EliteEscapes.Domain.Entities;

namespace EliteEscapes.Application.Common.Interfaces
{
   public interface IBookingRepository : IRepository<Booking>
    {
        void Update(Booking entity);
        
    }
}

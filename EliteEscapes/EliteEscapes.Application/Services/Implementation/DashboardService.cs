using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EliteEscapes.Application.Common.Interfaces;
using EliteEscapes.Application.Common.Utility;
using EliteEscapes.Application.Services.Interface;
using EliteEscapes.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EliteEscapes.Application.Services.Implementation
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        static int previousMonth = DateTime.Now.Month == 1 ? 12 : DateTime.Now.Month - 1;
        readonly DateTime previousMonthStartDate = new(DateTime.Now.Year, previousMonth, 1);
        readonly DateTime currentMonthStartDate = new(DateTime.Now.Year, DateTime.Now.Month, 1);

        public DashboardService(IUnitOfWork unitOfWork)
        {
          _unitOfWork = unitOfWork;
        }

        public async Task<PieChartDto> GetBookingPieChartData()
        {
            var totalBookings = _unitOfWork.Booking.GetAll(u => u.BookingDate >= DateTime.Now.AddDays(-30) && (u.Status != SD.StatusPending || u.Status == SD.StatusCancelled));

            var customerWithOneBooking = totalBookings.GroupBy(u => u.UserId).Where(x => x.Count() == 1).Select(u => u.Key).ToList();

            int bookingByNewCustomer = customerWithOneBooking.Count();

            int bookingByReturningCustomer = totalBookings.Count() - bookingByNewCustomer;

            PieChartDto pieChartDto = new()
            {
                Labels = new string[] { "New Customer Bookings", "Returning Customer Bookings" },
                Series = new decimal[] { bookingByNewCustomer, bookingByReturningCustomer }
            };
            return pieChartDto;
        }
        public async Task<LineChartDto> GetMemberAndBookingLineChartData()
        {
            var bookingData = _unitOfWork.Booking.GetAll(u => u.BookingDate >= DateTime.Now.AddDays(-30) && u.BookingDate.Date <= DateTime.Now)
                .GroupBy(x => x.BookingDate.Date)
                .Select(u => new
                {
                    DateTime = u.Key,
                    NewBookingCount = u.Count()
                });

            var customerData = _unitOfWork.User.GetAll(u => u.CreatedAt >= DateTime.Now.AddDays(-30) && u.CreatedAt.Date <= DateTime.Now)
               .GroupBy(x => x.CreatedAt.Date)
               .Select(u => new
               {
                   DateTime = u.Key,
                   NewCustomerCount = u.Count()
               });


            var leftJoin = bookingData.GroupJoin(customerData, booking => booking.DateTime, customer => customer.DateTime,
                 (booking, customer) => new
                 {
                     booking.DateTime,
                     booking.NewBookingCount,
                     NewCustomerCount = customer.Select(x => x.NewCustomerCount).FirstOrDefault()
                 });
            var rightJoin = customerData.GroupJoin(bookingData, customer => customer.DateTime, booking => booking.DateTime,
                (customer, booking) => new
                {
                    customer.DateTime,
                    NewBookingCount = booking.Select(x => x.NewBookingCount).FirstOrDefault(),
                    customer.NewCustomerCount
                });

            var mergedData = leftJoin.Union(rightJoin).OrderBy(x => x.DateTime).ToList();

            var newBookingData = mergedData.Select(x => x.NewBookingCount).ToArray();
            var newCustomerData = mergedData.Select(x => x.NewCustomerCount).ToArray();
            var categories = mergedData.Select(x => x.DateTime.ToString("MM/dd/yyyy")).ToArray();

            List<ChartData> chartDataList = new()
            {
                new ChartData
                {
                    Name="New Bookings",
                    Data=newBookingData,
                },
                new ChartData
                {
                    Name="New Customers",
                    Data=newCustomerData,
                }
            };
            LineChartDto lineChartDto = new()
            {
                Catagories = categories,
                Series = chartDataList
            };
            return lineChartDto;
        }
        public async Task<RadialBarChartDto> GetRegisteredUserChartData()
        {
            var totalUsers = _unitOfWork.User.GetAll();

            var countByCurrentMonth = totalUsers.Count(u => u.CreatedAt >= currentMonthStartDate && u.CreatedAt <= DateTime.Now);

            var countByPreviousMonth = totalUsers.Count(u => u.CreatedAt >= previousMonthStartDate && u.CreatedAt <= currentMonthStartDate);

            return SD.GetRadialCartDataModel(totalUsers.Count(), countByCurrentMonth, countByPreviousMonth);
        }
        public async Task<RadialBarChartDto> GetRevenueChartData()
        {
            var totalBookings = _unitOfWork.Booking.GetAll(u => u.Status ==SD.StatusApproved || u.Status == SD.StatusCheckedIn || u.Status == SD.StatusCompleted);

            var totalRevenue = Convert.ToInt32(totalBookings.Sum(u => u.TotalCost));

            var countByCurrentMonth = totalBookings.Where(u => u.BookingDate >= currentMonthStartDate &&
            u.BookingDate <= DateTime.Now).Sum(u => u.TotalCost);

            var countByPreviousMonth = totalBookings.Where(u => u.BookingDate >= previousMonthStartDate &&
            u.BookingDate <= currentMonthStartDate).Sum(u => u.TotalCost);

            return SD.GetRadialCartDataModel(totalRevenue, countByCurrentMonth, countByPreviousMonth);
        }
        public async Task<RadialBarChartDto> GetTotalBookingRadialChartData()
        {
            var totalbookings = _unitOfWork.Booking.GetAll(u => u.Status != SD.StatusPending || u.Status == SD.StatusCancelled);

            var countByCurrentMonth = totalbookings.Count(u => u.BookingDate >= currentMonthStartDate && u.BookingDate <= DateTime.Now);

            var countByPreviousMonth = totalbookings.Count(u => u.BookingDate >= previousMonthStartDate && u.BookingDate <= currentMonthStartDate);

            return SD.GetRadialCartDataModel(totalbookings.Count(), countByCurrentMonth, countByPreviousMonth);
        }
       
    }
}

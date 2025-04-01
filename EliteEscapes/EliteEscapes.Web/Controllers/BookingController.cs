using System.Security.Claims;
using EliteEscapes.Application.Common.Interfaces;
using EliteEscapes.Application.Common.Utility;
using EliteEscapes.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIO;
using Syncfusion.DocIORenderer;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using EliteEscapes.Application.Services.Interface;
using Microsoft.AspNetCore.Identity;
using EliteEscapes.Application.Contract;

namespace EliteEscapes.Web.Controllers
{
    public class BookingController : Controller
    {
       
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IVillaService _villaService;
        private readonly IBookingService _bookingService;
        private readonly IVillaNumberService _villaNumberService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPaymentService _paymentService;
        private readonly IEmailService _emailService;

        public BookingController(IWebHostEnvironment webHostEnvironment,IVillaService villaService,IBookingService bookingService,IVillaNumberService villaNumberService,UserManager<ApplicationUser> userManager,IPaymentService paymentService,IEmailService emailService)
        {
            
            _webHostEnvironment = webHostEnvironment;
            _villaService = villaService;
            _bookingService = bookingService;
            _villaNumberService = villaNumberService;
            _userManager = userManager;
            _paymentService = paymentService;
            _emailService = emailService;
        }

        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult FinalizeBooking(int villaId, string checkInDate, int nights)
        {

            var cliamIdentity = (ClaimsIdentity)User.Identity;
            var userId = cliamIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ApplicationUser user = _userManager.FindByIdAsync(userId).GetAwaiter().GetResult();

            DateOnly parsedCheckInDate = DateOnly.ParseExact(checkInDate, "dd/MM/yyyy");


            Booking booking = new()
            {
                VillaId = villaId,
                CheckInDate = parsedCheckInDate,
                Nights = nights,
                CheckOutDate = parsedCheckInDate.AddDays(nights),
                Villa = _villaService.GetVillaById(villaId),
                UserId = userId,
                Phone = user.PhoneNumber,
                Email = user.Email,
                Name = user.Name
            };
            booking.TotalCost = booking.Villa.Price * nights;
            
            return View(booking);
        }

        [Authorize]
        [HttpPost]
        public IActionResult FinalizeBooking(Booking booking)
        {
            var villa = _villaService.GetVillaById(booking.VillaId);
            booking.CheckOutDate = booking.CheckInDate.AddDays(booking.Nights);
            booking.TotalCost = villa.Price * booking.Nights;
            booking.Status = SD.StatusPending;
            booking.BookingDate = DateTime.Now;

            if(!_villaService.IsVillaAvailableByDate(villa.Id,booking.Nights,booking.CheckInDate))
            {
                TempData["Error"] = " Rooms  Has Been Sold Out!";

                return RedirectToAction(nameof(FinalizeBooking), new 
                {
                    villaId = booking.VillaId,
                    checkInDate = booking.CheckInDate,
                    nights = booking.Nights 
                });
            }

            _bookingService.CreateBooking(booking);

            var domain = Request.Scheme + "://" + Request.Host.Value + "/";
            
            var options = _paymentService.CreateStripeSessionOptions(booking,villa,domain);

            var session = _paymentService.CreateStripeSession(options);
            _bookingService.UpdateStripePaymentID(booking.Id, session.Id, session.PaymentIntentId);
            

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);

        }

        [Authorize]
        public IActionResult BookingConfirmation(int bookingId)
        {
            Booking bookingFromDb = _bookingService.GetBookingById(bookingId);

            if (bookingFromDb.Status == SD.StatusPending)
            {
                //this is a pending order, we need to confirm if payment was successful

                var service = new SessionService();
                Session session = service.Get(bookingFromDb.StripeSessionId);

                if (session.PaymentStatus == "paid")
                {
                    _bookingService.UpdateStatus(bookingFromDb.Id, SD.StatusApproved,0);
                    _bookingService.UpdateStripePaymentID(bookingFromDb.Id, session.Id, session.PaymentIntentId);

                    // Generate Invoice PDF
                    byte[] pdfBytes = GenerateInvoicePDF(bookingFromDb);

                    // Send Email with Invoice
                    _emailService.SendEmailWithAttachmentAsync(
                        bookingFromDb.Email,
                        "Booking Confirmation - EliteEscapes",
                        "<p>Your booking has been confirmed. Please find the attached invoice.</p>",
                        pdfBytes,
                        "BookingInvoice.pdf"
                    );

                }
            }
            return View(bookingFromDb);
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetAll(string status)
        {
            IEnumerable<Booking> objBookings;
            string userId = "";

            if (string.IsNullOrEmpty(status))
            {
                status = "";
            }
           if(!User.IsInRole(SD.Role_Admin))
            {
                var claimsIdentity =(ClaimsIdentity)User.Identity;
                userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            }
           objBookings = _bookingService.GetAllBookings(userId, status);
           
            return Json(new { data = objBookings });
        }

        [Authorize]
        public IActionResult BookingDetails(int bookingId)
        {
            Booking bookingFromDb = _bookingService.GetBookingById(bookingId);

            if(bookingFromDb.VillaNumber==0 && bookingFromDb.Status == SD.StatusApproved)
            {
                var availableVillaNumber = AssignAvailableVillaNumberByVilla(bookingFromDb.VillaId);

                bookingFromDb.VillaNumbers = _villaNumberService.GetAllVillaNumbers().Where(u=>u.VillaId == bookingFromDb.VillaId && availableVillaNumber.Any(x=>x==u.Villa_Number)).ToList();
            }


            return View(bookingFromDb);
        }

        [HttpPost]
        [Authorize]
        public IActionResult GenerateInvoice(int id,string downloadType)
        {
            string basePath = _webHostEnvironment.WebRootPath;

            WordDocument document = new WordDocument();


            // Load the template.
            string dataPath = basePath + @"/exports/BookingDetails.docx";
            using FileStream fileStream = new(dataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            document.Open(fileStream, FormatType.Automatic);

            //Update Template
            Booking bookingFromDb = _bookingService.GetBookingById(id);

            TextSelection textSelection = document.Find("xx_customer_name", false, true);
            WTextRange textRange = textSelection.GetAsOneRange();
            textRange.Text = bookingFromDb.Name;

            textSelection = document.Find("xx_customer_phone", false, true);
            textRange = textSelection.GetAsOneRange();
            textRange.Text = bookingFromDb.Phone;

            textSelection = document.Find("xx_customer_email", false, true);
            textRange = textSelection.GetAsOneRange();
            textRange.Text = bookingFromDb.Email;

            textSelection = document.Find("XX_BOOKING_NUMBER", false, true);
            textRange = textSelection.GetAsOneRange();
            textRange.Text = "BOOKING ID - " + bookingFromDb.Id;
            textSelection = document.Find("XX_BOOKING_DATE", false, true);
            textRange = textSelection.GetAsOneRange();
            textRange.Text = "BOOKING DATE - " + bookingFromDb.BookingDate.ToShortDateString();


            textSelection = document.Find("xx_payment_date", false, true);
            textRange = textSelection.GetAsOneRange();
            textRange.Text = bookingFromDb.PaymentDate.ToShortDateString();
            textSelection = document.Find("xx_checkin_date", false, true);
            textRange = textSelection.GetAsOneRange();
            textRange.Text = bookingFromDb.CheckInDate.ToShortDateString();
            textSelection = document.Find("xx_checkout_date", false, true);
            textRange = textSelection.GetAsOneRange();
            textRange.Text = bookingFromDb.CheckOutDate.ToShortDateString(); ;
            textSelection = document.Find("xx_booking_total", false, true);
            textRange = textSelection.GetAsOneRange();
            textRange.Text = bookingFromDb.TotalCost.ToString("c");

            WTable table = new(document);

            table.TableFormat.Borders.LineWidth = 1f;
            table.TableFormat.Borders.Color = Color.Black;
            table.TableFormat.Paddings.Top = 7f;
            table.TableFormat.Paddings.Bottom = 7f;
            table.TableFormat.Borders.Horizontal.LineWidth = 1f;

            int rows = bookingFromDb.VillaNumber > 0 ? 3 : 2;
            table.ResetCells(rows, 4);

            WTableRow row0 = table.Rows[0];

            row0.Cells[0].AddParagraph().AppendText("NIGHTS");
            row0.Cells[0].Width = 80;
            row0.Cells[1].AddParagraph().AppendText("VILLA");
            row0.Cells[1].Width = 220;
            row0.Cells[2].AddParagraph().AppendText("PRICE PER NIGHT");
            row0.Cells[3].AddParagraph().AppendText("TOTAL");
            row0.Cells[3].Width = 80;

            WTableRow row1 = table.Rows[1];

            row1.Cells[0].AddParagraph().AppendText(bookingFromDb.Nights.ToString());
            row1.Cells[0].Width = 80;
            row1.Cells[1].AddParagraph().AppendText(bookingFromDb.Villa.Name);
            row1.Cells[1].Width = 220;
            row1.Cells[2].AddParagraph().AppendText((bookingFromDb.TotalCost / bookingFromDb.Nights).ToString("c"));
            row1.Cells[3].AddParagraph().AppendText(bookingFromDb.TotalCost.ToString("c"));
            row1.Cells[3].Width = 80;

            if(bookingFromDb.VillaNumber > 0)
            {
                WTableRow row2 = table.Rows[2];

                row2.Cells[0].Width = 80;
                row2.Cells[1].AddParagraph().AppendText("Villa Number - " + bookingFromDb.VillaNumber.ToString());
                row2.Cells[1].Width = 220;
                row2.Cells[3].Width = 80;
            }

            WTableStyle tableStyle = document.AddTableStyle("CustomStyle") as WTableStyle;
            tableStyle.TableProperties.RowStripe = 1;
            tableStyle.TableProperties.ColumnStripe = 2;
            tableStyle.TableProperties.Paddings.Top = 2;
            tableStyle.TableProperties.Paddings.Bottom = 1;
            tableStyle.TableProperties.Paddings.Left = 5.4f;
            tableStyle.TableProperties.Paddings.Right = 5.4f;

            ConditionalFormattingStyle firstRowStyle = tableStyle.ConditionalFormattingStyles.Add(ConditionalFormattingType.FirstRow);
            firstRowStyle.CharacterFormat.Bold = true;
            firstRowStyle.CharacterFormat.TextColor = Color.FromArgb(255, 255, 255, 255);
            firstRowStyle.CellProperties.BackColor = Color.Black;

            table.ApplyStyle("CustomStyle");

            TextBodyPart bodyPart = new(document);
            bodyPart.BodyItems.Add(table);

            document.Replace("<ADDTABLEHERE>", bodyPart, false, false);


            using DocIORenderer renderer = new();

            MemoryStream stream = new();
           if(downloadType == "word")
            {
                document.Save(stream, FormatType.Docx);
                stream.Position = 0;
                return File(stream, "application/docx", "BookingDetails.docx");
            }
           else
            {
                PdfDocument pdfDocument = renderer.ConvertToPDF(document);
                pdfDocument.Save(stream);
                stream.Position = 0;
                return File(stream, "application/pdf", "BookingDetails.pdf");
            }
           

        }

        private byte[] GenerateInvoicePDF(Booking booking)
        {
            using WordDocument document = new();
            string basePath = _webHostEnvironment.WebRootPath;
            string dataPath = basePath + @"/exports/BookingDetails.docx";

            using FileStream fileStream = new(dataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            document.Open(fileStream, FormatType.Automatic);

            document.Replace("xx_customer_name", booking.Name, false, true);
            document.Replace("xx_customer_phone", booking.Phone, false, true);
            document.Replace("xx_customer_email", booking.Email, false, true);
            document.Replace("XX_BOOKING_NUMBER", "BOOKING ID - " + booking.Id, false, true);
            document.Replace("XX_BOOKING_DATE", "BOOKING DATE - " + booking.BookingDate.ToShortDateString(), false, true);
            document.Replace("xx_payment_date", booking.PaymentDate.ToShortDateString(), false, true);
            document.Replace("xx_checkin_date", booking.CheckInDate.ToShortDateString(), false, true);
            document.Replace("xx_checkout_date", booking.CheckOutDate.ToShortDateString(), false, true);
            document.Replace("xx_booking_total", booking.TotalCost.ToString("c"), false, true);

            WTable table = new(document);

            table.TableFormat.Borders.LineWidth = 1f;
            table.TableFormat.Borders.Color = Color.Black;
            table.TableFormat.Paddings.Top = 7f;
            table.TableFormat.Paddings.Bottom = 7f;
            table.TableFormat.Borders.Horizontal.LineWidth = 1f;

            int rows = booking.VillaNumber > 0 ? 3 : 2;
            table.ResetCells(rows, 4);

            WTableRow row0 = table.Rows[0];

            row0.Cells[0].AddParagraph().AppendText("NIGHTS");
            row0.Cells[0].Width = 80;
            row0.Cells[1].AddParagraph().AppendText("VILLA");
            row0.Cells[1].Width = 220;
            row0.Cells[2].AddParagraph().AppendText("PRICE PER NIGHT");
            row0.Cells[3].AddParagraph().AppendText("TOTAL");
            row0.Cells[3].Width = 80;

            WTableRow row1 = table.Rows[1];

            row1.Cells[0].AddParagraph().AppendText(booking.Nights.ToString());
            row1.Cells[0].Width = 80;
            row1.Cells[1].AddParagraph().AppendText(booking.Villa.Name);
            row1.Cells[1].Width = 220;
            row1.Cells[2].AddParagraph().AppendText((booking.TotalCost / booking.Nights).ToString("c"));
            row1.Cells[3].AddParagraph().AppendText(booking.TotalCost.ToString("c"));
            row1.Cells[3].Width = 80;

            if (booking.VillaNumber > 0)
            {
                WTableRow row2 = table.Rows[2];

                row2.Cells[0].Width = 80;
                row2.Cells[1].AddParagraph().AppendText("Villa Number - " + booking.VillaNumber.ToString());
                row2.Cells[1].Width = 220;
                row2.Cells[3].Width = 80;
            }

            WTableStyle tableStyle = document.AddTableStyle("CustomStyle") as WTableStyle;
            tableStyle.TableProperties.RowStripe = 1;
            tableStyle.TableProperties.ColumnStripe = 2;
            tableStyle.TableProperties.Paddings.Top = 2;
            tableStyle.TableProperties.Paddings.Bottom = 1;
            tableStyle.TableProperties.Paddings.Left = 5.4f;
            tableStyle.TableProperties.Paddings.Right = 5.4f;

            ConditionalFormattingStyle firstRowStyle = tableStyle.ConditionalFormattingStyles.Add(ConditionalFormattingType.FirstRow);
            firstRowStyle.CharacterFormat.Bold = true;
            firstRowStyle.CharacterFormat.TextColor = Color.FromArgb(255, 255, 255, 255);
            firstRowStyle.CellProperties.BackColor = Color.Black;

            table.ApplyStyle("CustomStyle");

            TextBodyPart bodyPart = new(document);
            bodyPart.BodyItems.Add(table);

            document.Replace("<ADDTABLEHERE>", bodyPart, false, false);

            using DocIORenderer renderer = new();
            using PdfDocument pdfDocument = renderer.ConvertToPDF(document);

            using MemoryStream stream = new();
            pdfDocument.Save(stream);
            return stream.ToArray();
        }

        [HttpPost]
        [Authorize(Roles =SD.Role_Admin)]
        public IActionResult CheckIn(Booking booking)
        {
            _bookingService.UpdateStatus(booking.Id, SD.StatusCheckedIn, booking.VillaNumber);
            TempData["Success"] = "Booking Updated Succesfully";
            return RedirectToAction(nameof(BookingDetails) , new {bookingId = booking.Id});
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult CheckOut(Booking booking)
        {
            _bookingService.UpdateStatus(booking.Id, SD.StatusCompleted, booking.VillaNumber);
            TempData["Success"] = "Booking Completed Succesfully";
            return RedirectToAction(nameof(BookingDetails), new { bookingId = booking.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult CancelBooking(Booking booking)
        {
            _bookingService.UpdateStatus(booking.Id, SD.StatusCancelled, 0);
            TempData["Success"] = "Booking Cancelled Succesfully";
            return RedirectToAction(nameof(BookingDetails), new { bookingId = booking.Id });
        }
        private List<int> AssignAvailableVillaNumberByVilla(int villaId)
        {
            List<int> availableVillaNumbers = new();
            var villaNumbers = _villaNumberService.GetAllVillaNumbers().Where(x => x.VillaId == villaId);
            var checkInVilla = _bookingService.GetCheckedInVillaNumber(villaId);

            foreach (var villaNumber in villaNumbers)
            {
                if(!checkInVilla.Contains(villaNumber.Villa_Number))
                {
                    availableVillaNumbers.Add(villaNumber.Villa_Number);
                }
            }
            return availableVillaNumbers;
        }
    }
}

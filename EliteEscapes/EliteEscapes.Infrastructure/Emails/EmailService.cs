using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EliteEscapes.Application.Contract;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace EliteEscapes.Infrastructure.Emails
{
    public class EmailService : IEmailService
    {
        private readonly string _sendGridKey;
        public EmailService(IConfiguration configuration)
        {
            _sendGridKey = configuration["SendGrid:Key"];
            Console.WriteLine($"SendGrid API Key: {_sendGridKey}");
        }
        public async Task<bool> SendEmailAsync(string email, string subject, string message)
        {
            var client = new SendGridClient(_sendGridKey);
            var from = new EmailAddress("technicalfresil@gmail.com", "ElliteEscapes - Diya & Fresil");
            var to = new EmailAddress(email);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", message);
            var response = await client.SendEmailAsync(msg);

            if(response.StatusCode==System.Net.HttpStatusCode.Accepted|| response.StatusCode==System.Net.HttpStatusCode.OK)
            {
                return true;
            }
            return false;
        }

        public async Task<bool> SendEmailWithAttachmentAsync(string email, string subject, string message, byte[] attachmentData, string fileName)
        {
            var client = new SendGridClient(_sendGridKey);
            var from = new EmailAddress("technicalfresil@gmail.com", "ElliteEscapes - Diya & Fresil");
            var to = new EmailAddress(email);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", message);
           

            if (attachmentData != null && attachmentData.Length > 0)
            {
                var attachment = new Attachment
                {
                    Content = Convert.ToBase64String(attachmentData),
                    Filename = fileName,
                    Type = "application/pdf",
                    Disposition = "attachment"
                };

                msg.AddAttachment(attachment);
            }
            var response = await client.SendEmailAsync(msg);

            if (response.StatusCode == System.Net.HttpStatusCode.Accepted || response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return true;
            }
            return false;
        }
    }
}

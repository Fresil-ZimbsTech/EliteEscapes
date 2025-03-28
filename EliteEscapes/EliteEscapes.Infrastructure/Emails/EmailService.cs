using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EliteEscapes.Application.Contract;

namespace EliteEscapes.Infrastructure.Emails
{
    public class EmailService : IEmailService
    {
        public Task<bool> SendEmailAsync(string email, string subject, string message)
        {
            throw new NotImplementedException();
        }
    }
}

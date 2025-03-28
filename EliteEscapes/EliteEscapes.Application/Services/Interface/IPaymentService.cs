using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EliteEscapes.Domain.Entities;
using Stripe.Checkout;


namespace EliteEscapes.Application.Services.Interface
{
    public interface IPaymentService
    {
        SessionCreateOptions CreateStripeSessionOptions(Booking booking, Villa villa, string domain);
        Session CreateStripeSession(SessionCreateOptions options);

    }
}

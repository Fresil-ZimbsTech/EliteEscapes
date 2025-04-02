# EliteEscapes

## Overview

EliteEscapes is a villa and resort booking platform designed for vacation rentals. It provides a seamless and smart booking experience, allowing users to select a villa, choose their stay duration, and complete payments securely. The platform also includes an advanced admin panel with analytics and revenue tracking.

## Features

### **For Customers:**

- Search for available villas based on preferred dates.
- View villa details, including images and pricing.
- Book a villa by selecting the number of nights and completing the payment.
- Multiple payment options: Credit/Debit Card, Net Banking, QR Code.
- Receive booking confirmation and invoice via email (PDF format).

### **For Admins:**

- **Dashboard with analytics:**
  - Revenue charts (monthly trends with positive/negative indicators).
  - New customers vs. returning customers chart.
  - Total bookings summary.
- Manage villa listings and availability.
- Check-in and check-out management.

## Tech Stack

- **Backend:** ASP.NET 8 MVC (Clean Architecture)
- **Frontend:**ASP.NET core
- **Database:** Microsoft SQL Server (SSMS)
- **Payment Gateway:** Stripe / Razorpay (for secure transactions)
- **Email Service:** Integrated via API

## Installation & Setup

### **Prerequisites:**

- .NET 8 SDK
- SQL Server (SSMS)
- Visual Studio / VS Code

### **Steps:**

1. Clone the repository:
   ```sh
   git clone https://github.com/your-repo/EliteEscapes.git
   ```
2. Navigate to the project directory:
   ```sh
   cd EliteEscapes
   ```
3. Set up the database:
   - Update `appsettings.json` with your SQL Server connection string.
   - Run migrations:
     ```sh
     dotnet ef database update
     ```
4. Build and run the project:
   ```sh
   dotnet run
   ```
5. Access the application in the browser at `http://localhost:5000`

## Usage Guide

- **Customer Booking:**
  - Search for available villas.
  - Select desired villa and enter the stay duration.
  - Complete payment using available methods.
  - Receive email confirmation with booking details.
- **Admin Management:**
  - Monitor revenue trends and booking statistics.
  - Check-in and check-out customers.
  - Send automated emails and generate invoices.

##Contributing
Feel free to fork the repository and submit pull requests for improvements or bug fixes.


## License

This project is licensed under the MIT License. Feel free to modify and contribute!


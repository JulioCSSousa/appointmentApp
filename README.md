# 🗓️ Multi-Purpose Booking & Appointment Management System

A complete, production-ready management and appointment scheduling system developed using **ASP.NET Core MVC**. Built with a clean architecture focus, high data security, and Role-Based Access Control (RBAC) to manage administrators, staff, and clients seamlessly.

Due to its generic and flexible core, this system can be easily customized for **any appointment-based business**, such as Barber Shops, Beauty Salons, Tattoo Studios, Pet Shops/Grooming, and private clinics.

---

## 🚀 Key Features

* **Robust Authentication & Authorization:** Secure user login and registration powered by **ASP.NET Core Identity**.
* **Role-Based Access Control (RBAC):**
  * **Admin:** Full administrative dashboard to manage professionals, services, pricing, schedules, and financial revenue insights. 
  * **Client:** Dedicated logged-in area to view available time slots and book appointments.
  * **Worker/Staff:** Exclusive staff view featuring a dynamic **KANBAN Board** to monitor the real-time waiting queue and smoothly update appointment statuses (e.g., *Waiting*, *In Progress*, *Completed*).
* **Double-Layer Data Validation & Security:**
  * Strict validation on both Front-end (JavaScript/HTML5) and Back-end (`ModelState` / Data Annotations).
  * Built-in protection against cross-site request forgery using Anti-Forgery Tokens (`[ValidateAntiForgeryToken]`).
* **Asynchronous Email Communication:** Automated welcome and notification emails triggered in the background using **MailKit**, keeping the UI fast and responsive.
* **Environment-Driven Configuration:** Complete isolation of sensitive data (API keys, SMTP credentials, and database strings) using environment variables, fully prepared for safe cloud deployment.

---

## 🛠️ Tech Stack

* **Back-End:** .NET 8 / C#
* **Web Framework:** ASP.NET Core MVC
* **Data Persistence:** Entity Framework Core (Code-First)
* **Database:** SQL Server / PostgreSQL / MySQL (Production-ready)
* **Authentication:** ASP.NET Core Identity
* **Email Service:** MailKit / MimeKit (SMTP Integration)
* **Front-End:** Razor Views, Bootstrap 5, JavaScript, CSS3

---

## ⚙️ Local Setup

Follow the steps below to get the project running locally on your machine:

### 1. Clone the repository
```bash
git clone [https://github.com/YourUsername/appointmentApp.git](https://github.com/YourUsername/appointmentApp.git)
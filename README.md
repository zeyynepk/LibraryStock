📚 LibraryStock

LibraryStock is an ASP.NET Core–based web application developed to manage library and stock processes with role-based authorization.
The system centralizes user management, stock tracking, order creation, and order approval in a secure and structured way.


🎯 Project Purpose

This project was developed to:
	•	Manage stock operations efficiently
	•	Control user access based on roles
	•	Make order processes more secure and traceable

for small to medium-sized organizations.


🧩 Features
	•	🔐 Role-Based Authorization
	•	Admin
	•	Staff
	•	👤 User Management (Admin Only)
	•	Add, update, and delete users
	•	Assign roles
	•	📦 Stock Management
	•	View stock items
	•	Add to stock / reduce stock
	•	Critical stock level tracking
	•	📝 Order Management
	•	Create orders
	•	Approve orders (Admin only)
	•	Prevent non-critical orders until critical items are handled
	•	📧 Email-Based Password Reset
	•	Verification code sent via SMTP


🧱 Technologies Used
	•	ASP.NET Core (.NET 8)
	•	Blazor Server
	•	Entity Framework Core
	•	SQL Server
	•	HTML / CSS (Bootstrap)


📋 Requirements

To run this project, the following are required:
	•	.NET SDK 8.0
	•	SQL Server (LocalDB or SQL Server Express is sufficient)
	•	SMTP-enabled email account
	•	(e.g. Gmail with App Password)
	•	macOS, Windows, or Linux


📦 Libraries & Packages

The project uses the following main libraries:
	•	Microsoft.EntityFrameworkCore
	•	Microsoft.EntityFrameworkCore.SqlServer
	•	Microsoft.EntityFrameworkCore.Tools
	•	Microsoft.AspNetCore.Components
	•	System.Net.Mail (for SMTP email operations)

Additional libraries are part of the standard .NET ecosystem.


⚙️ Installation

1. **Clone the repository:**

```bash
git clone https://github.com/zeyynepk/LibraryStock.git
```

2. **Navigate to the project directory:**

```bash
cd LibraryStock.App.Clean
```

3. **Configure the following in `appsettings.Development.json`:**
- Database connection string  
- SMTP email settings  

4. **Run the application:**

```bash
dotnet run
```

5. **Open in your browser:**

`http://localhost:5100`

🔐 Security Notes
	•	Database credentials and SMTP passwords must NOT be committed to GitHub.
	•	appsettings.Development.json is excluded via .gitignore.
	•	Sample configuration values are provided in appsettings.json.


📂 Project Structure (Overview)
	•	Components/ → UI components and pages
	•	Services/ → Business logic (Auth, Stock, Orders, Users)
	•	Models/ → Database models
	•	Data/ → DbContext and EF Core configuration


👩‍💻 Developer

Zeynep Kediz
Computer Engineering Student
Focused on ASP.NET Core & Blazor projects

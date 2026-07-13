# Database Migration and Usage Guide

First, let's address your questions about how the data is stored, the tools used (SSMS vs. MySQL), and how to maintain the same functionality!

## 1. How is your data currently stored?
Right now, your application uses **Entity Framework Core**. This is an ORM (Object-Relational Mapper) that automatically takes your C# models (`Orders`, `Reservations`, `Users`, `Admins`, etc.) and turns them into *perfectly structured relational database tables*. 

**Good News:** Because you are using Entity Framework Core, your functionality is strictly tied to your C# code (like the logic you mentioned where deleting day history moves it to month history). This means **changing the database to MySQL will NOT break any of your functionality**. The C# code remains exactly the same, and Entity Framework simply translates the code into MySQL language instead of SQL Server language.

## 2. Clarification: SSMS vs. MySQL
You mentioned **SSMS (SQL Server Management Studio)** but also mentioned learning **MySQL** at university. 

* **SSMS** is exclusively for **Microsoft SQL Server**. Your project currently uses a local, lightweight version of SQL Server (`(localdb)\mssqllocaldb`). Therefore, you *can* open SSMS right now, connect it to `(localdb)\mssqllocaldb`, and you will see all your tables in a clean, tabular format. It is incredibly easy to use.
* **MySQL** is a different database technology entirely. You cannot connect SSMS to MySQL. For MySQL, you would use a tool like **MySQL Workbench** or **phpMyAdmin** (usually bundled with XAMPP). They provide the exact same easy-to-use tabular interface as SSMS, allowing you to write queries, view relationships, and delete/update visually.

Since this is for a university DBMS project learning MySQL, below are the instructions to correctly convert your project to use MySQL. 

---

## 3. How to Convert Your Local App to a MySQL Database

If you want to migrate everything to **MySQL** so you can use MySQL Workbench or phpMyAdmin, follow these steps:

### Step A: Install the MySQL Package
Right now your project uses SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`). You need the MySQL equivalent.
1. Open up your terminal/Command Prompt in your project directory.
2. Run this command:
   ```bash
   dotnet add package Pomelo.EntityFrameworkCore.MySql
   ```

### Step B: Update your `appsettings.json`
Change your connection string from the LocalDB one to your new MySQL connection string.

**Change from this:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FoodHeavenDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
}
```

**To this (Assuming you use XAMPP or local MySQL with standard credentials):**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=FoodHeavenDb;User=root;Password=;"
}
```
*(Make sure to put your actual MySQL password if your `root` user has one).*

### Step C: Update `Program.cs`
Tell the application to use MySQL instead of SQL Server.

**Find this block (around line 53 in `Program.cs`):**
```csharp
else
{
    // Local / Default SQL Server
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}
```

**Change it to this:**
```csharp
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 31)); // Use your MySQL version here

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(connectionString, serverVersion));
}
```

### Step D: Re-Create the Database Migrations
Since you switched database engines, your old SQL Server migrations won't work perfectly on MySQL.

1. Delete the `Migrations` folder in your project directory. (This won't delete your C# code, just the historic database generation scripts).
2. Open the terminal and create a new migration for MySQL:
   ```bash
   dotnet ef migrations add InitialMySQLMigration
   ```
3. Create the database in MySQL:
   ```bash
   dotnet ef database update
   ```

*(Note: If the `dotnet ef` command isn't recognized, run `dotnet tool install --global dotnet-ef` first).*

---

## Conclusion

Once Step D is done, open up **MySQL Workbench** or **phpMyAdmin**. You will see a new database named `FoodHeavenDb`. Inside, you will see separate tables for `Admins`, `MenuItems`, `Orders`, `Reservations`, and more. The relationships will all be there, and you can write custom SQL queries simply right in the editor! All functionality (like tracking day history) continues to work.

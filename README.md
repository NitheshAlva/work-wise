# WorkWise - Task Management & Productivity Tracker

A modern, user-centric task management application built with ASP.NET Core MVC that helps users organize their work through personalized categories and efficient task tracking.

## 🚀 Features

### Core Functionality
- **User Authentication** - Secure registration, login, and profile management
- **Custom Categories** - Create personalized categories with color coding
- **Task Management** - Full CRUD operations with priority levels and due dates
- **Smart Dashboard** - Category-grouped tasks with overdue tracking
- **Responsive Design** - Clean, professional UI that works on all devices

### Key Highlights
- **User-Specific Categories**: Each user creates their own organization system
- **Flexible Task Organization**: Tasks can be categorized or left uncategorized
- **Visual Organization**: Color-coded categories for quick identification
- **Priority Management**: High, Medium, Low priority levels
- **Due Date Tracking**: Never miss important deadlines

## 🛠️ Technology Stack

- **Backend**: ASP.NET Core MVC
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **Frontend**: Bootstrap 5, HTML5, CSS3
- **Containerization**: Docker & Docker Compose

## 📋 Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Git](https://git-scm.com/)

## 🚀 Quick Start

### 1. Clone the Repository
```bash
git clone https://github.com/yourusername/workwise.git
cd workwise
```

### 2. Start PostgreSQL Database
```bash
# Start PostgreSQL container
docker-compose up -d

# Verify database is running
docker ps
```

### 3. Setup Database
```bash
# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Create and run migrations
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4. Run the Application
```bash
# Restore dependencies
dotnet restore

# Run the application
dotnet run

# Application will be available at:
# https://localhost:5001 or http://localhost:5000
```

## 🐳 Docker Configuration

The application uses PostgreSQL running in a Docker container. The database configuration is managed through the provided `docker-compose.yml` file.

### Database Connection
- **Host**: localhost
- **Database**: devdb
- **Username**: postgres
- **Password**: postgres
- **Port**: 5432

### Docker Commands
```bash
# Start database
docker-compose up -d

# Stop database
docker-compose down

# View logs
docker-compose logs postgres

# Reset database (removes all data)
docker-compose down -v
docker-compose up -d
```

## 📊 Database Schema

### Core Tables
```sql
Users
├── Id (Primary Key)
├── Email
├── Password (Hashed)
├── Name
└── CreatedDate

Categories
├── Id (Primary Key)
├── UserId (Foreign Key → Users.Id)
├── Name
├── Color
└── CreatedDate

Tasks
├── Id (Primary Key)
├── UserId (Foreign Key → Users.Id)
├── Title
├── Description
├── DueDate
├── Priority (High/Medium/Low)
├── CategoryId (Foreign Key → Categories.Id, Nullable)
├── IsCompleted
└── CreatedDate
```

## 🎯 Usage Examples

### Creating Categories
Users can create personalized categories such as:
- 🔵 **Interview Prep** - Job interview preparation tasks
- 🟢 **Side Projects** - Personal coding projects
- 🟡 **Learning** - Skill development and courses
- 🔴 **Urgent** - High-priority tasks
- 🟣 **Health & Fitness** - Personal wellness goals

### Task Organization
- Assign tasks to specific categories or leave them uncategorized
- Set priority levels and due dates
- Track completion status
- View tasks grouped by categories on the dashboard

## 🔧 Development Setup

### Environment Configuration
Create `appsettings.Development.json` (if not exists):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=devdb;Username=postgres;Password=postgres;Port=5432"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Common Development Commands
```bash
# Create new migration
dotnet ef migrations add [MigrationName]

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove

# Watch for file changes (hot reload)
dotnet watch run
```

## 🏗️ Project Structure
```
WorkWise/
├── Controllers/         # MVC Controllers
├── Models/             # Data models and ViewModels
├── Views/              # Razor views
├── Data/               # DbContext and configurations
├── Areas/Identity/     # Identity pages
├── wwwroot/           # Static files (CSS, JS, images)
├── Migrations/        # EF Core migrations
└── Program.cs         # Application entry point
```



**WorkWise** - Organize your tasks, boost your productivity! 🚀
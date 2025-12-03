# ===============================================
# VIEW GENERATOR SCRIPT
# ===============================================
# This script helps create all remaining views
# Run this in PowerShell or use the manual steps below

Write-Host "==================================" -ForegroundColor Green
Write-Host "AttendanceEase - View Generator" -ForegroundColor Green
Write-Host "==================================" -ForegroundColor Green
Write-Host ""

Write-Host "OPTION 1: Visual Studio Scaffolding (RECOMMENDED)" -ForegroundColor Cyan
Write-Host "------------------------------------------------" -ForegroundColor Cyan
Write-Host "1. Right-click on Controllers folder"
Write-Host "2. Select Add > New Scaffolded Item"
Write-Host "3. Choose 'MVC Controller with views, using Entity Framework'"
Write-Host "4. Select Model class and Data context"
Write-Host "5. Click Add"
Write-Host ""

Write-Host "OPTION 2: Package Manager Console Commands" -ForegroundColor Cyan
Write-Host "------------------------------------------------" -ForegroundColor Cyan
Write-Host ""

$controllers = @(
    @{Name="Sections"; Model="Section"},
    @{Name="Courses"; Model="Course"},
    @{Name="Teachers"; Model="Teacher"},
    @{Name="TimeTables"; Model="TimeTable"}
)

Write-Host "Copy and paste these commands into Package Manager Console:" -ForegroundColor Yellow
Write-Host ""

foreach ($controller in $controllers) {
    $cmd = "Scaffold-DbContext ""Name=ConnectionStrings:DefaultConnection"" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -Context ApplicationDbContext -Force"
    Write-Host "# For $($controller.Name) Controller:" -ForegroundColor Green
    Write-Host "dotnet aspnet-codegenerator controller -name $($controller.Name)Controller -m $($controller.Model) -dc ApplicationDbContext --relativeFolderPath Controllers --useDefaultLayout --referenceScriptLibraries -udl" -ForegroundColor White
    Write-Host ""
}

Write-Host ""
Write-Host "OPTION 3: Views Created So Far" -ForegroundColor Cyan
Write-Host "------------------------------------------------" -ForegroundColor Cyan
Write-Host "[?] Batches - All 5 views (Index, Create, Edit, Delete, Details)" -ForegroundColor Green
Write-Host "[?] Sections - 2 views (Index, Create)" -ForegroundColor Green
Write-Host "[?] Dashboard - All 3 views (Admin, Teacher, Student)" -ForegroundColor Green
Write-Host ""
Write-Host "Still Need:" -ForegroundColor Yellow
Write-Host "[ ] Sections - Edit, Delete, Details" -ForegroundColor Yellow
Write-Host "[ ] Courses - All 5 views" -ForegroundColor Yellow
Write-Host "[ ] Teachers - All 5 views" -ForegroundColor Yellow
Write-Host "[ ] Students - Update existing 5 views" -ForegroundColor Yellow
Write-Host "[ ] TimeTables - All 5 views" -ForegroundColor Yellow
Write-Host "[ ] Attendance - Mark, MarkByCourse, ViewStudent, Index" -ForegroundColor Yellow
Write-Host "[ ] Reports - Index, CourseAttendance, StudentAttendance" -ForegroundColor Yellow
Write-Host "[ ] Settings - Index, ChangePassword" -ForegroundColor Yellow
Write-Host ""

Write-Host "NEXT STEPS:" -ForegroundColor Magenta
Write-Host "------------------------------------------------" -ForegroundColor Magenta
Write-Host "1. I'll create the remaining critical views for you" -ForegroundColor White
Write-Host "2. Run the migration: Add-Migration AddCompleteAttendanceSystem" -ForegroundColor White
Write-Host "3. Update database: Update-Database" -ForegroundColor White
Write-Host "4. Run the application: dotnet run" -ForegroundColor White
Write-Host ""

Write-Host "Press Enter to see the simple step-by-step guide..." -ForegroundColor Yellow
Read-Host

Write-Host ""
Write-Host "==================================" -ForegroundColor Green
Write-Host "SIMPLE STEP-BY-STEP GUIDE" -ForegroundColor Green
Write-Host "==================================" -ForegroundColor Green
Write-Host ""
Write-Host "Step 1: Create Database" -ForegroundColor Cyan
Write-Host "Open Package Manager Console (Tools > NuGet Package Manager > Package Manager Console)" -ForegroundColor White
Write-Host "Run: Add-Migration AddCompleteAttendanceSystem" -ForegroundColor Yellow
Write-Host "Run: Update-Database" -ForegroundColor Yellow
Write-Host ""

Write-Host "Step 2: Run Application" -ForegroundColor Cyan
Write-Host "Press F5 or run: dotnet run" -ForegroundColor Yellow
Write-Host ""

Write-Host "Step 3: Login" -ForegroundColor Cyan
Write-Host "Email: admin@attendanceease.com" -ForegroundColor Yellow
Write-Host "Password: Admin@123" -ForegroundColor Yellow
Write-Host ""

Write-Host "Step 4: Create Test Data" -ForegroundColor Cyan
Write-Host "- Create a Batch (e.g., 2024)" -ForegroundColor White
Write-Host "- Create Sections in that batch" -ForegroundColor White
Write-Host "- Create Courses" -ForegroundColor White
Write-Host "- Create Teachers" -ForegroundColor White
Write-Host "- Create Students" -ForegroundColor White
Write-Host "- Create Timetables" -ForegroundColor White
Write-Host "- Mark Attendance" -ForegroundColor White
Write-Host ""

Write-Host "==================================" -ForegroundColor Green
Write-Host "Need more help? Check these files:" -ForegroundColor Green
Write-Host "- IMPLEMENTATION_GUIDE.md" -ForegroundColor White
Write-Host "- VIEW_CREATION_GUIDE.md" -ForegroundColor White
Write-Host "- QUICK_REFERENCE.md" -ForegroundColor White
Write-Host "==================================" -ForegroundColor Green

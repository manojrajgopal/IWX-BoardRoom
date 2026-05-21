using IWX.Contracts.Departments;
using IWX.Departments.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxDepartmentService(DepartmentRegistry.PlatformIntelligence);

var app = builder.Build();

app.UseIwxDepartmentService(DepartmentRegistry.PlatformIntelligence);

app.Run();

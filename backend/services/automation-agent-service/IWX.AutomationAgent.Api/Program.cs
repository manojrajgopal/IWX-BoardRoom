using IWX.Contracts.Departments;
using IWX.Departments.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxDepartmentService(DepartmentRegistry.Automation);

var app = builder.Build();

app.UseIwxDepartmentService(DepartmentRegistry.Automation);

app.Run();

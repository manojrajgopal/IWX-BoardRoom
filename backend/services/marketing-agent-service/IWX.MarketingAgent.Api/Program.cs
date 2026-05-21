using IWX.Contracts.Departments;
using IWX.Departments.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxDepartmentService(DepartmentRegistry.Marketing);

var app = builder.Build();

app.UseIwxDepartmentService(DepartmentRegistry.Marketing);

app.Run();

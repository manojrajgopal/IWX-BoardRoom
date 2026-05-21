using IWX.Contracts.Departments;
using IWX.Departments.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxDepartmentService(DepartmentRegistry.Development);

var app = builder.Build();

app.UseIwxDepartmentService(DepartmentRegistry.Development);

app.Run();

using IWX.Contracts.Departments;
using IWX.Departments.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxDepartmentService(DepartmentRegistry.Legal);

var app = builder.Build();

app.UseIwxDepartmentService(DepartmentRegistry.Legal);

app.Run();

using IWX.Contracts.Departments;
using IWX.Departments.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxDepartmentService(DepartmentRegistry.CustomerSupport);

var app = builder.Build();

app.UseIwxDepartmentService(DepartmentRegistry.CustomerSupport);

app.Run();

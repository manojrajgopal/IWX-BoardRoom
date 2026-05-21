using IWX.Contracts.Departments;
using IWX.Departments.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxDepartmentService(DepartmentRegistry.Research);

var app = builder.Build();

app.UseIwxDepartmentService(DepartmentRegistry.Research);

app.Run();

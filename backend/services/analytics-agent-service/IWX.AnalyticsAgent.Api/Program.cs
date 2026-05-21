using IWX.Contracts.Departments;
using IWX.Departments.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxDepartmentService(DepartmentRegistry.Analytics);

var app = builder.Build();

app.UseIwxDepartmentService(DepartmentRegistry.Analytics);

app.Run();

using IWX.Contracts.Departments;
using IWX.Departments.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxDepartmentService(DepartmentRegistry.Sales);

var app = builder.Build();

app.UseIwxDepartmentService(DepartmentRegistry.Sales);

app.Run();

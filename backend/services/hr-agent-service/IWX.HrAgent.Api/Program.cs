using IWX.Contracts.Departments;
using IWX.Departments.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxDepartmentService(DepartmentRegistry.Hr);

var app = builder.Build();

app.UseIwxDepartmentService(DepartmentRegistry.Hr);

app.Run();

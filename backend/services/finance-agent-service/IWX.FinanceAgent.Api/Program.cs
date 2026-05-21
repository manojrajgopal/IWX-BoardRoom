using IWX.Contracts.Departments;
using IWX.Departments.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxDepartmentService(DepartmentRegistry.Finance);

var app = builder.Build();

app.UseIwxDepartmentService(DepartmentRegistry.Finance);

app.Run();

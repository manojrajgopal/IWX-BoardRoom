using IWX.Contracts.Departments;
using IWX.Departments.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.AddIwxDepartmentService(DepartmentRegistry.SocialMedia);

var app = builder.Build();

app.UseIwxDepartmentService(DepartmentRegistry.SocialMedia);

app.Run();

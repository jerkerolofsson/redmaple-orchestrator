using MudBlazor.Services;
using MudExtensions.Services;
using RedMaple.Orchestrator.Controller.Components;
using RedMaple.Orchestrator.Controller.Controllers;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddContainerServices()
    .AddCertificateAuthority()
    .AddControllerDomainServices()
    .AddControllerInfrastructure();

builder.Services.AddControllers(options =>
        options.InputFormatters.Add(new ByteArrayInputFormatter()));
builder.Services.AddMudServices().AddMudExtensions();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler("/Error", createScopeForErrors: true);
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

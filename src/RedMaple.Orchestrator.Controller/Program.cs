using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

using MudBlazor.Services;
using MudExtensions.Services;
using RedMaple.Orchestrator.Controller.Components;
using RedMaple.Orchestrator.Controller.Controllers;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var oidcAuthority = Environment.GetEnvironmentVariable("OIDC_AUTHORITY") ?? "https://keycloak.lan/realms/master";
oidcAuthority = null;
var clientId = "redmaple";
var clientSecret = "55QcC8RiWLfB5M8A808v0P37Jje4uMZH";
if(!string.IsNullOrEmpty(oidcAuthority))
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
    })
    .AddOpenIdConnect(options =>
    {
        options.Authority = oidcAuthority;
        //options.MetadataAddress = "https://MyServer/realms/ATG/.well-known/openid-configuration";
        options.ClientId = clientId;
        options.ClientSecret = clientSecret;
        options.ResponseType = "code";
        options.SaveTokens = true;

        options.Scope.Add("openid");
        options.CallbackPath = "/signin-oidc"; // Update callback path
        options.SignedOutCallbackPath = "/signout-callback-oidc"; // Update signout callback path
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = Environment.GetEnvironmentVariable("OIDC_CLAIM_NAME") ?? "preferred_username",
            RoleClaimType = Environment.GetEnvironmentVariable("OIDC_CLAIM_ROLES") ?? "roles"
        };
    });
}

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
app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

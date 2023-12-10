// create web-app
using TrafficControlService.Actors;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ISpeedingViolationCalculator>(
    new DefaultSpeedingViolationCalculator("A12", 10, 100, 5));

builder.Services.AddHttpClient();
builder.Services.AddDaprClient(builder => builder
    .UseHttpEndpoint($"http://localhost:3600")
    .UseGrpcEndpoint($"http://localhost:60000"));

//Register Actors
builder.Services.AddActors(options => {
    options.Actors.RegisterActor<VehicleActor>();
});

builder.Services.AddSingleton<IVehicleStateRepository, DaprVehicleStateRepository>();
builder.Services.AddControllers();

var app = builder.Build();

// configure web-app
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// configure routing
app.MapControllers();

//Add actors endpoints.
app.MapActorsHandlers();

// let's go!
app.Run("http://localhost:6000");

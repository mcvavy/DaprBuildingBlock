﻿namespace FineCollectionService.Controllers;

[ApiController]
[Route("")]
public class CollectionController : ControllerBase
{
    private static string? _fineCalculatorLicenseKey = null;
    private readonly ILogger<CollectionController> _logger;
    private readonly IFineCalculator _fineCalculator;
    private readonly VehicleRegistrationService _vehicleRegistrationService;
    private readonly DaprClient _daprClient;

    public CollectionController(IConfiguration config, ILogger<CollectionController> logger,
        IFineCalculator fineCalculator, VehicleRegistrationService vehicleRegistrationService, DaprClient daprClient)
    {
        _logger = logger;
        _fineCalculator = fineCalculator;
        _vehicleRegistrationService = vehicleRegistrationService;
        _daprClient = daprClient;

        // set finecalculator component license-key
        if (_fineCalculatorLicenseKey == null)
        {
            var secrets = _daprClient.GetSecretAsync("trafficcontrol-secrets", "finecalculator.licensekey").Result;
            _fineCalculatorLicenseKey = secrets["finecalculator.licensekey"];
        }
    }

    [Topic("pubsub", "speedingviolations")]
    [Route("collectfine")]
    [HttpPost()]
    public async Task<ActionResult> CollectFine(SpeedingViolation speedingViolation, [FromServices] DaprClient daprClient)
    {
        decimal fine = _fineCalculator.CalculateFine(_fineCalculatorLicenseKey!, speedingViolation.ViolationInKmh);

        // get owner info
        var vehicleInfo = await _vehicleRegistrationService.GetVehicleInfo(speedingViolation.VehicleId);

        // log fine
        string fineString = fine == 0 ? "tbd by the prosecutor" : $"{fine} Euro";
        _logger.LogInformation($"Sent speeding ticket to {vehicleInfo.OwnerName}. " +
            $"Road: {speedingViolation.RoadId}, Licensenumber: {speedingViolation.VehicleId}, " +
            $"Vehicle: {vehicleInfo.Brand} {vehicleInfo.Model}, " +
            $"Violation: {speedingViolation.ViolationInKmh} Km/h, Fine: {fineString}, " +
            $"On: {speedingViolation.Timestamp.ToString("dd-MM-yyyy")} " +
            $"at {speedingViolation.Timestamp.ToString("hh:mm:ss")}.");

        // send fine by email
        // TODO
        var body = EmailUtils.CreateEmailBody(speedingViolation, vehicleInfo, fineString);

        var metadata = new Dictionary<string, string>
        {
            ["emailFrom"] = "noreply@cfca.gov",
            ["emailTo"] = vehicleInfo.OwnerEmail,
            ["subject"] = $"Speeding violation on the {speedingViolation.RoadId}"
        };

        await daprClient.InvokeBindingAsync("sendmail", "create", body, metadata);

        return Ok();
    }
}
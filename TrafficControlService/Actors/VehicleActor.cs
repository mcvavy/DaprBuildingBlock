using Dapr.Actors.Runtime;

namespace TrafficControlService.Actors
{
    public class VehicleActor : Actor, IVehicleActor
    {
        private readonly ISpeedingViolationCalculator _speedingViolationCalculator;
        private readonly DaprClient _daprClient;
        public VehicleActor(ActorHost host, ISpeedingViolationCalculator speedingViolationCalculator, DaprClient daprClient) : base(host)
        {
            _speedingViolationCalculator = speedingViolationCalculator;
            _daprClient = daprClient;
        }

        public async Task RegisterEntryAsync(VehicleRegistered msg)
        {
            Logger.LogInformation($"ENTRY detected in lane {msg.Lane} at " +
            $"{msg.Timestamp.ToString("hh:mm:ss")} " +
            $"of vehicle with license-number {msg.LicenseNumber}");

            var vehicleState = new VehicleState
            {
                LicenseNumber = msg.LicenseNumber,
                EntryTimestamp = msg.Timestamp
            };

            await StateManager.SetStateAsync("VehicleState", vehicleState);
        }

        public async Task RegisterExitAsync(VehicleRegistered msg)
        {
            Logger.LogInformation($"EXIT detected in lane {msg.Lane} at " + 
            $"{msg.Timestamp.ToString("hh:mm:ss")} " + 
            $"of vehicle with license-number {msg.LicenseNumber}");

            var vehicleState = await StateManager.GetStateAsync<VehicleState>("VehicleState");

            if(vehicleState == null){
                throw new InvalidOperationException($"State not found for vehicle {msg.LicenseNumber}.");
            }

            vehicleState.ExitTimestamp = msg.Timestamp;

            // handle possible speeding violation
            int violation = _speedingViolationCalculator.DetermineSpeedingViolationInKmh(
                vehicleState.EntryTimestamp, vehicleState.ExitTimestamp.Value);
                
            if (violation > 0)
            {
                Logger.LogInformation($"Speeding violation detected ({violation} KMh) of vehicle" +
                    $"with license-number {vehicleState.LicenseNumber}.");

                var speedingViolation = new SpeedingViolation
                {
                    VehicleId = msg.LicenseNumber,
                    RoadId = _speedingViolationCalculator.GetRoadId(),
                    ViolationInKmh = violation,
                    Timestamp = msg.Timestamp
                };

                // publish speedingviolation
                await _daprClient.PublishEventAsync("pubsub", "speedingviolations", speedingViolation);
            }
        }
    }
}
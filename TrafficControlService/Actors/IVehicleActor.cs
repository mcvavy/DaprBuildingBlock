using Dapr.Actors;

namespace TrafficControlService.Actors
{
    public interface IVehicleActor : IActor
    {
        Task RegisterEntryAsync(VehicleRegistered msg);
        Task RegisterExitAsync(VehicleRegistered msg);
    }
}


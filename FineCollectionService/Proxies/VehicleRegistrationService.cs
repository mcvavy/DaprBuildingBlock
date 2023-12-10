namespace FineCollectionService.Proxies;

public class VehicleRegistrationService
{
    private HttpClient _httpClient;

    public VehicleRegistrationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<VehicleInfo> GetVehicleInfo(string licenseNumber)
    {
        return await _httpClient.GetFromJsonAsync<VehicleInfo>(
            $"/vehicleinfo/{licenseNumber}");
             // $"http://localhost:3601/v1.0/invoke/vehicleregistrationservice/method/vehicleinfo/{licenseNumber}");
    }
}

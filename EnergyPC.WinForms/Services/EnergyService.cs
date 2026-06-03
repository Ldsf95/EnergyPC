using EnergyPC.WinForms.Models;

namespace EnergyPC.WinForms.Services;

/// <summary>Service métier côté client : façade lisible au-dessus de l'API.</summary>
public class EnergyService
{
    private readonly ApiClient _api;
    public EnergyService(ApiClient api) => _api = api;

    public Task<List<ReadingModel>?> GetLatestAsync() =>
        _api.GetAsync<List<ReadingModel>>("api/readings/latest");

    public Task<List<PointModel>?> GetHistoryAsync(string code, int minutes = 5) =>
        _api.GetAsync<List<PointModel>>(
            $"api/readings/history?code={code}&minutes={minutes}");

    public Task<EnergieModel?> GetEnergieAsync(string code, int minutes = 60) =>
        _api.GetAsync<EnergieModel>(
            $"api/readings/energie?code={code}&minutes={minutes}");
}

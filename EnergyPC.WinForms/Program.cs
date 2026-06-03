using EnergyPC.WinForms.Forms;
using EnergyPC.WinForms.Services;
using Microsoft.Extensions.Configuration;

namespace EnergyPC.WinForms;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Configuration externalisée (URL de l'API, seuil d'alerte, identifiants).
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var baseUrl = config["Api:BaseUrl"] ?? "https://localhost:5001";
        var seuil = double.TryParse(config["Alerte:SeuilSystemPowerW"], out var s) ? s : 60.0;

        var api = new ApiClient(baseUrl);

        // Authentification JWT optionnelle : si les identifiants sont fournis,
        // on récupère un token et on le joint aux requêtes suivantes.
        TryLogin(api, config);

        var service = new EnergyService(api);

        Application.Run(new FormDashboard(service, seuil));
    }

    private static void TryLogin(ApiClient api, IConfiguration config)
    {
        var user = config["Auth:Utilisateur"];
        var pass = config["Auth:MotDePasse"];
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass)) return;

        try
        {
            var rep = api.PostAsync<object, LoginResponse>(
                "api/auth/login",
                new { utilisateur = user, motDePasse = pass }).GetAwaiter().GetResult();

            if (rep is not null && !string.IsNullOrEmpty(rep.Token))
                api.SetToken(rep.Token);
        }
        catch
        {
            // Les endpoints de données restent accessibles sans token :
            // on continue en mode anonyme si la connexion échoue.
        }
    }

    private record LoginResponse(string Token, DateTime Expiration);
}

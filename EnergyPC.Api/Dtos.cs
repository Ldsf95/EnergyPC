namespace EnergyPC.Api;

/// <summary>Type de capteur exposé par l'API.</summary>
public record SensorTypeDto(int Id, string Code, string Libelle,
    string Unite, string Famille);

/// <summary>Dernière valeur connue d'un capteur (snapshot).</summary>
public record ReadingDto(string Code, string Libelle, string Unite,
    string Famille, double? Valeur, DateTime? Horodatage);

/// <summary>Point d'une série temporelle.</summary>
public record PointDto(DateTime Horodatage, double Valeur);

/// <summary>Énergie cumulée (Wh) calculée sur une fenêtre.</summary>
public record EnergieDto(string Code, int FenetreMinutes, double Wh);

/// <summary>Requête de connexion (obtention d'un token JWT).</summary>
public record LoginRequest(string Utilisateur, string MotDePasse);

/// <summary>Réponse de connexion contenant le token signé.</summary>
public record LoginResponse(string Token, DateTime Expiration);

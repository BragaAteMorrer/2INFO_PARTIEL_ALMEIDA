using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace AlmeidaPartielQualiteAir
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private const string apiKey = "Hxx3krT5tohuvdC3sIJEtoQxPaOwwetzfzgwjfnX"; // Clé API pour les villes et la qualité de l'air
        private const string cityApiUrl = "https://api.api-ninjas.com/v1/city";
        private const string airQualityApiUrl = "https://api.api-ninjas.com/v1/airquality";

        static async Task Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("Bienvenue dans le système de vérification de la qualité de l'air:");
                Console.WriteLine("1. Taper une ville pour obtenir la qualité de l'air (Bonus) (IL FAUT TAPER LES VILLES EN ANGLAIS)");
                Console.WriteLine("2. Taper un pays pour obtenir les 15 plus grandes villes et la qualité de l'air de chacune (IL FAUT TAPER LES PAYS EN ANGLAIS)");
                Console.WriteLine("3. Quitter");
                Console.Write("Quel est votre choix ? : ");

                string choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        Console.Write("Entrez le nom de la ville: ");
                        string city = Console.ReadLine();
                        await GetAirQualityForCityAsync(city);
                        break;
                    case "2":
                        Console.Write("Entrez le nom du pays: ");
                        string country = Console.ReadLine();
                        await GetTopCitiesByCountryAsync(country);
                        break;
                    case "3":
                        return;
                    default:
                        Console.WriteLine("Choix invalide, veuillez réessayer.");
                        break;
                }
            }
        }

        static async Task GetAirQualityForCityAsync(string city)
        {
            if (string.IsNullOrEmpty(city))
            {
                Console.WriteLine("Le nom de la ville ne peut pas être vide.");
                return;
            }

            try
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
                Console.WriteLine($"Envoi de la requête à l'API pour la qualité de l'air de {city}...");
                var response = await client.GetAsync($"{airQualityApiUrl}?city={city}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Erreur de l'API : {response.StatusCode}");
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                    return;
                }

                var responseData = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Réponse brute de l'API pour {city}: {responseData}");

                var airQualityResponse = JsonSerializer.Deserialize<JsonElement>(responseData);
                if (airQualityResponse.TryGetProperty("overall_aqi", out JsonElement overallAqiElement))
                {
                    Console.WriteLine($"Qualité de l'air à {city}: AQI {overallAqiElement.GetInt32()}");
                }
                else
                {
                    Console.WriteLine($"Impossible d'obtenir la qualité de l'air pour {city}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération de la qualité de l'air : {ex.Message}");
            }
        }

        static async Task GetTopCitiesByCountryAsync(string country)
        {
            if (string.IsNullOrEmpty(country))
            {
                Console.WriteLine("Le nom du pays ne peut pas être vide.");
                return;
            }

            string? countryCode = GetCountryCode(country);
            if (countryCode == null)
            {
                Console.WriteLine("Code ISO-3166 non trouvé pour le pays spécifié.");
                return;
            }

            try
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
                Console.WriteLine("Envoi de la requête à l'API pour les villes...");
                var response = await client.GetAsync($"{cityApiUrl}?country={countryCode}&limit=15");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Erreur de l'API : {response.StatusCode}");
                    return;
                }

                var cities = await response.Content.ReadFromJsonAsync<List<City>>();
                if (cities == null || cities.Count == 0)
                {
                    Console.WriteLine("Impossible d'obtenir la liste des villes.");
                    return;
                }

                Console.WriteLine("Liste des villes obtenue avec succès.");

                var cityAirQuality = new List<CityAirQuality>();

                foreach (var city in cities)
                {
                    if (string.IsNullOrEmpty(city.Name))
                    {
                        Console.WriteLine("Le nom de la ville est null ou vide.");
                        continue;
                    }

                    var airQuality = await GetAirQualityAsync(city.Name!, country);
                    if (airQuality != null)
                    {
                        cityAirQuality.Add(new CityAirQuality
                        {
                            CityName = city.Name!,
                            AirQualityIndex = airQuality.OverallAqi,
                            Population = city.Population
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Impossible d'obtenir la qualité de l'air pour {city.Name}.");
                    }
                }

                if (cityAirQuality.Count == 0)
                {
                    Console.WriteLine("Aucune donnée de qualité de l'air disponible.");
                    return;
                }

                cityAirQuality.Sort((a, b) => b.Population.CompareTo(a.Population));

                Console.WriteLine($"Top 15 des villes du pays {country} classées par la population:");
                foreach (var city in cityAirQuality)
                {
                    Console.WriteLine($"{city.CityName}: Population {city.Population}, AQI {city.AirQualityIndex}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des villes : {ex.Message}");
            }
        }

        static string? GetCountryCode(string countryName)
        {
            try
            {
                var cultureInfoList = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
                foreach (var cultureInfo in cultureInfoList)
                {
                    var region = new RegionInfo(cultureInfo.Name);
                    if (region.EnglishName.ToLower().Contains(countryName.ToLower()))
                    {
                        return region.TwoLetterISORegionName;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la conversion du nom du pays : {ex.Message}");
            }
            return null;
        }

        static async Task<AirQuality?> GetAirQualityAsync(string cityName, string countryName)
        {
            try
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
                Console.WriteLine($"Envoi de la requête à l'API pour la qualité de l'air de {cityName}...");
                var response = await client.GetAsync($"{airQualityApiUrl}?city={cityName}&country={countryName}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Erreur de l'API : {response.StatusCode}");
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                    return null;
                }

                var responseData = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Réponse brute de l'API pour {cityName}: {responseData}");

                var airQualityResponse = JsonSerializer.Deserialize<JsonElement>(responseData);
                if (airQualityResponse.TryGetProperty("overall_aqi", out JsonElement overallAqiElement))
                {
                    return new AirQuality { OverallAqi = overallAqiElement.GetInt32() };
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération de la qualité de l'air : {ex.Message}");
                return null;
            }
        }
    }

    class City
    {
        public string? Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Population { get; set; } // Ajout de la propriété Population
    }

    class AirQuality
    {
        public int OverallAqi { get; set; }
    }

    class CityAirQuality
    {
        public string? CityName { get; set; }
        public int AirQualityIndex { get; set; }
        public int Population { get; set; } // Ajout de la propriété Population
    }
}

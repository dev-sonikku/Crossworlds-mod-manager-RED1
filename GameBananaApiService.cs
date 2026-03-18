using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CrossworldsModManager
{
    public class GameBananaApiResponse
    {
        [JsonPropertyName("_aRecords")]
        public List<GameBananaMod>? Records { get; set; }
    }

    public class GameBananaModProfile
    {
        [JsonPropertyName("_sText")]
        public string Description { get; set; } = ""; // The HTML content of the mod's description page

        [JsonPropertyName("_sName")]
        public string Name { get; set; } = "";

        [JsonPropertyName("_nLikeCount")]
        public int LikeCount { get; set; }

        [JsonPropertyName("_aSubmitter")]
        public GameBananaSubmitter? Submitter { get; set; }

        [JsonPropertyName("_aPreviewMedia")]
        public GameBananaMedia? Media { get; set; }
    }

    public class GameBananaDownloadPage
    {
        [JsonPropertyName("_aFiles")]
        public List<GameBananaFile>? Files { get; set; }
    }

    public class GameBananaFile
    {
        [JsonPropertyName("_idRow")]
        public int FileId { get; set; }

        [JsonPropertyName("_sFile")]
        public string FileName { get; set; } = "";

        [JsonPropertyName("_sDownloadUrl")]
        public string DownloadUrl { get; set; } = "";

        public override string ToString() => FileName; // For easy display in ListBox
    }

    public class GameBananaMod
    {
        [JsonPropertyName("_idRow")]
        public int Id { get; set; }

        [JsonPropertyName("_sModelName")]
        public string ModelName { get; set; } = "";

        [JsonPropertyName("_sName")]
        public string Name { get; set; } = "";

        [JsonPropertyName("_sVersion")]
        public string Version { get; set; } = "";

        [JsonPropertyName("_sProfileUrl")]
        public string ProfileUrl { get; set; } = "";

        [JsonPropertyName("_nLikeCount")]
        public int LikeCount { get; set; }

        [JsonPropertyName("_aSubmitter")]
        public GameBananaSubmitter? Submitter { get; set; }

        [JsonPropertyName("_aPreviewMedia")]
        public GameBananaMedia? Media { get; set; }

        public string Author => Submitter?.Name ?? "Unknown";
        public string? ThumbnailUrl => Media?.Images?.FirstOrDefault()?.BaseUrl + "/" + Media?.Images?.FirstOrDefault()?.File530;
        public string DownloadUrl => $"https://gamebanana.com/dl/{Id}";
    }

    public class GameBananaSubmitter
    {
        [JsonPropertyName("_sName")]
        public string Name { get; set; } = "";
    }

    public class GameBananaMedia
    {
        [JsonPropertyName("_aImages")]
        public List<GameBananaImage>? Images { get; set; }
    }

    public class GameBananaImage
    {
        [JsonPropertyName("_sBaseUrl")]
        public string BaseUrl { get; set; } = "";

        [JsonPropertyName("_sFile100")]
        public string File100 { get; set; } = ""; // 100x75 thumbnail

        [JsonPropertyName("_sFile220")]
        public string File220 { get; set; } = ""; // 220x165 thumbnail

        [JsonPropertyName("_sFile530")]
        public string File530 { get; set; } = ""; // 530px wide thumbnail
    }

    public static class GameBananaApiService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "https://gamebanana.com/apiv11";

        public static async Task<List<GameBananaMod>?> SearchModsAsync(int gameId, int page = 1, string search = "")
        {
            // Use the /Subfeed endpoint to get the latest submissions.
            // This endpoint supports pagination, sorting, and text-based searching.
            var url = $"{ApiBaseUrl}/Game/{gameId}/Subfeed?_nPage={page}&_sSort=new";

            if (!string.IsNullOrWhiteSpace(search))
            {
                // The API uses _sName for searching.
                url += $"&_sName={System.Net.WebUtility.UrlEncode(search)}";
            }

            try
            {
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Request to GameBanana API failed with status code {response.StatusCode}.\nURL: {url}\nResponse: {content}");
                }
                var apiResponse = JsonSerializer.Deserialize<GameBananaApiResponse>(content);

                // Filter out non-mod submissions like WIPs, questions, etc.
                var excludedTypes = new HashSet<string> { "Wip", "Question", "Request" };
                var filteredRecords = apiResponse?.Records?
                    .Where(mod => !excludedTypes.Contains(mod.ModelName, StringComparer.OrdinalIgnoreCase)).ToList();

                return filteredRecords;
            }
            catch (Exception ex)
            {
                // Re-throw to be caught by the UI layer, which will display the message.
                throw new Exception($"An error occurred while contacting the GameBanana API. Please check your internet connection. Details: {ex.Message}", ex);
            }
        }

        public static async Task<GameBananaModProfile?> GetModDetailsAsync(GameBananaMod mod)
        {
            var url = $"{ApiBaseUrl}/{mod.ModelName}/{mod.Id}/ProfilePage";

            try
            {
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Request to GameBanana API failed with status code {response.StatusCode}.\nURL: {url}\nResponse: {content}");
                }
                return JsonSerializer.Deserialize<GameBananaModProfile>(content);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while fetching mod details. Details: {ex.Message}", ex);
            }
        }

        public static async Task<GameBananaDownloadPage?> GetModDownloadPageAsync(GameBananaMod mod)
        {
            var url = $"{ApiBaseUrl}/{mod.ModelName}/{mod.Id}/DownloadPage";

            try
            {
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Request to GameBanana API failed with status code {response.StatusCode}.\nURL: {url}\nResponse: {content}");
                }
                return JsonSerializer.Deserialize<GameBananaDownloadPage>(content);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while fetching download page. Details: {ex.Message}", ex);
            }
        }

        public static async Task<GameBananaMod?> GetModFromProfilePageAsync(string modType, int modId)
        {
            var url = $"{ApiBaseUrl}/{modType}/{modId}/ProfilePage";

            try
            {
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Request to GameBanana API failed with status code {response.StatusCode}.\nURL: {url}\nResponse: {content}");
                }
                var modProfile = JsonSerializer.Deserialize<GameBananaModProfile>(content);

                if (modProfile == null) return null;

                // Construct a GameBananaMod object from the profile data
                return new GameBananaMod
                {
                    Id = modId,
                    ModelName = modType,
                    Name = modProfile.Name,
                    LikeCount = modProfile.LikeCount,
                    Submitter = modProfile.Submitter,
                    Media = modProfile.Media,
                    ProfileUrl = $"https://gamebanana.com/{modType.ToLower()}/{modId}"
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while fetching mod profile page. Details: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Fetches the latest version number for a mod from the GameBanana API's update count.
        /// </summary>
        /// <param name="itemType">The item type (e.g., "Mod").</param>
        /// <param name="itemId">The item ID.</param>
        /// <returns>The latest version number as a string, or null if it cannot be fetched.</returns>
        public static async Task<string?> GetLatestModUpdateCountAsync(string itemType, int itemId)
        {
            if (string.IsNullOrEmpty(itemType) || itemId <= 0)
            {
                return null;
            }

            string apiUrl = $"{ApiBaseUrl}/{itemType}/{itemId}/Updates?_nPage=1&_nPerpage=1";

            try
            {
                var response = await _httpClient.GetAsync(apiUrl);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Request to GameBanana API failed with status code {response.StatusCode}.\nURL: {apiUrl}\nResponse: {content}");
                }

                using var jsonDoc = JsonDocument.Parse(content);

                jsonDoc.RootElement.TryGetProperty("_aMetadata", out var cur_metadata);
                cur_metadata.TryGetProperty("_nRecordCount", out var recordCount);
                return recordCount.GetInt32().ToString() ?? null;
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while fetching mod version. Details: {ex.Message}", ex);
            }
        }

        public static async Task<string?> GetLatestModVersionAsync(string itemType, int itemId)
        {
            if (string.IsNullOrEmpty(itemType) || itemId <= 0)
            {
                return null;
            }

            string apiUrl = $"{ApiBaseUrl}/{itemType}/{itemId}/Updates?_nPage=1&_nPerpage=1";

            try
            {
                var response = await _httpClient.GetAsync(apiUrl);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Request to GameBanana API failed with status code {response.StatusCode}.\nURL: {apiUrl}\nResponse: {content}");
                }

                using var jsonDoc = JsonDocument.Parse(content);

                jsonDoc.RootElement.TryGetProperty("_aRecords", out var cur_records);
                if (cur_records.GetArrayLength() > 0)
                {
                    cur_records[0].TryGetProperty("_sVersion", out var version); 
                    return version.GetString() ?? null;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while fetching mod version. Details: {ex.Message}", ex);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public static class Addon
{
    public static int Id { get; set; }
    public static string Slug { get; set; }
    public static string Author { get; set; }
    public static string Name { get; set; }
    public static string Url { get; set; }
    public static string Version { get; set; }
    public static string ChangelogUrl { get; set; }
    public static string TicketUrl { get; set; }
    public static string GitUrl { get; set; }
    public static List<string> Patch { get; set; }
    public static string LastUpdate { get; set; }
    public static string WebUrl { get; set; }
    public static string DonateUrl { get; set; }
    public static string SmallDesc { get; set; }
    public static string Desc { get; set; }
    public static string ScreenshotUrl { get; set; }
    public static List<string> GalleryUrl { get; set; }
    public static string LogoUrl { get; set; }
    public static string LogoSquareUrl { get; set; }
    public static List<string> Directories { get; set; }

    public static async Task LoadFromUrl(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string jsonResponse = await response.Content.ReadAsStringAsync();

            // Désérialiser le JSON en un dictionnaire temporaire pour éviter les problèmes avec les membres statiques
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var addon = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonResponse, options);

            // Assigner les valeurs aux propriétés statiques
            Id = addon["id"].GetInt32();
            Slug = addon["slug"].GetString();
            Author = addon["author"].GetString();
            Name = addon["name"].GetString();
            Url = addon["url"].GetString();
            Version = addon["version"].GetString();
            ChangelogUrl = addon["changelog_url"].GetString();
            TicketUrl = addon["ticket_url"].GetString();
            GitUrl = addon["git_url"].GetString();
            Patch = JsonSerializer.Deserialize<List<string>>(addon["patch"].ToString());
            LastUpdate = addon["last_update"].GetString();
            WebUrl = addon["web_url"].GetString();
            DonateUrl = addon["donate_url"].GetString();
            SmallDesc = addon["small_desc"].GetString();
            Desc = addon["desc"].GetString();
            ScreenshotUrl = addon["screenshot_url"].GetString();
            GalleryUrl = JsonSerializer.Deserialize<List<string>>(addon["gallery_url"].ToString());
            LogoUrl = addon["logo_url"].GetString();
            LogoSquareUrl = addon["logo_square_url"].GetString();
            Directories = JsonSerializer.Deserialize<List<string>>(addon["directories"].ToString());
        }
    }
}

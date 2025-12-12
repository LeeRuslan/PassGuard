using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PassGuard
{
    public static class DataService
    {
        public static void SaveData(List<PasswordEntry> entries, string filePath, string masterPassword)
        {
            try
            {
                // Encrypt passwords before saving
                var entriesCopy = new List<PasswordEntry>();
                foreach (var entry in entries)
                {
                    var entryCopy = new PasswordEntry
                    {
                        Id = entry.Id,
                        Title = entry.Title,
                        Username = entry.Username,
                        Category = entry.Category,
                        CreatedDate = entry.CreatedDate
                    };

                    // Encrypt the password
                    entryCopy.EncryptedPassword = CryptoService.Encrypt(entry.EncryptedPassword, masterPassword);
                    entriesCopy.Add(entryCopy);
                }

                // Serialize to JSON
                string json = JsonSerializer.Serialize(entriesCopy, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Encrypt the entire JSON
                string encryptedData = CryptoService.Encrypt(json, masterPassword);
                File.WriteAllText(filePath, encryptedData);
            }
            catch (Exception ex)
            {
                // Save backup without encryption
                try
                {
                    string json = JsonSerializer.Serialize(entries, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    File.WriteAllText(filePath + ".backup", json);
                }
                catch
                {
                    // Ignore backup error
                }

                throw new InvalidOperationException($"Save error: {ex.Message}");
            }
        }

        public static List<PasswordEntry> LoadData(string filePath, string masterPassword)
        {
            if (!File.Exists(filePath))
                return new List<PasswordEntry>();

            try
            {
                string encryptedData = File.ReadAllText(filePath);
                string json = CryptoService.Decrypt(encryptedData, masterPassword);

                var entries = JsonSerializer.Deserialize<List<PasswordEntry>>(json)
                    ?? new List<PasswordEntry>();

                // Decrypt passwords
                foreach (var entry in entries)
                {
                    entry.EncryptedPassword = CryptoService.Decrypt(entry.EncryptedPassword, masterPassword);
                }

                return entries;
            }
            catch (Exception ex)
            {
                // Try to load from backup
                if (File.Exists(filePath + ".backup"))
                {
                    try
                    {
                        string json = File.ReadAllText(filePath + ".backup");
                        return JsonSerializer.Deserialize<List<PasswordEntry>>(json)
                            ?? new List<PasswordEntry>();
                    }
                    catch
                    {
                        // Ignore backup error
                    }
                }

                throw new InvalidOperationException($"Load error: {ex.Message}");
            }
        }
    }
}
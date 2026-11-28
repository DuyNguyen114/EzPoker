using PokerTrainer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace PokerTrainer.Mobile.Services
{
    public sealed class HandHistoryStoreService
    {
        public async Task SaveAsync(HandHistoryRecord record)
        {
            string folder = Path.Combine(FileSystem.AppDataDirectory, "hand-history");
            Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, $"{record.HistoryId:N}.json");
            string tempPath = path + ".tmp";

            string json = JsonSerializer.Serialize(record);
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, path, overwrite: true);
        }
    }
}

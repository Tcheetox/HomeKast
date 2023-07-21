﻿using System.Text.Json.Serialization;

namespace Kast.Provider.Media
{
    public class Metadata
    {
        public string? Backdrop { get; init; }
        public string? MediaType { get; init; }
        public string? OriginalTitle { get; init; }
        public DateTime? Released { get; init; }
        public double? Vote { get; init; }
        public string? Description { get; init; }
        public string? ImageUrl { get; init; }

        [JsonIgnore]
        public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath) && File.Exists(ImagePath);
        public string? ImagePath { get; set; }
        [JsonIgnore]
        public bool HasThumbnail => !string.IsNullOrWhiteSpace(ThumbnailPath) && File.Exists(ThumbnailPath);
        public string? ThumbnailPath { get; set; }
    }
}

using Kast.Provider.Cast;
using Kast.Provider.Media;

namespace Kast.Api.Models
{
    public record class Caster
    {
        public Guid Id { get; private set; }
        public string? Name { get; private set; }
        public string? Title { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsOwner { get; private set; }
        public bool IsLaunched { get; private set; }
        public bool? IsMuted { get; private set; }
        public bool? IsIdle { get; private set; }
        public float? Volume { get; private set; }
        public string? Owner { get; private set; }
        public TimeSpan? Current { get; private set; }
        public TimeSpan? Duration { get; private set; }
        public Media? Media { get; private set; }

        public static Caster From(ReceiverContext<IMedia> receiver)
            => new()
            {
                Id = receiver.Id,
                Name = receiver.Name,
                IsConnected = receiver.IsConnected,
                IsOwner = receiver.IsOwner,
                IsLaunched = receiver.IsLaunched,
                IsMuted = receiver.IsMuted,
                Volume = receiver.Volume,
                Owner = receiver.Owner,
                Current = receiver.Current,
                Duration = receiver.Duration,
                Title = receiver.Title,
                Media = receiver.Media is not null ? Media.From(receiver.Media) : null
            };
    }
}

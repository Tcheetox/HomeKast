using Kast.Provider.Cast;
using Kast.Provider.Media;

namespace Kast.Api.Models
{
    public class Caster
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsOwner { get; private set; }
        public bool IsLaunched { get; private set; }
        public bool? IsMuted  { get; private set; }
        public bool? IsIdle  { get; private set; }
        public float? Volume { get; private set; }
        public string? Owner { get; private set; }
        public TimeSpan? Current { get; private set; }
        public Media? Media { get; private set; }

        private Caster(ReceiverContext<IMedia> context) 
        { 
            Id = context.Id;
            Name = context.Name;
            IsConnected = context.IsConnected;
            IsOwner = context.IsOwner;
            IsLaunched = context.IsLaunched;
            IsMuted = context.IsMuted;
            Volume = context.Volume;
            Owner = context.Owner;
            Current = context.Current;
            if (context.Media != null)
                Media = Media.From(context.Media);
        }

        public static Caster From(ReceiverContext<IMedia> receiver) 
            => new(receiver);
    }
}

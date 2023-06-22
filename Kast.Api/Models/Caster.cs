using Kast.Provider.Cast;
using Kast.Provider.Media;

namespace Kast.Api.Models
{
    public class Caster
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        private Caster(ReceiverContext<IMedia> context) 
        { 
            Id = context.Id;
            Name = context.Name;
        }

        public static Caster From(ReceiverContext<IMedia> receiver) 
            => new(receiver);
    }
}

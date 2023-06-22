using System.Runtime.Serialization;
using GoogleCast.Messages;
using GoogleCast.Models;

namespace Kast.Provider.Cast.Messages
{
    [DataContract]
    internal class VolumeMessage : MessageWithId
    {
        [DataMember(Name = "volume")]
        public Volume Volume { get; set; } = default!;
        public VolumeMessage() 
        {
            Type = "SET_VOLUME";
        }

        public VolumeMessage(bool mute) : this()
        {
            Volume = new Volume()
            {
                IsMuted = mute,
            };
        }

        public VolumeMessage(float level) : this()
        {
            Volume = new Volume()
            {
                Level = level,
            };
        }

    }
}

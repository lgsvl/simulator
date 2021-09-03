
namespace Docker.DotNet.Models
{
    using System.Runtime.Serialization;

    public enum RestartPolicyKind
    {
        [EnumMember(Value = "")]
        Undefined,

        [EnumMember(Value = "no")]
        No,

        [EnumMember(Value = "always")]
        Always,

        [EnumMember(Value = "on-failure")]
        OnFailure,

        [EnumMember(Value = "unless-stopped")]
        UnlessStopped
    }
}

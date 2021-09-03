using System.Runtime.Serialization;

namespace Docker.DotNet.Models
{
    public enum TaskState
    {
        [EnumMember(Value = "new")]
        New,

        [EnumMember(Value = "allocated")]
        Allocated,

        [EnumMember(Value = "pending")]
        Pending,

        [EnumMember(Value = "assigned")]
        Assigned,

        [EnumMember(Value = "accepted")]
        Accepted,

        [EnumMember(Value = "preparing")]
        Preparing,

        [EnumMember(Value = "ready")]
        Ready,

        [EnumMember(Value = "starting")]
        Starting,

        [EnumMember(Value = "running")]
        Running,

        [EnumMember(Value = "complete")]
        Complete,

        [EnumMember(Value = "shutdown")]
        Shutdown,

        [EnumMember(Value = "failed")]
        Failed,

        [EnumMember(Value = "rejected")]
        Rejected,

        [EnumMember(Value = "remove")]
        Remove,

        [EnumMember(Value = "orphaned")]
        Orphaned
    }
}

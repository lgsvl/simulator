
namespace Docker.DotNet.Models
{
    /// <summary>
    /// In go something like map[string]struct{} has no correct match so we create
    /// the empty type here that can be used for something like IDictionary<string, EmptyStruct>;
    /// </summary>
    public struct EmptyStruct
    {
    }
}
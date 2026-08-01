using System.Text.Json.Serialization;
using Marmot.Backend.Projects;

[JsonSerializable(typeof(Project))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class JsonContext : JsonSerializerContext;
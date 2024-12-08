
namespace PdbReader.Microsoft.CodeView
{
    internal interface INamedItem
    {
        public const string NoName = "<UNNAMED>";

        string Name { get; }
    }
}

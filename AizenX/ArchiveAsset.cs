using System;

namespace AizenX;

public sealed record ArchiveAsset(
    string Name,
    string Extension,
    string ArchivePath,
    string StoredPath,
    uint Hash,
    bool XmlConvertible)
{
    public string FileName => Name.EndsWith(Extension, StringComparison.OrdinalIgnoreCase)
        ? Name
        : Name + Extension;

    public string DisplayText
    {
        get
        {
            string capability = XmlConvertible ? "XML" : "NATIVE";
            return string.IsNullOrWhiteSpace(ArchivePath)
                ? FileName + "  |  " + capability
                : FileName + "  |  " + capability + "  |  " + ArchivePath;
        }
    }

    public override string ToString() => DisplayText;
}
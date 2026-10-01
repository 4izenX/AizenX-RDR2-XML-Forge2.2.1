namespace AizenX;

public sealed record FileInspection(string Path, long Size, string Extension, bool IsRsc8, string Header, string Details);
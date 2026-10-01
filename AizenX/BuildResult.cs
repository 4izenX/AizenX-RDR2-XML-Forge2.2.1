namespace AizenX;

public sealed record BuildResult(string Input, string? Output, string Kind, bool Success, bool Verified, string Message);
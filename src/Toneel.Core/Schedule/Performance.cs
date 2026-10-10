namespace Toneel.Core.Schedule;

/// <summary>One evening or afternoon on which the play is performed.</summary>
public sealed record Performance(DateOnly Date, string Location);

namespace KBot.Framework;

public sealed record ShipUpdateProgress(int Total, int Updated, int Unchanged, int Failed)
{
    public int Completed => Updated + Unchanged + Failed;
}

internal enum ShipUpdateOutcome { Updated, Unchanged, Failed }

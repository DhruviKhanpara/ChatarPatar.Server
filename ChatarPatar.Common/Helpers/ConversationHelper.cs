namespace ChatarPatar.Common.Helpers;

public static class ConversationHelper
{
    /// <summary>
    /// Always put the smaller GUID in position 1 so the unique index
    /// treats (A,B) and (B,A) as identical.
    /// </summary>
    public static (Guid userA, Guid userB) Normalize(Guid a, Guid b) =>
        a.CompareTo(b) <= 0 ? (a, b) : (b, a);
}

using Groot.Core.Sessions;

namespace Groot.Data.Tests;

/// <summary>
/// Compares two sessions the way a round-trip test needs to. A record's generated equality
/// compares <see cref="LoggedSession.Sets"/> by reference, so two sessions holding equal but
/// distinct set lists are unequal — which makes a plain <c>Assert.Equal</c> on a stored session
/// pass only when the list happens to be the empty singleton, and quietly assert nothing about
/// the sets on every other session.
/// </summary>
public static class SessionAssert
{
    public static void Matches(LoggedSession expected, LoggedSession? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected with { Sets = [] }, actual with { Sets = [] });
        Assert.Equal(expected.Sets, actual.Sets);
    }
}

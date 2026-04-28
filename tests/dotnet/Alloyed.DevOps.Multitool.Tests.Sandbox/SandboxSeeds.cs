namespace Alloyed.DevOps.Multitool.Tests.Sandbox;

// Fixed seeds produce the same scenario content across all runs and environments.
// Changing a seed is a breaking change to the scenario — treat it as an intentional fixture update.
internal static class SandboxSeeds
{
    internal const int Default = 42;
    internal const int Mixed = 137;
    internal const int Large = 2718;
}

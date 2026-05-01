namespace Alloyed.DevOps.Multitool.Host.PowerShell.Services;

using Models;
using Spectre.Console;

/// <summary>
/// Hosts rich interactive prompts for runtime configuration initialization so Spectre.Console
/// stays confined to the Host.PowerShell layer.
/// </summary>
public static class RuntimeConfigurationPromptService
{
    /// <summary>
    /// Prompts the user for initial runtime configuration defaults using Spectre.Console widgets.
    /// </summary>
    public static RuntimeConfigurationInitializationSelection PromptDefaults()
    {
        var outputPrompt = new SelectionPrompt<string>()
            .Title("Select console output mode:")
            .AddChoices("Plain", "Rich");
        var outputMode = AnsiConsole.Prompt(outputPrompt);

        var enableTransparency = AnsiConsole.Prompt(
            new ConfirmationPrompt("Enable transparency by default?"));

        var enableSession = AnsiConsole.Prompt(
            new ConfirmationPrompt("Enable session mode by default?"));

        var retryPrompt = new SelectionPrompt<string>()
            .Title("Select runtime retry policy:")
            .AddChoices("0", "1", "2", "3");
        var maxRetries = int.Parse(AnsiConsole.Prompt(retryPrompt));

        var enableBackoff = AnsiConsole.Prompt(
            new ConfirmationPrompt("Enable exponential backoff?"));

        var enablePreview = AnsiConsole.Prompt(
            new ConfirmationPrompt("Enable runtime preview logs?"));

        var profilePrompt = new SelectionPrompt<string>()
            .Title("Select transparency output profile:")
            .AddChoices("standard", "minimal", "debug");
        var transparencyProfile = AnsiConsole.Prompt(profilePrompt);

        var applyToCurrentSession = AnsiConsole.Prompt(
            new ConfirmationPrompt("Apply these settings to the current session now?"));

        return new RuntimeConfigurationInitializationSelection(
            outputMode,
            enableTransparency,
            enableSession,
            maxRetries,
            enableBackoff,
            enablePreview,
            transparencyProfile,
            applyToCurrentSession);
    }
}

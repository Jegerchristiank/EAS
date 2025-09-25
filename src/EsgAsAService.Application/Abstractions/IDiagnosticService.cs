namespace EsgAsAService.Application.Abstractions;

using System.Runtime.CompilerServices;

public interface IDiagnosticService
{
    /// <summary>
    /// Records an exception with context and returns a short diagnostic code
    /// safe to show to users and support.
    /// </summary>
    /// <param name="ex">The exception</param>
    /// <param name="area">Logical area (e.g., Wizard.Step1, API.Calculations)</param>
    /// <param name="data">Optional context (anonymous object / dict)</param>
    /// <returns>Diagnostic code</returns>
    string Capture(
        Exception ex,
        string area,
        object? data = null,
        string? code = null,
        [CallerFilePath] string? file = null,
        [CallerMemberName] string? member = null,
        [CallerLineNumber] int line = 0);
}

// This file contains suppressions for analyzer rules that are not applicable
// to ASP.NET Core controllers routing requirements.
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Design",
    "CA1515:Consider making public types internal",
    Justification = "Controllers must be public for ASP.NET Core routing.",
    Scope = "namespaceanddescendants",
    Target = "EsgAsAService.Api.Controllers")]


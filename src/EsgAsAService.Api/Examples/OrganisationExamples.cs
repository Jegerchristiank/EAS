using Swashbuckle.AspNetCore.Filters;
using EsgAsAService.Api.Models;

namespace EsgAsAService.Api.Examples;

public class OrganisationRequestExample : IExamplesProvider<OrganisationRequest>
{
    public OrganisationRequest GetExamples() => new(
        Name: "Acme ApS",
        Industry: "Manufacturing",
        CountryCode: "DK",
        OrganizationNumber: "CVR-12345678");
}


namespace FhirService.Abstraction
{
    public interface IFhirResource
    {
        FhirResourceType ResourceType { get; set; }
    }

    public enum FhirResourceType
    {
        Patient = 0
    }
}

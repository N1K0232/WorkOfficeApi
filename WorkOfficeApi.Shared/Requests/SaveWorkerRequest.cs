using WorkOfficeApi.Shared.Common;
using WorkOfficeApi.Shared.Enums;

namespace WorkOfficeApi.Shared.Requests;

public class SaveWorkerRequest : BaseRequestModel
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public DateTime DateOfBirth { get; set; }

    public string City { get; set; }

    public string Country { get; set; }

    public string HomeAddress { get; set; }

    public string CellphoneNumber { get; set; }

    public string EmailAddress { get; set; }

    public WorkerType WorkerType { get; set; }
}
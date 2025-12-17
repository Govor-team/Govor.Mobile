namespace Govor.Mobile.Utilities;

public class HubResult<T>
{
    public HubResultStatus Status { get; set; }
    public T? Result { get; set; }
    public string? ErrorMessage { get; set; }
    
    public static HubResult<T> Ok(T? result = default) => new()
    {
        Status = HubResultStatus.Success,
        Result = result
    };

    public static HubResult<T> Created(T? result = default) => new()
    {
        Status = HubResultStatus.Created,
        Result = result
    };

    public static HubResult<T> NoContent() => new()
    {
        Status = HubResultStatus.NoContent
    };

    public static HubResult<T> BadRequest(string message, T? details = default) => new()
    {
        Status = HubResultStatus.BadRequest,
        ErrorMessage = message,
        Result = details
    };
    
    public static HubResult<T> NotFound(string message, T? details = default) => new()
    {
        Status = HubResultStatus.NotFound,
        ErrorMessage = message,
        Result = details
    };

    public static HubResult<T> Unauthorized(string message) => new()
    {
        Status = HubResultStatus.Unauthorized,
        ErrorMessage = message
    };
    
    public static HubResult<T> Conflict(string message) => new()
    {
        Status = HubResultStatus.Conflict,
        ErrorMessage = message,
    };

    public static HubResult<T> UnprocessableEntity(string message) => new()
    {
        Status = HubResultStatus.UnprocessableEntity,
        ErrorMessage = message
    };
    
    public static HubResult<T> Error(string message) => new()
    {
        Status = HubResultStatus.ServerError,
        ErrorMessage = message
    };
}

public enum HubResultStatus : int 
{
    Success = 200,
    Created = 201,
    NoContent = 204,
    BadRequest = 400,
    Unauthorized = 401,
    NotFound = 404,
    Conflict = 409,
    UnprocessableEntity = 422,
    ServerError = 500,
}
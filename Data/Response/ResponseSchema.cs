namespace Data.Response;

public class ResponseSchema()
{
    public ResponseSchema(bool success, string? message, List<string>? errors) : this()
    {
        Success = success;
        Message = message;
        Errors = errors;
    }

    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
}

// Generic ResponseSchema (Data property added for successful responses)
public class ResponseSchema<T>() : ResponseSchema()
{
    public ResponseSchema(bool success, T? data, string? message, List<string>? errors) : this()
    {
        Success = success;
        Data = data;
        Message = message;
        Errors = errors;
    }

    public T? Data { get; set; }
}
namespace Data.Response;

public class ResponseSchema
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
}

// Generic ResponseSchema (Data property added for successful responses)
public class ResponseSchema<T> : ResponseSchema
{
    public T? Data { get; set; }
}
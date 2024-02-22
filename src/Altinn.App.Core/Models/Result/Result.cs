namespace Altinn.App.Core.Models.Result;

/// <summary>
/// Result type that can be used when a method can return a value or an error
/// </summary>
/// <typeparam name="T">Type returned when Ok result</typeparam>
/// <typeparam name="TU">Type returned when Error result</typeparam>
public class Result<T, TU> where T: class? where TU: class, IResultError
{
    private readonly T? _value;
    private readonly TU? _error;
    
    /// <summary>
    /// Creates a new Ok result
    /// </summary>
    /// <param name="value">The value returned</param>
    /// <returns></returns>
    public static Result<T, TU> Ok(T value)
    {
        return new Result<T, TU>(value);
    }
    
    /// <summary>
    /// Creates a new Error result
    /// </summary>
    /// <param name="error">The error returned</param>
    /// <returns></returns>
    public static Result<T, TU> Err(TU error)
    {
        return new Result<T, TU>(error);
    }
    
    /// <summary>
    /// Unwraps the value if the result is Ok, otherwise throws an exception
    /// </summary>
    /// <returns>The Ok value</returns>
    /// <exception cref="Exception">Exception with the Error.Reason as message</exception>
    public T Unwrap()
    {
        if (_value == null)
        {
            throw new Exception(_error!.Reason());
        }
        return _value;
    }
    
    /// <summary>
    /// Unwraps the value if the result is Ok, otherwise returns the result of the provided function
    /// </summary>
    /// <param name="err">Function that returns a value of <see cref="T"/> based on the error</param>
    /// <returns></returns>
    public T UnwrapOrElse(Func<TU, T> err)
    {
        return _value ?? err(_error!);
    }
    
    /// <summary>
    /// Maps the result to a new value
    /// </summary>
    /// <param name="ok">Function that maps the Ok value to a new value</param>
    /// <param name="err">Function that maps the Error value to a new value</param>
    /// <typeparam name="TV">The type of the new value</typeparam>
    /// <returns></returns>
    public TV Map<TV>(Func<T, TV> ok, Func<TU, TV> err)
    {
        return _value != null ? ok(_value) : err(_error!);
    }
    
    /// <summary>
    /// Gets the value
    /// </summary>
    /// <returns></returns>
    public T? Value()
    {
        return _value;
    }
    
    /// <summary>
    /// Gets the error
    /// </summary>
    /// <returns></returns>
    public TU? Error()
    {
        return _error;
    }
    
    /// <summary>
    /// Converts the result to a value of <see cref="ResultType"/> to simplify for example switch statements
    /// </summary>
    /// <param name="claim"></param>
    /// <returns></returns>
    public static implicit operator ResultType(Result<T, TU> claim)
    {
        return claim._value != null ? ResultType.Ok : ResultType.Err;
    }

    
    private Result(T value)
    {
        _value = value; 
    }
    
    private Result(TU error)
    {
        _error = error;
    }
    
}

/// <summary>
/// Enum for the different result types
/// </summary>
public enum ResultType
{
    /// <summary>
    /// Ok result
    /// </summary>
    Ok,
    /// <summary>
    /// Error result
    /// </summary>
    Err
}
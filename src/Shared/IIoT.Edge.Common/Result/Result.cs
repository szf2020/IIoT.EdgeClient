namespace IIoT.Edge.Common.Result;

/// <summary>
/// 通用操作结果（简化版，适用于边缘端）
/// 
/// 只关心三件事：成功还是失败、错误信息、数据
/// 不需要云端的 HTTP 状态码语义
/// 
/// 用法：
///   return Result.Success();
///   return Result.Success(data);
///   return Result.Failure("保存失败");
///   
///   if (result.IsSuccess) { var data = result.Value; }
/// </summary>
public class Result<T>
{
    protected Result(T? value, bool isSuccess, string? errorMessage)
    {
        Value = value;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// 返回的数据（失败时为 default）
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// 错误信息（成功时为 null）
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// 内部工厂方法（供 Result 基类调用）
    /// </summary>
    internal static Result<T> Create(T? value, bool isSuccess, string? errorMessage)
        => new(value, isSuccess, errorMessage);

    /// <summary>
    /// 允许从无泛型 Result 隐式转换
    /// </summary>
    public static implicit operator Result<T>(Result result)
        => new(default, result.IsSuccess, result.ErrorMessage);
}

/// <summary>
/// 无数据的操作结果（写操作常用）
/// </summary>
public class Result : Result<object>
{
    private Result(bool isSuccess, string? errorMessage)
        : base(null, isSuccess, errorMessage)
    {
    }

    public static Result Success()
        => new(true, null);

    public static Result<T> Success<T>(T value)
        => Result<T>.Create(value, true, null);

    public static Result Failure(string errorMessage)
        => new(false, errorMessage);
}
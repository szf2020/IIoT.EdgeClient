using MediatR;

namespace IIoT.Edge.Common.Messaging;

/// <summary>
/// 写操作指令基接口
/// 
/// 用法（跟云端一致）：
///   public record SaveSystemConfigsCommand(...) : ICommand<Result>;
///   public class SaveSystemConfigsHandler : ICommandHandler<SaveSystemConfigsCommand, Result> { }
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse>;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;
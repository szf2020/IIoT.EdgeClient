using MediatR;

namespace IIoT.Edge.Common.Messaging;

/// <summary>
/// 读操作查询基接口
/// 
/// 用法（跟云端一致）：
///   public record GetAllSystemConfigsQuery() : IQuery<Result<List<SystemConfigDto>>>;
///   public class GetAllSystemConfigsHandler : IQueryHandler<GetAllSystemConfigsQuery, Result<List<SystemConfigDto>>> { }
/// </summary>
public interface IQuery<out TResponse> : IRequest<TResponse>;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;
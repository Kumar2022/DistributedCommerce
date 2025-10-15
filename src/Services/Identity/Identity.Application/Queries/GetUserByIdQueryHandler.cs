using Identity.Application.DTOs;

namespace Identity.Application.Queries;

/// <summary>
/// Handler for GetUserByIdQuery
/// </summary>
public sealed class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user is null)
            return Result.Failure<UserDto>(Error.NotFound("User", request.UserId));

        var userDto = new UserDto(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.IsActive,
            user.LastLoginAt,
            user.CreatedAt);

        return Result.Success(userDto);
    }
}

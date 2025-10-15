using Identity.Application.DTOs;
using Identity.Application.Services;
using Identity.Domain.ValueObjects;

namespace Identity.Application.Commands;

/// <summary>
/// Handler for LoginCommand
/// </summary>
public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, string>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<string>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        // Validate email format
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Failure<string>(emailResult.Error);

        // Get user by email
        var user = await _userRepository.GetByEmailAsync(emailResult.Value.Value, cancellationToken);
        if (user is null)
            return Result.Failure<string>(Error.NotFound("User", request.Email));

        // Verify password and update login timestamp
        var loginResult = user.Login(request.Password);
        if (loginResult.IsFailure)
            return Result.Failure<string>(loginResult.Error);

        // Save updated login timestamp
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Generate JWT token
        var token = _jwtTokenGenerator.GenerateToken(user);

        return Result.Success(token);
    }
}

using Identity.Application.DTOs;
using Identity.Domain.Aggregates.UserAggregate;
using Identity.Domain.ValueObjects;

namespace Identity.Application.Commands;

/// <summary>
/// Handler for RegisterUserCommand
/// </summary>
public sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        // Create email value object
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Failure<Guid>(emailResult.Error);

        // Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(emailResult.Value.Value, cancellationToken);
        if (existingUser is not null)
            return Result.Failure<Guid>(Error.Conflict("A user with this email already exists"));

        // Create user aggregate
        var userResult = User.Create(
            emailResult.Value,
            request.Password,
            request.FirstName,
            request.LastName);

        if (userResult.IsFailure)
            return Result.Failure<Guid>(userResult.Error);

        // Save to repository
        await _userRepository.AddAsync(userResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(userResult.Value.Id);
    }
}

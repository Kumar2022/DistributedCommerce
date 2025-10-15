using Identity.Domain.Aggregates.UserAggregate;
using Identity.Domain.Events;
using Identity.Domain.ValueObjects;

namespace Identity.UnitTests.Domain;

/// <summary>
/// Unit tests for User aggregate
/// Tests user creation, authentication, and password management
/// </summary>
public class UserTests
{
    #region Create Tests

    [Fact(DisplayName = "Create: Valid parameters should create user")]
    public void Create_WithValidParameters_ShouldCreateUser()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;
        var password = "SecurePassword123!";
        var firstName = "John";
        var lastName = "Doe";

        // Act
        var result = User.Create(email, password, firstName, lastName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Email.Should().Be(email);
        result.Value.FirstName.Should().Be(firstName);
        result.Value.LastName.Should().Be(lastName);
        result.Value.IsActive.Should().BeTrue();
        result.Value.LastLoginAt.Should().BeNull();
        result.Value.Id.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "Create: Should trim names")]
    public void Create_WithWhitespaceInNames_ShouldTrimNames()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;

        // Act
        var result = User.Create(email, "Password123!", "  John  ", "  Doe  ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("John");
        result.Value.LastName.Should().Be("Doe");
    }

    [Fact(DisplayName = "Create: Should raise UserRegisteredEvent")]
    public void Create_WhenSuccessful_ShouldRaiseUserRegisteredEvent()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;

        // Act
        var result = User.Create(email, "Password123!", "John", "Doe");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var domainEvents = result.Value.DomainEvents;
        domainEvents.Should().ContainSingle();
        domainEvents.First().Should().BeOfType<UserRegisteredEvent>();
        
        var userRegisteredEvent = (UserRegisteredEvent)domainEvents.First();
        userRegisteredEvent.UserId.Should().Be(result.Value.Id);
        userRegisteredEvent.Email.Should().Be(email.Value);
    }

    [Theory(DisplayName = "Create: Empty first name should fail")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyFirstName_ShouldFail(string firstName)
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;

        // Act
        var result = User.Create(email, "Password123!", firstName, "Doe");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("First name is required");
    }

    [Theory(DisplayName = "Create: Empty last name should fail")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyLastName_ShouldFail(string lastName)
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;

        // Act
        var result = User.Create(email, "Password123!", "John", lastName);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Last name is required");
    }

    [Theory(DisplayName = "Create: Password too short should fail")]
    [InlineData("")]
    [InlineData("1234567")] // 7 chars
    [InlineData("Pass1")]   // 5 chars
    public void Create_WithShortPassword_ShouldFail(string password)
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;

        // Act
        var result = User.Create(email, password, "John", "Doe");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("at least 8 characters");
    }

    #endregion

    #region Login Tests

    [Fact(DisplayName = "Login: Correct password should succeed")]
    public void Login_WithCorrectPassword_ShouldSucceed()
    {
        // Arrange
        var password = "SecurePassword123!";
        var user = CreateTestUser(password);

        // Act
        var result = user.Login(password);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact(DisplayName = "Login: Should raise UserLoggedInEvent")]
    public void Login_WhenSuccessful_ShouldRaiseUserLoggedInEvent()
    {
        // Arrange
        var password = "SecurePassword123!";
        var user = CreateTestUser(password);
        user.ClearDomainEvents(); // Clear creation events

        // Act
        var result = user.Login(password);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var domainEvents = user.DomainEvents;
        domainEvents.Should().ContainSingle();
        domainEvents.First().Should().BeOfType<UserLoggedInEvent>();
        
        var loggedInEvent = (UserLoggedInEvent)domainEvents.First();
        loggedInEvent.UserId.Should().Be(user.Id);
        loggedInEvent.Email.Should().Be(user.Email.Value);
        loggedInEvent.LoggedInAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact(DisplayName = "Login: Incorrect password should fail")]
    public void Login_WithIncorrectPassword_ShouldFail()
    {
        // Arrange
        var user = CreateTestUser("CorrectPassword123!");

        // Act
        var result = user.Login("WrongPassword123!");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Invalid password");
        user.LastLoginAt.Should().BeNull();
    }

    [Fact(DisplayName = "Login: Inactive user should fail")]
    public void Login_WhenUserInactive_ShouldFail()
    {
        // Arrange
        var password = "SecurePassword123!";
        var user = CreateTestUser(password);
        user.Deactivate();

        // Act
        var result = user.Login(password);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("not active");
    }

    [Fact(DisplayName = "Login: Multiple successful logins should update LastLoginAt")]
    public void Login_MultipleLogins_ShouldUpdateLastLoginAt()
    {
        // Arrange
        var password = "SecurePassword123!";
        var user = CreateTestUser(password);

        // Act
        user.Login(password);
        var firstLoginTime = user.LastLoginAt;
        
        Thread.Sleep(10); // Small delay
        
        user.Login(password);
        var secondLoginTime = user.LastLoginAt;

        // Assert
        secondLoginTime.Should().BeAfter(firstLoginTime.Value);
    }

    #endregion

    #region ChangePassword Tests

    [Fact(DisplayName = "ChangePassword: Correct current password should succeed")]
    public void ChangePassword_WithCorrectCurrentPassword_ShouldSucceed()
    {
        // Arrange
        var currentPassword = "OldPassword123!";
        var newPassword = "NewPassword456!";
        var user = CreateTestUser(currentPassword);

        // Act
        var result = user.ChangePassword(currentPassword, newPassword);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Old password should no longer work
        user.Login(currentPassword).IsFailure.Should().BeTrue();
        
        // New password should work
        user.Login(newPassword).IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "ChangePassword: Should raise PasswordChangedEvent")]
    public void ChangePassword_WhenSuccessful_ShouldRaisePasswordChangedEvent()
    {
        // Arrange
        var user = CreateTestUser("OldPassword123!");
        user.ClearDomainEvents();

        // Act
        var result = user.ChangePassword("OldPassword123!", "NewPassword456!");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var domainEvents = user.DomainEvents;
        domainEvents.Should().ContainSingle();
        domainEvents.First().Should().BeOfType<PasswordChangedEvent>();
        
        var passwordChangedEvent = (PasswordChangedEvent)domainEvents.First();
        passwordChangedEvent.UserId.Should().Be(user.Id);
    }

    [Fact(DisplayName = "ChangePassword: Incorrect current password should fail")]
    public void ChangePassword_WithIncorrectCurrentPassword_ShouldFail()
    {
        // Arrange
        var user = CreateTestUser("CorrectPassword123!");

        // Act
        var result = user.ChangePassword("WrongPassword123!", "NewPassword456!");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Current password is incorrect");
    }

    [Theory(DisplayName = "ChangePassword: New password too short should fail")]
    [InlineData("1234567")] // 7 chars
    [InlineData("Pass1")]   // 5 chars
    [InlineData("")]
    public void ChangePassword_WithShortNewPassword_ShouldFail(string newPassword)
    {
        // Arrange
        var currentPassword = "OldPassword123!";
        var user = CreateTestUser(currentPassword);

        // Act
        var result = user.ChangePassword(currentPassword, newPassword);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("at least 8 characters");
    }

    #endregion

    #region Activate/Deactivate Tests

    [Fact(DisplayName = "Deactivate: Should set IsActive to false")]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var user = CreateTestUser("Password123!");
        user.IsActive.Should().BeTrue();

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
    }

    [Fact(DisplayName = "Activate: Should set IsActive to true")]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var user = CreateTestUser("Password123!");
        user.Deactivate();

        // Act
        user.Activate();

        // Assert
        user.IsActive.Should().BeTrue();
    }

    [Fact(DisplayName = "Activate: After reactivation, login should work")]
    public void Activate_AfterDeactivation_LoginShouldWork()
    {
        // Arrange
        var password = "Password123!";
        var user = CreateTestUser(password);
        user.Deactivate();
        user.Login(password).IsFailure.Should().BeTrue();

        // Act
        user.Activate();
        var loginResult = user.Login(password);

        // Assert
        loginResult.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Integration Scenarios

    [Fact(DisplayName = "Scenario: Complete user lifecycle")]
    public void Scenario_CompleteUserLifecycle_ShouldWorkCorrectly()
    {
        // Arrange & Act - User registration
        var email = Email.Create("user@example.com").Value;
        var initialPassword = "InitialPassword123!";
        var user = User.Create(email, initialPassword, "Jane", "Smith").Value;
        
        user.IsActive.Should().BeTrue();
        user.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<UserRegisteredEvent>();

        // Act - First login
        user.ClearDomainEvents();
        var loginResult = user.Login(initialPassword);
        loginResult.IsSuccess.Should().BeTrue();
        user.LastLoginAt.Should().NotBeNull();
        user.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<UserLoggedInEvent>();

        // Act - Change password
        user.ClearDomainEvents();
        var newPassword = "NewSecurePassword456!";
        var changeResult = user.ChangePassword(initialPassword, newPassword);
        changeResult.IsSuccess.Should().BeTrue();
        user.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PasswordChangedEvent>();

        // Act - Login with new password
        user.ClearDomainEvents();
        var loginWithNewPasswordResult = user.Login(newPassword);
        loginWithNewPasswordResult.IsSuccess.Should().BeTrue();

        // Act - Deactivate account
        user.Deactivate();
        user.IsActive.Should().BeFalse();
        user.Login(newPassword).IsFailure.Should().BeTrue();

        // Act - Reactivate account
        user.Activate();
        user.IsActive.Should().BeTrue();
        user.Login(newPassword).IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Scenario: Failed login attempts")]
    public void Scenario_FailedLoginAttempts_ShouldNotUpdateLastLoginAt()
    {
        // Arrange
        var correctPassword = "CorrectPassword123!";
        var user = CreateTestUser(correctPassword);

        // Act - Multiple failed login attempts
        user.Login("WrongPassword1!").IsFailure.Should().BeTrue();
        user.Login("WrongPassword2!").IsFailure.Should().BeTrue();
        user.Login("WrongPassword3!").IsFailure.Should().BeTrue();

        // Assert - LastLoginAt should still be null
        user.LastLoginAt.Should().BeNull();

        // Act - Successful login
        user.Login(correctPassword).IsSuccess.Should().BeTrue();

        // Assert - LastLoginAt should now be set
        user.LastLoginAt.Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    private static User CreateTestUser(string password)
    {
        var email = Email.Create("test@example.com").Value;
        return User.Create(email, password, "Test", "User").Value;
    }

    #endregion
}

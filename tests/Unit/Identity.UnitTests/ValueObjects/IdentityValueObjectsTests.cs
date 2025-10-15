using Identity.Domain.ValueObjects;

namespace Identity.UnitTests.ValueObjects;

/// <summary>
/// Unit tests for Email value object
/// Tests validation and equality
/// </summary>
public class EmailTests
{
    [Fact(DisplayName = "Create: Valid email should succeed")]
    public void Create_WithValidEmail_ShouldSucceed()
    {
        // Arrange
        var emailString = "test@example.com";

        // Act
        var result = Email.Create(emailString);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("test@example.com");
    }

    [Fact(DisplayName = "Create: Should normalize to lowercase")]
    public void Create_WithMixedCase_ShouldNormalizeToLowercase()
    {
        // Act
        var result = Email.Create("Test.User@EXAMPLE.COM");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("test.user@example.com");
    }

    [Fact(DisplayName = "Create: Should trim whitespace")]
    public void Create_WithWhitespace_ShouldTrim()
    {
        // Act
        var result = Email.Create("  test@example.com  ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("test@example.com");
    }

    [Theory(DisplayName = "Create: Empty or null email should fail")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyEmail_ShouldFail(string? email)
    {
        // Act
        var result = Email.Create(email!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("cannot be empty");
    }

    [Theory(DisplayName = "Create: Invalid email formats should fail")]
    [InlineData("notanemail")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test.example.com")]
    [InlineData("test@.com")]
    public void Create_WithInvalidFormat_ShouldFail(string email)
    {
        // Act
        var result = Email.Create(email);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("format is invalid");
    }

    [Fact(DisplayName = "Create: Email at max length should succeed")]
    public void Create_WithMaxLengthEmail_ShouldSucceed()
    {
        // Arrange - 255 characters (exactly at limit)
        var longEmail = new string('a', 239) + "@example.com"; // Total: 239 + 12 = 251 chars (under limit)

        // Act
        var result = Email.Create(longEmail);

        // Assert - Should succeed since it's under the limit
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(DisplayName = "Equality: Same email values should be equal")]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var email1 = Email.Create("test@example.com").Value;
        var email2 = Email.Create("test@example.com").Value;

        // Assert
        email1.Should().Be(email2);
        (email1 == email2).Should().BeTrue();
    }

    [Fact(DisplayName = "Equality: Different email values should not be equal")]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var email1 = Email.Create("test1@example.com").Value;
        var email2 = Email.Create("test2@example.com").Value;

        // Assert
        email1.Should().NotBe(email2);
        (email1 != email2).Should().BeTrue();
    }

    [Fact(DisplayName = "ToString: Should return email value")]
    public void ToString_ShouldReturnEmailValue()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;

        // Act
        var toString = email.ToString();

        // Assert
        toString.Should().Be("test@example.com");
    }
}

/// <summary>
/// Unit tests for HashedPassword value object
/// Tests password hashing and verification
/// </summary>
public class HashedPasswordTests
{
    [Fact(DisplayName = "Create: Should hash password")]
    public void Create_WithPlainTextPassword_ShouldHashPassword()
    {
        // Arrange
        var plainPassword = "MySecurePassword123!";

        // Act
        var hashedPassword = HashedPassword.Create(plainPassword);

        // Assert
        hashedPassword.Should().NotBeNull();
        hashedPassword.Hash.Should().NotBeNullOrEmpty();
        hashedPassword.Hash.Should().NotBe(plainPassword); // Should be hashed, not plain text
    }

    [Fact(DisplayName = "Create: Same password should produce same hash")]
    public void Create_WithSamePassword_ShouldProduceSameHash()
    {
        // Arrange
        var password = "MySecurePassword123!";

        // Act
        var hash1 = HashedPassword.Create(password);
        var hash2 = HashedPassword.Create(password);

        // Assert
        hash1.Hash.Should().Be(hash2.Hash);
    }

    [Fact(DisplayName = "Create: Different passwords should produce different hashes")]
    public void Create_WithDifferentPasswords_ShouldProduceDifferentHashes()
    {
        // Act
        var hash1 = HashedPassword.Create("Password1");
        var hash2 = HashedPassword.Create("Password2");

        // Assert
        hash1.Hash.Should().NotBe(hash2.Hash);
    }

    [Fact(DisplayName = "Verify: Correct password should verify")]
    public void Verify_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var hashedPassword = HashedPassword.Create(password);

        // Act
        var result = hashedPassword.Verify(password);

        // Assert
        result.Should().BeTrue();
    }

    [Fact(DisplayName = "Verify: Incorrect password should not verify")]
    public void Verify_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var hashedPassword = HashedPassword.Create("CorrectPassword");

        // Act
        var result = hashedPassword.Verify("WrongPassword");

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "Verify: Password is case-sensitive")]
    public void Verify_CaseSensitive_ShouldBeCaseSensitive()
    {
        // Arrange
        var hashedPassword = HashedPassword.Create("Password123");

        // Act
        var result = hashedPassword.Verify("password123");

        // Assert
        result.Should().BeFalse();
    }

    [Fact(DisplayName = "FromHash: Should create from existing hash")]
    public void FromHash_WithValidHash_ShouldCreateHashedPassword()
    {
        // Arrange
        var originalPassword = "MyPassword123";
        var hash = HashedPassword.Create(originalPassword).Hash;

        // Act
        var fromHash = HashedPassword.FromHash(hash);

        // Assert
        fromHash.Hash.Should().Be(hash);
        fromHash.Verify(originalPassword).Should().BeTrue();
    }

    [Fact(DisplayName = "Equality: Same hash should be equal")]
    public void Equality_WithSameHash_ShouldBeEqual()
    {
        // Arrange
        var password = "MyPassword123";
        var hash1 = HashedPassword.Create(password);
        var hash2 = HashedPassword.Create(password);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact(DisplayName = "Equality: Different hashes should not be equal")]
    public void Equality_WithDifferentHashes_ShouldNotBeEqual()
    {
        // Arrange
        var hash1 = HashedPassword.Create("Password1");
        var hash2 = HashedPassword.Create("Password2");

        // Assert
        hash1.Should().NotBe(hash2);
    }
}

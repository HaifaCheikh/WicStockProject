using FluentAssertions;
using WicStock_.Services;
using Xunit;

namespace WicStock.Api.Tests
{
    public class PasswordResetServiceTests
    {
        private readonly PasswordResetService _service;

        public PasswordResetServiceTests()
        {
            _service = new PasswordResetService();
        }

        [Fact]
        public void GenerateCode_ShouldReturn6DigitCode_AndBeVerifiable()
        {
            // Arrange
            var email = "user@example.com";

            // Act
            var code = _service.GenerateCode(email);

            // Assert
            code.Should().NotBeNullOrEmpty();
            code.Length.Should().Be(6);
            int.TryParse(code, out _).Should().BeTrue();

            var isVerified = _service.VerifyCode(email, code);
            isVerified.Should().BeTrue();
        }

        [Fact]
        public void VerifyCode_WithCaseInsensitiveEmailAndSpaces_ShouldSucceed()
        {
            // Arrange
            var emailInput = "  Test.User@Domain.COM  ";
            var code = _service.GenerateCode(emailInput);

            // Act
            var isVerified = _service.VerifyCode("test.user@domain.com", code);

            // Assert
            isVerified.Should().BeTrue();
        }

        [Fact]
        public void VerifyCode_WithWrongCode_ShouldReturnFalse()
        {
            // Arrange
            var email = "test@example.com";
            _service.GenerateCode(email);

            // Act
            var isVerified = _service.VerifyCode(email, "000000");

            // Assert
            isVerified.Should().BeFalse();
        }

        [Fact]
        public void RemoveCode_ShouldPreventSubsequentVerification()
        {
            // Arrange
            var email = "remove@example.com";
            var code = _service.GenerateCode(email);

            // Act
            _service.RemoveCode(email);
            var isVerified = _service.VerifyCode(email, code);

            // Assert
            isVerified.Should().BeFalse();
        }
    }
}

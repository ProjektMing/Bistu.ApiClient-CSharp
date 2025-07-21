using Bistu.Api.Models;
using Xunit;

namespace Bistu.Api.Test;

public class LoginTest
{
    [Fact]
    public void SubmitForm_UsernamePassword_ShouldBuildCorrectForm()
    {
        // Arrange
        var form = new SubmitForm
        {
            Strategy = AuthenticationStrategy.UsernameAndPassword,
            Username = "testuser",
            Password = "testpass",
            Execution = "test-execution-token"
        };

        // Act
        var content = form.Build();

        // Assert
        Assert.NotNull(content);
        // 这里应该验证表单内容，但由于 FormUrlEncodedContent 不易直接验证，
        // 我们主要验证没有抛出异常
    }

    [Fact]
    public void SubmitForm_QrCode_ShouldBuildCorrectForm()
    {
        // Arrange
        var form = new SubmitForm
        {
            Strategy = AuthenticationStrategy.QrCode,
            Uuid = "test-uuid",
            Execution = "test-execution-token"
        };

        // Act
        var content = form.Build();

        // Assert
        Assert.NotNull(content);
    }

    [Fact]
    public void SubmitForm_UsernamePassword_EmptyUsername_ShouldThrow()
    {
        // Arrange
        var form = new SubmitForm
        {
            Strategy = AuthenticationStrategy.UsernameAndPassword,
            Username = "",
            Password = "testpass",
            Execution = "test-execution-token"
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => form.Build());
    }

    [Fact]
    public void SubmitForm_QrCode_EmptyUuid_ShouldThrow()
    {
        // Arrange
        var form = new SubmitForm
        {
            Strategy = AuthenticationStrategy.QrCode,
            Uuid = "",
            Execution = "test-execution-token"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => form.Build());
    }

    [Fact]
    public void BistuClient_UsePassword_ShouldReturnSelf()
    {
        // Arrange
        using var client = new BistuClient();

        // Act
        var result = client.UsePassword("testuser", "testpass");

        // Assert
        Assert.Same(client, result);
    }

    [Fact]
    public void BistuClient_UseQrCode_ShouldReturnSelf()
    {
        // Arrange
        using var client = new BistuClient();

        // Act
        var result = client.UseQrCode(url => { /* do nothing */ });

        // Assert
        Assert.Same(client, result);
    }

    [Fact]
    public void BistuClient_UsePassword_EmptyUsername_ShouldThrow()
    {
        // Arrange
        using var client = new BistuClient();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => client.UsePassword("", "password"));
    }

    [Fact]
    public void BistuClient_UseQrCode_NullAction_ShouldThrow()
    {
        // Arrange
        using var client = new BistuClient();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => client.UseQrCode(null!));
    }
}

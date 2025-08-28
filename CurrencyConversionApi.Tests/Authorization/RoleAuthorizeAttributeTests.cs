using CurrencyConversionApi.Authorization;
using CurrencyConversionApi.Models;
using FluentAssertions;
using Xunit;

namespace CurrencyConversionApi.Tests.Authorization;

public class RoleAuthorizeAttributeTests
{
    [Fact]
    public void RoleAuthorizeAttribute_Should_Set_Single_Role()
    {
        // Arrange & Act
        var attribute = new RoleAuthorizeAttribute(UserRoles.Admin);

        // Assert
        attribute.Roles.Should().Be("Admin");
    }

    [Fact]
    public void RoleAuthorizeAttribute_Should_Set_Multiple_Roles()
    {
        // Arrange & Act
        var attribute = new RoleAuthorizeAttribute(UserRoles.Premium, UserRoles.Admin);

        // Assert
        attribute.Roles.Should().Be("Premium,Admin");
    }

    [Fact]
    public void RoleAuthorizeAttribute_Should_Handle_Empty_Roles()
    {
        // Arrange & Act
        var attribute = new RoleAuthorizeAttribute();

        // Assert
        attribute.Roles.Should().BeEmpty();
    }

    [Fact]
    public void AdminOnlyAttribute_Should_Set_Admin_Role_Only()
    {
        // Arrange & Act
        var attribute = new AdminOnlyAttribute();

        // Assert
        attribute.Roles.Should().Be("Admin");
    }

    [Fact]
    public void PremiumOrAdminAttribute_Should_Set_Premium_And_Admin_Roles()
    {
        // Arrange & Act
        var attribute = new PremiumOrAdminAttribute();

        // Assert
        attribute.Roles.Should().Be("Premium,Admin");
    }

    [Fact]
    public void AuthenticatedUserAttribute_Should_Set_All_User_Roles()
    {
        // Arrange & Act
        var attribute = new AuthenticatedUserAttribute();

        // Assert
        attribute.Roles.Should().Be("Basic,Premium,Admin");
    }

    [Theory]
    [InlineData("Role1")]
    [InlineData("Role1", "Role2")]
    [InlineData("Role1", "Role2", "Role3")]
    public void RoleAuthorizeAttribute_Should_Join_Multiple_Roles_With_Comma(params string[] roles)
    {
        // Arrange & Act
        var attribute = new RoleAuthorizeAttribute(roles);

        // Assert
        var expectedRoles = string.Join(",", roles);
        attribute.Roles.Should().Be(expectedRoles);
    }
}

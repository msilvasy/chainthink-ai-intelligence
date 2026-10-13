using Microsoft.Extensions.Options;

namespace DocumentAI.Configuration;

public sealed class JwtSettingsValidator : IValidateOptions<JwtSettings>
{
    public ValidateOptionsResult Validate(string? name, JwtSettings options)
    {
        if (options == null)
            return ValidateOptionsResult.Fail("JwtSettings cannot be null");

        if (string.IsNullOrWhiteSpace(options.SecretKey))
            return ValidateOptionsResult.Fail("JwtSettings.SecretKey is required");

        if (options.SecretKey.Length < 32)
            return ValidateOptionsResult.Fail("JwtSettings.SecretKey must be at least 32 characters long");

        if (string.IsNullOrWhiteSpace(options.Issuer))
            return ValidateOptionsResult.Fail("JwtSettings.Issuer is required");

        if (string.IsNullOrWhiteSpace(options.Audience))
            return ValidateOptionsResult.Fail("JwtSettings.Audience is required");

        return ValidateOptionsResult.Success;
    }
}
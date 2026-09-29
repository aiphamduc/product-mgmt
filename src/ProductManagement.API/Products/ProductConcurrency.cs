namespace ProductManagement.API.Products;

public static class ProductConcurrency
{
    public static string ForProduct(byte[] rowVersion) => $"\"product-{Convert.ToBase64String(rowVersion)}\"";

    public static string ForVariant(byte[] rowVersion) => $"\"variant-{Convert.ToBase64String(rowVersion)}\"";

    public static bool TryParse(string? header, string prefix, out byte[] version)
    {
        version = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(header)) return false;
        var value = header.Trim().Trim('"');
        if (!value.StartsWith(prefix, StringComparison.Ordinal)) return false;
        try
        {
            version = Convert.FromBase64String(value[prefix.Length..]);
            return version.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool Matches(string? header, string etag) =>
        string.Equals(header?.Trim(), etag, StringComparison.Ordinal);
}

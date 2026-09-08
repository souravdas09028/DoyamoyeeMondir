namespace DoyamoyeeMondir.Web.Services;

public static class DocumentFiles
{
    public const int MaxBytes = 10 * 1024 * 1024;
    public static (string ContentType, string Extension)? Detect(byte[] content)
    {
        if (content.Length < 8 || content.Length > MaxBytes) return null;
        if (content.AsSpan(0, 5).SequenceEqual("%PDF-"u8)) return ("application/pdf", ".pdf");
        if (content.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ("image/png", ".png");
        if (content[0] == 255 && content[1] == 216 && content[2] == 255) return ("image/jpeg", ".jpg");
        return null;
    }
}

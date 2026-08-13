namespace Lingarr.Server.Exceptions;

public class TranslationParseException : Exception
{
    public TranslationParseException(string message, Exception? exception = null)
        : base(message, exception)
    {
    }
}

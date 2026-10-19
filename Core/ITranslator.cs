namespace ScreenTranslate.Core;

public interface ITranslator
{
    Task<string> TranslateAsync(string text, CancellationToken cancellationToken);
}

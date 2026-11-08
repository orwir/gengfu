using System.Globalization;

namespace Gengfu.App.Tests;

/// <summary>Shows the strings in one language for one test, and puts the thread's language back after.</summary>
internal sealed class UiCulture : IDisposable
{
    private readonly CultureInfo _was = CultureInfo.CurrentUICulture;

    public UiCulture(string name) => CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);

    public void Dispose() => CultureInfo.CurrentUICulture = _was;
}

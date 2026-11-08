namespace FormuLang.NET.Abstractions.Processing
{
    public interface ICleaner
    {
        ICleanUpResult CleanUp(string expression);
        //Task<ICleanUpResult> CleanUpAsync(string expression);
    }
}
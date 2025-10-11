using System;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Marks an instance method that returns a readable representation for save inspection tools.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class SaveReadableMethodAttribute : Attribute
    {
    }
}
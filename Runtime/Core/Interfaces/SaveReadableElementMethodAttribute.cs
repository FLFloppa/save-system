using System;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Provides runtime annotations for readable UI via attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class SaveReadableElementMethodAttribute : Attribute
    {
    }
}
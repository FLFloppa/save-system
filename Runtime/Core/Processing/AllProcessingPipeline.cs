using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FLFloppa.SaveSystem
{
    /// <summary>
    /// Processing pipeline that executes all registered modules in order for saving and reverse order for loading.
    /// </summary>
    public sealed class AllProcessingPipeline : IProcessingPipeline
    {
        private readonly ReadOnlyCollection<IProcessingModule> _modules;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllProcessingPipeline"/> class.
        /// </summary>
        /// <param name="modules">Processing modules to apply in sequence.</param>
        public AllProcessingPipeline(IEnumerable<IProcessingModule> modules)
        {
            if (modules == null)
            {
                throw new ArgumentNullException(nameof(modules));
            }

            var list = new List<IProcessingModule>();
            foreach (var module in modules)
            {
                if (module == null)
                {
                    throw new SaveSystemException("Processing pipeline cannot contain null modules.");
                }

                list.Add(module);
            }

            _modules = new ReadOnlyCollection<IProcessingModule>(list);
        }

        /// <inheritdoc />
        public IReadOnlyList<IProcessingModule> AllModules => _modules;

        /// <inheritdoc />
        public byte[] ProcessSave(byte[] data)
        {
            if (data == null)
            {
                throw new SaveSystemException("Processing pipeline cannot process null save data.");
            }

            var current = data;
            foreach (var module in _modules)
            {
                current = module.Process(current) ?? throw new SaveSystemException(
                    $"Module '{module.GetType().Name}' returned null during save processing.");
            }

            return current;
        }

        /// <inheritdoc />
        public byte[] ProcessLoad(byte[] data)
        {
            if (data == null)
            {
                throw new SaveSystemException("Processing pipeline cannot process null load data.");
            }

            var current = data;
            for (var i = _modules.Count - 1; i >= 0; i--)
            {
                var module = _modules[i];
                current = module.Reverse(current) ?? throw new SaveSystemException(
                    $"Module '{module.GetType().Name}' returned null during load processing.");
            }

            return current;
        }

        /// <inheritdoc />
        public IReadOnlyList<IProcessingModule> GetProcessingChain() => _modules;
    }
}

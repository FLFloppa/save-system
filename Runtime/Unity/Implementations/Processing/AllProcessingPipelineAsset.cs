using System.Collections.Generic;
using UnityEngine;

namespace FLFloppa.SaveSystem
{
    [CreateAssetMenu(
        fileName = "AllProcessingPipeline",
        menuName = "FLFloppa/Save System/Processing/All Modules Pipeline",
        order = 300)]
    public sealed class AllProcessingPipelineAsset : ProcessingPipelineConfiguration
    {
        [SerializeField]
        [Tooltip("Ordered list of processing modules applied during save operations.")]
        private List<ProcessingModuleConfiguration> _modules = new();

        public override IProcessingPipeline Build()
        {
            var builtModules = new List<IProcessingModule>(_modules.Count);

            foreach (var moduleConfig in _modules)
            {
                if (moduleConfig == null)
                {
                    continue;
                }

                builtModules.Add(moduleConfig.Build());
            }

            return new AllProcessingPipeline(builtModules);
        }
    }
}

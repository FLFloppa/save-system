using UnityEngine;

namespace FLFloppa.SaveSystem
{
    public abstract class ProcessingPipelineConfiguration : ScriptableObject
    {
        public abstract IProcessingPipeline Build();
    }
}
using UnityEngine;

namespace FLFloppa.SaveSystem
{
    [CreateAssetMenu(
        fileName = "GzipProcessingModule",
        menuName = "FLFloppa/Save System/Processing/GZip Compression",
        order = 310)]
    public sealed class GzipProcessingModuleAsset : ProcessingModuleConfiguration
    {
        public override IProcessingModule Build()
        {
            return new GzipProcessingModule();
        }
    }
}

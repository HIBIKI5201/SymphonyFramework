using System;

namespace SymphonyFrameWork.Editor
{
    [Serializable]
    public class AssemblyDefinitionData
    {
        public AssemblyDefinitionData(string name)
        {
            this.name = name;
        }

        public string name = string.Empty;
        public string rootNamespace = string.Empty;
        public string[] references = new string[0];
        public string[] includePlatforms = new string[0];
        public string[] excludePlatforms = new string[0];
        public bool allowUnsafeCode;
        public bool overrideReferences;
        public string[] precompiledReferences = new string[0];
        public bool autoReferenced = true;
        public string[] defineConstraints = new string[0];
        public string[] versionDefines = new string[0];
        public bool noEngineReferences;
        public string[] platforms = new string[0];
    }
}

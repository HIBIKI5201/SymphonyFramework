using System;
using UnityEngine;

namespace SymphonyFrameWork.Attribute
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SubclassSelectorAttribute : PropertyAttribute
    {
        public SubclassSelectorAttribute(bool includeMono = false)
        {
            _includeMono = includeMono;
        }

        public bool IsIncludeMono() => _includeMono;

        private readonly bool _includeMono;

    }
}

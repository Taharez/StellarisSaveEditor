using System;

namespace StellarisSaveEditor.Attributes
{
    public class EnumFileFieldAttribute : Attribute
    {
        public string Name { get; private set; }

        public EnumFileFieldAttribute(string name)
        {
            this.Name = name;
        }
    }
}

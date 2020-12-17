using System;

namespace VcxProjLib {
    public class Define {
        public static readonly char[] Separator = {'='};
        public static readonly String DefaultValue = "";

        public Define(String name, String value) {
            Name = name;
            Value = value;
        }

        public Define(String defineString) {
            String[] parts = defineString.Split(Separator, 2, StringSplitOptions.RemoveEmptyEntries);
            Name = parts[0];
            if (parts.Length < 2) {
                Value = DefaultValue;
            }
            else {
                Value = parts[1];
            }
        }

        public String Name { get; private set; }
        public String Value { get; set; }

        public override String ToString() {
            if (Value == DefaultValue) {
                return Name;
            }

            return Name + "=" + Value;
        }
    }
}
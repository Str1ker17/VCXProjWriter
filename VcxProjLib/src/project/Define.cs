using System;

namespace VcxProjLib {
    public class Define : IComparable {
        public static readonly Char[] Separator = {'='};
        public static readonly String DefaultValue = "";

        public static DefineExactComparer ExactComparer = new DefineExactComparer();

        public Define(String name, String value) {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Creates a Define object from a string of format NAME[=VALUE]
        /// </summary>
        /// <param name="defineString">String like 'USE_TYPE_T' or 'MAX_PATH=512'</param>
        public Define(String defineString) {
            String[] parts = defineString.Split(Separator, 2, StringSplitOptions.RemoveEmptyEntries);
            Name = parts[0];
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (parts.Length < 2) {
                Value = DefaultValue;
            }
            else {
                // unquotize value if required
                Value = parts[1].Trim('"');
            }
        }

        public String Name { get; }
        public String Value { get; set; }

        public override String ToString() {
            if (Value == DefaultValue) {
                return Name;
            }

            return Name + "=" + Value;
        }

        /// <summary>
        /// Compare defines by name to sort them.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public Int32 CompareTo(Object obj) {
            if(obj == null) return Int32.MaxValue;
            if(!(obj is Define define)) return Int32.MaxValue;
            return String.Compare(this.Name, define.Name, StringComparison.Ordinal);
        }
    }
}
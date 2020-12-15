namespace VcxProjLib.HelperClasses.Filters {
    // Примечание. Для запуска созданного кода может потребоваться NET Framework версии 4.5 или более поздней версии и .NET Core или Standard версии 2.0 или более поздней.
    /// <remarks/>
    [System.Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true,
            Namespace = "http://schemas.microsoft.com/developer/msbuild/2003")]
    [System.Xml.Serialization.XmlRoot(Namespace = "http://schemas.microsoft.com/developer/msbuild/2003",
            IsNullable = false)]
    public class Project {
        private ProjectItemGroup[] itemGroupField;

        private decimal toolsVersionField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("ItemGroup")]
        public ProjectItemGroup[] ItemGroup {
            get { return this.itemGroupField; }
            set { this.itemGroupField = value; }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute]
        public decimal ToolsVersion {
            get { return this.toolsVersionField; }
            set { this.toolsVersionField = value; }
        }
    }

    /// <remarks/>
    [System.Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true,
            Namespace = "http://schemas.microsoft.com/developer/msbuild/2003")]
    public class ProjectItemGroup {
        private ProjectItemGroupClCompile[] clCompileField;

        private ProjectItemGroupClInclude[] clIncludeField;

        private ProjectItemGroupFilter[] filterField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("ClCompile")]
        public ProjectItemGroupClCompile[] ClCompile {
            get { return this.clCompileField; }
            set { this.clCompileField = value; }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("ClInclude")]
        public ProjectItemGroupClInclude[] ClInclude {
            get { return this.clIncludeField; }
            set { this.clIncludeField = value; }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("Filter")]
        public ProjectItemGroupFilter[] Filter {
            get { return this.filterField; }
            set { this.filterField = value; }
        }
    }

    /// <remarks/>
    [System.Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true,
            Namespace = "http://schemas.microsoft.com/developer/msbuild/2003")]
    public class ProjectItemGroupClCompile {
        private string filterField;

        private string includeField;

        /// <remarks/>
        public string Filter {
            get { return this.filterField; }
            set { this.filterField = value; }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute]
        public string Include {
            get { return this.includeField; }
            set { this.includeField = value; }
        }
    }

    /// <remarks/>
    [System.Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true,
            Namespace = "http://schemas.microsoft.com/developer/msbuild/2003")]
    public class ProjectItemGroupClInclude {
        private string filterField;

        private string includeField;

        /// <remarks/>
        public string Filter {
            get { return this.filterField; }
            set { this.filterField = value; }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute]
        public string Include {
            get { return this.includeField; }
            set { this.includeField = value; }
        }
    }

    /// <remarks/>
    [System.Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true,
            Namespace = "http://schemas.microsoft.com/developer/msbuild/2003")]
    public class ProjectItemGroupFilter {
        private string uniqueIdentifierField;

        private string includeField;

        /// <remarks/>
        public string UniqueIdentifier {
            get { return this.uniqueIdentifierField; }
            set { this.uniqueIdentifierField = value; }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute]
        public string Include {
            get { return this.includeField; }
            set { this.includeField = value; }
        }
    }
}
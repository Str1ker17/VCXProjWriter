// Примечание. Для запуска созданного кода может потребоваться NET Framework версии 4.5 или более поздней версии и .NET Core или Standard версии 2.0 или более поздней.

namespace VcxProjLib.HelperClasses.Projects {
    /// <remarks/>
    [System.Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true
          , Namespace = "http://schemas.microsoft.com/developer/msbuild/2003")]
    [System.Xml.Serialization.XmlRoot(Namespace = "http://schemas.microsoft.com/developer/msbuild/2003"
          , IsNullable = false, ElementName = "Project")]
    public class VS2019LinuxProject : VCXProject {
        private ProjectImport importField;

        private ProjectPropertyGroup[] propertyGroupField;

        private ProjectItemGroup[] itemGroupField;

        private string defaultTargetsField;

        private decimal toolsVersionField;

        /// <remarks/>
        public ProjectImport Import {
            get { return this.importField; }
            set { this.importField = value; }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("PropertyGroup")]
        public ProjectPropertyGroup[] PropertyGroup {
            get { return this.propertyGroupField; }
            set { this.propertyGroupField = value; }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("ItemGroup")]
        public ProjectItemGroup[] ItemGroup {
            get { return this.itemGroupField; }
            set { this.itemGroupField = value; }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute]
        public string DefaultTargets {
            get { return this.defaultTargetsField; }
            set { this.defaultTargetsField = value; }
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
    public class ProjectImport {
        private string projectField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute]
        public string Project {
            get { return this.projectField; }
            set { this.projectField = value; }
        }
    }

    /// <remarks/>
    [System.Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true,
            Namespace = "http://schemas.microsoft.com/developer/msbuild/2003")]
    public class ProjectPropertyGroup {
        private string nMakeIncludeSearchPathField;

        private string nMakeForcedIncludesField;

        private string nMakePreprocessorDefinitionsField;

        private string additionalOptionsField;

        private string projectGuidField;

        private string labelField;

        /// <remarks/>
        public string NMakeIncludeSearchPath {
            get { return this.nMakeIncludeSearchPathField; }
            set { this.nMakeIncludeSearchPathField = value; }
        }

        /// <remarks/>
        public string NMakeForcedIncludes {
            get { return this.nMakeForcedIncludesField; }
            set { this.nMakeForcedIncludesField = value; }
        }

        /// <remarks/>
        public string NMakePreprocessorDefinitions {
            get { return this.nMakePreprocessorDefinitionsField; }
            set { this.nMakePreprocessorDefinitionsField = value; }
        }

        /// <remarks/>
        public string AdditionalOptions {
            get { return this.additionalOptionsField; }
            set { this.additionalOptionsField = value; }
        }

        /// <remarks/>
        public string ProjectGuid {
            get { return this.projectGuidField; }
            set { this.projectGuidField = value; }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute]
        public string Label {
            get { return this.labelField; }
            set { this.labelField = value; }
        }
    }

    /// <remarks/>
    [System.Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true,
            Namespace = "http://schemas.microsoft.com/developer/msbuild/2003")]
    public class ProjectItemGroup {
        private ProjectItemGroupClInclude[] clIncludeField;

        private ProjectItemGroupClCompile[] clCompileField;

        private string labelField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("ClInclude")]
        public ProjectItemGroupClInclude[] ClInclude {
            get { return this.clIncludeField; }
            set { this.clIncludeField = value; }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("ClCompile")]
        public ProjectItemGroupClCompile[] ClCompile {
            get { return this.clCompileField; }
            set { this.clCompileField = value; }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute]
        public string Label {
            get { return this.labelField; }
            set { this.labelField = value; }
        }
    }

    /// <remarks/>
    [System.Serializable]
    [System.ComponentModel.DesignerCategory("code")]
    [System.Xml.Serialization.XmlType(AnonymousType = true,
            Namespace = "http://schemas.microsoft.com/developer/msbuild/2003")]
    public class ProjectItemGroupClInclude {
        private string includeField;

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
    public class ProjectItemGroupClCompile {
        private string includeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttribute]
        public string Include {
            get { return this.includeField; }
            set { this.includeField = value; }
        }
    }
}
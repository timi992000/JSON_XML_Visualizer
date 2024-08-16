using System.Collections.ObjectModel;

namespace JSON_XML_Visualizer.Entities
{
    internal class XTreeNode : ViewModelBase
    {

        public string Name { get; set; } = "";
        public string Value
        {
            get => Get<string>();
            set => Set(value);
        }
        public ObservableCollection<XTreeNode> Children { get; set; } = [];

        public bool IsInEdit
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsUpon(nameof(Value))]
        public string NameValueString => $"\"{Name}\": {Value}";
    }
}

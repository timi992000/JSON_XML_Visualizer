using System.Collections.ObjectModel;

namespace JSON_XML_Visualizer.Entities
{
    internal class JsonTreeNode
    {

        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public ObservableCollection<JsonTreeNode> Children { get; set; } = [];

        public string NameValueString => $"\"{Name}\": {Value}";
    }
}

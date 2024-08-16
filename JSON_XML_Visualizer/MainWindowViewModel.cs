using JSON_XML_Visualizer.Entities;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Xml.Linq;

namespace JSON_XML_Visualizer
{
    internal class MainWindowViewModel : ViewModelBase
    {

        public ObservableCollection<XTreeNode> TreeNodes
        {
            get => Get<ObservableCollection<XTreeNode>>();
            set => Set(value);
        }

        public XTreeNode SelectedNode 
        { 
            get => Get<XTreeNode>();
            set => Set(value); 
        }

        [DependsUpon(nameof(FileName))]
        public bool HasFile => FileName.IsNotNullOrEmpty();

        public string FileName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SelectedFileText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool EditMode
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsUpon(nameof(EditMode))]
        public Visibility EditModeVisibility => EditMode ? Visibility.Visible : Visibility.Collapsed;

        [DependsUpon(nameof(TreeNodes))]
        public bool CanExecute_RemoveFile() => TreeNodes != null;
        public void Execute_RemoveFile()
        {
            EditMode = false;
            TreeNodes = null;
            FileName = "";
            SelectedFileText = "";
        }

        public void Execute_SelectFile()
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "JSON/XML Files|*.json;*.xml;";

            SelectedFileText = string.Empty;
            FileName = string.Empty;

            dlg.ShowDialog();

            if (dlg.FileName.IsNotNullOrEmpty())
            {
                FileName = dlg.FileName;
                try
                {
                    var allLines = File.ReadAllLines(FileName);
                    int lineCount = allLines.Length;
                    string contentString = string.Concat(allLines);

                    var rootNodes = new ObservableCollection<XTreeNode>();

                    if (FileName.EndsWith("json", StringComparison.OrdinalIgnoreCase))
                        __ParseJson(contentString, rootNodes);
                    else if (FileName.EndsWith("xml", StringComparison.OrdinalIgnoreCase))
                    {
                        __ParseXml(rootNodes);
                    }
                    else
                        return;

                    TreeNodes = rootNodes;
                    SelectedFileText = $"{FileName} ({lineCount:N0} Lines)";
                }
                catch (Exception)
                {
                }
            }
        }

        [DependsUpon(nameof(EditMode))]
        public bool CanExecute_SaveFile() => EditMode;

        public void Execute_SaveFile()
        {

        }

        public void Execute_EditNode()
        { 
            if(SelectedNode != null)
                SelectedNode.IsInEdit = true;
        }

        private void __ParseJson(string contentString, ObservableCollection<XTreeNode> rootNodes)
        {
            using (JsonDocument doc = JsonDocument.Parse(contentString))
            {
                JsonElement root = doc.RootElement;
                rootNodes.Add(ProcessJsonElement(root, "Root"));
            }
        }

        private XTreeNode ProcessJsonElement(JsonElement element, string name)
        {
            var node = new XTreeNode { Name = name };

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (JsonProperty property in element.EnumerateObject())
                    {
                        node.Children.Add(ProcessJsonElement(property.Value, property.Name));
                    }
                    break;
                case JsonValueKind.Array:
                    int index = 0;
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        node.Children.Add(ProcessJsonElement(item, $"[{index}]"));
                        index++;
                    }
                    break;
                case JsonValueKind.String:
                    node.Value = element.GetString();
                    break;
                case JsonValueKind.Number:
                    node.Value = element.GetDouble().ToString();
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    node.Value = element.GetBoolean().ToString();
                    break;
                case JsonValueKind.Null:
                    node.Value = "null";
                    break;
                default:
                    throw new NotImplementedException();
            }

            return node;
        }

        private void __ParseXml(ObservableCollection<XTreeNode> rootNodes)
        {
            var doc = XDocument.Load(FileName);
            rootNodes.Add(ProcessXmlElement(doc.Root));
        }

        private XTreeNode ProcessXmlElement(XElement element)
        {
            var node = new XTreeNode
            {
                Name = element.Name.LocalName,
                Value = element.HasElements ? null : element.Value.Trim()
            };

            foreach (var childElement in element.Elements())
            {
                node.Children.Add(ProcessXmlElement(childElement));
            }

            return node;
        }

        private void __SaveTreeNode()
        {
            if (FileName.IsNullOrEmpty() || TreeNodes.IsEmpty())
                return;

            if (FileName.EndsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                __ConvertAndWriteJSON();
            }
            else if (FileName.EndsWith("xml", StringComparison.OrdinalIgnoreCase))
            {

            }
        }

        private void __ConvertAndWriteJSON()
        {
            var jsonElements = new Dictionary<string, JsonElement>();

            foreach (var rootNode in TreeNodes)
            {
                jsonElements[rootNode.Name] = ConvertTreeNodeToJsonElement(rootNode);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(jsonElements, options);
            var copyFileName = Path.Combine(Path.GetDirectoryName(FileName), Path.GetFileNameWithoutExtension(FileName) + "_Changed" + Path.GetExtension(FileName));

            File.WriteAllText(FileName, jsonString);
        }

        private string ConvertTreeNodesToJson(XTreeNode node)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonElement = ConvertTreeNodeToJsonElement(node);
            return JsonSerializer.Serialize(jsonElement, options);
        }

        private JsonElement ConvertTreeNodeToJsonElement(XTreeNode node)
        {
            using (var doc = JsonDocument.Parse("{}"))
            {
                var jsonObject = new Dictionary<string, JsonElement>();

                if (node.Children.Count > 0)
                {
                    var childrenObject = new Dictionary<string, JsonElement>();
                    foreach (var child in node.Children)
                    {
                        childrenObject[child.Name] = ConvertTreeNodeToJsonElement(child);
                    }

                    var jsonObjectNode = JsonSerializer.SerializeToElement(childrenObject);
                    jsonObject[node.Name] = jsonObjectNode;
                }
                else
                {
                    jsonObject[node.Name] = JsonDocument.Parse($"\"{node.Value}\"").RootElement;
                }

                var jsonNode = JsonSerializer.SerializeToElement(jsonObject);
                return jsonNode;
            }
        }

    }
}

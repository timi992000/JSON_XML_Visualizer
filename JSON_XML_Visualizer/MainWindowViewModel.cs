using JSON_XML_Visualizer.Entities;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Xml.Linq;

namespace JSON_XML_Visualizer
{
    internal class MainWindowViewModel : ViewModelBase
    {

        public ObservableCollection<XTreeNode> DeserializesJSONItems
        {
            get => Get<ObservableCollection<XTreeNode>>();
            set => Set(value);
        }

        public string SelectedFileText
        {
            get => Get<string>();
            set => Set(value);
        }

        [DependsUpon(nameof(DeserializesJSONItems))]
        public bool CanExecute_RemoveFile() => DeserializesJSONItems != null;
        public void Execute_RemoveFile()
        {
            DeserializesJSONItems = null;
            SelectedFileText = "";
        }

        public void Execute_SelectFile()
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "JSON/XML Files|*.json;*.xml;";

            SelectedFileText = string.Empty;

            dlg.ShowDialog();


            if (dlg.FileName.IsNotNullOrEmpty())
            {

                try
                {
                    var allLines = File.ReadAllLines(dlg.FileName);
                    int lineCount = allLines.Length;
                    string contentString = string.Concat(allLines);


                    var rootNodes = new ObservableCollection<XTreeNode>();

                    if (dlg.FileName.EndsWith("json", StringComparison.OrdinalIgnoreCase))
                        __ParseJson(contentString, rootNodes);
                    else if (dlg.FileName.EndsWith("xml", StringComparison.OrdinalIgnoreCase))
                    {
                        __ParseXml(dlg.FileName, rootNodes);
                    }
                    else
                        return;

                    DeserializesJSONItems = rootNodes;
                    SelectedFileText = $"{dlg.FileName} ({lineCount:N0} Lines)";
                }
                catch (Exception)
                {
                }

            }
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

        private void __ParseXml(string fileName, ObservableCollection<XTreeNode> rootNodes)
        {
            var doc = XDocument.Load(fileName);
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

    }
}

using JSON_XML_Visualizer.Entities;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Policy;
using System.Text.Json;

namespace JSON_XML_Visualizer
{
    internal class MainWindowViewModel : ViewModelBase
    {

        public ObservableCollection<JsonTreeNode> DeserializesJSONItems
        {
            get => Get<ObservableCollection<JsonTreeNode>>();
            set => Set(value);
        }

        public string SelectedFileText
        {
            get => Get<string>();
            set => Set(value);
        }

        public void Execute_SelectFile()
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "JSON Files|*.json;";

            SelectedFileText = string.Empty;

            dlg.ShowDialog();


            if (dlg.FileName.IsNotNullOrEmpty())
            {

                try
                {
                    var allLines = File.ReadAllLines(dlg.FileName);
                    int lineCount = allLines.Length;
                    string jsonString = string.Concat(allLines);


                    var rootNodes = new ObservableCollection<JsonTreeNode>();

                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        JsonElement root = doc.RootElement;
                        rootNodes.Add(ProcessJsonElement(root, "Root"));
                    }

                    DeserializesJSONItems = rootNodes;
                    SelectedFileText = $"{dlg.FileName} ({lineCount:N0} Lines)";
                }
                catch (Exception)
                {
                }

            }
        }

        private JsonTreeNode ProcessJsonElement(JsonElement element, string name)
        {
            var node = new JsonTreeNode { Name = name };

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
    }
}

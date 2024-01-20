using StellarisSaveEditor.Models;
using Microsoft.UI.Xaml.Controls;

namespace StellarisSaveEditor.Helpers
{
    public class GameStateRawHelpers
    {
        public static void PopulateGameStateRawSections(ListView sectionListView, GameStateRawSection rawSection)
        {
            if (sectionListView?.Items == null)
                return;

            foreach (var childSection in rawSection.Sections)
            {
                var section = new ListViewItem()
                {
                    Content = string.IsNullOrEmpty(childSection.Name) ? "*" : childSection.Name,
                    DataContext = childSection
                };
                sectionListView.Items.Add(section);
            }
        }

        public static void PopulateGameStateRawAttributes(ListView sectionListView, GameStateRawSection rawSection)
        {
            if (sectionListView?.Items == null)
                return;

            foreach (var attribute in rawSection.Attributes)
            {
                var section = new ListViewItem()
                {
                    Content = (string.IsNullOrEmpty(attribute.Name) ? "" : attribute.Name + ": ") + attribute.Value,
                    DataContext = attribute
                };
                sectionListView.Items.Add(section);
            }
        }
        
        public static void PopulateGameStateRawSectionDetails(ListView sectionListView, GameStateRawSection rawSection)
        {
            PopulateGameStateRawSections(sectionListView, rawSection);

            PopulateGameStateRawAttributes(sectionListView, rawSection);            
        }

        public static void PopulateGameStateRawSectionDetails(TreeViewNode node, GameStateRawSection rawSection)
        {
            foreach (var childSection in rawSection.Sections)
            {
                var childNode = new TreeViewNode { Content = string.IsNullOrEmpty(childSection.Name) ? "*" : childSection.Name };
                node.Children.Add(childNode);
                PopulateGameStateRawSectionDetails(childNode, childSection);
            }

            foreach (var attribute in rawSection.Attributes)
            {
                node.Children.Add(new TreeViewNode { Content = (string.IsNullOrEmpty(attribute.Name) ? "" : attribute.Name + ": ") + attribute.Value });
            }
        }
    }
}

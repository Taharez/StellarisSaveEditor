using System;
using System.Diagnostics;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace StellarisSaveEditor.Helpers
{
    public static class SvgXamlHelper
    {
        // From https://stackoverflow.com/questions/22989172/convert-path-to-geometric-shape
        public static Geometry PathMarkupToGeometry(string pathMarkup)
        {
            try
            {
                string xaml =
                "<Path " +
                "xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
                "<Path.Data>" + pathMarkup + "</Path.Data></Path>";
                // Detach the PathGeometry from the Path
                if (XamlReader.Load(xaml) is Path path)
                {
                    var geometry = path.Data;
                    path.Data = null;
                    return geometry;
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return null;
        }
    }
}

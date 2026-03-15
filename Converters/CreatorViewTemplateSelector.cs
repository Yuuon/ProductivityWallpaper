using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace ProductivityWallpaper.Converters
{
    /// <summary>
    /// Factory-based DataTemplateSelector for CreatorView.
    /// Maps ViewModel types to their DataTemplates using explicit resource key lookup,
    /// avoiding WPF's implicit DataTemplate resolution which can fail silently.
    /// </summary>
    public class CreatorViewTemplateSelector : DataTemplateSelector
    {
        private readonly Dictionary<Type, DataTemplate?> _templateCache = new();

        public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
        {
            if (item == null) return null;

            var vmType = item.GetType();

            if (!_templateCache.TryGetValue(vmType, out var template))
            {
                // Build resource key from type name (e.g., "DesktopBackgroundViewModel" → "DesktopBackgroundTemplate")
                var resourceKey = vmType.Name.Replace("ViewModel", "Template");
                template = System.Windows.Application.Current.TryFindResource(resourceKey) as DataTemplate;

                if (template != null)
                {
                    Debug.WriteLine($"[TemplateSelector] Found template for {vmType.Name} using key: {resourceKey}");
                }
                else
                {
                    // Fallback: try implicit DataTemplate (by DataType)
                    var dataTemplateKey = new DataTemplateKey(vmType);
                    template = System.Windows.Application.Current.TryFindResource(dataTemplateKey) as DataTemplate;

                    if (template != null)
                    {
                        Debug.WriteLine($"[TemplateSelector] Found implicit template for {vmType.Name}");
                    }
                    else
                    {
                        Debug.WriteLine($"[TemplateSelector] Warning: No template found for {vmType.Name}, tried key: {resourceKey}");
                    }
                }

                _templateCache[vmType] = template;
            }

            Debug.WriteLine($"[CreatorViewTemplateSelector] SelectTemplate for {vmType.Name}: {(template != null ? "FOUND" : "NOT FOUND")}");
            return template;
        }
    }
}

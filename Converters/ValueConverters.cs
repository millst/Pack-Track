// Converters/ValueConverters.cs - Updated with better boolean handling
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Pack_Track.Models;

namespace Pack_Track.Converters
{
    public class ProductTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                TrackedProduct => "Tracked",
                InventoryProduct => "Inventory",
                _ => "Unknown"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AssetQuantityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                TrackedProduct tracked => string.IsNullOrEmpty(tracked.AssetNumber) ? "No Assets" : $"{tracked.AssetNumber.Split(',').Length} Assets",
                InventoryProduct inventory => $"Qty: {inventory.QuantityTotal}/{inventory.QuantityAvailable} Available",
                _ => ""
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                return value is bool boolValue ? !boolValue : false;
            }

            if (targetType == typeof(Visibility) || targetType == typeof(Visibility?))
            {
                bool isVisible = value is bool boolValue2 ? !boolValue2 : true;
                return isVisible ? Visibility.Visible : Visibility.Collapsed;
            }

            return value is bool boolValue3 ? !boolValue3 : false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            if (value is Visibility visibility)
                return visibility != Visibility.Visible;
            return false;
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = false;

            if (value is bool boolValue)
            {
                isVisible = boolValue;
            }
            else if (value != null)
            {
                isVisible = true; // Non-null object = visible
            }

            // Handle inverse parameter
            if (parameter?.ToString() == "Inverse" || parameter?.ToString() == "Invert")
            {
                isVisible = !isVisible;
            }

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = value is Visibility visibility && visibility == Visibility.Visible;

            // Handle inverse parameter
            if (parameter?.ToString() == "Inverse" || parameter?.ToString() == "Invert")
            {
                isVisible = !isVisible;
            }

            return isVisible;
        }
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasValue = !string.IsNullOrEmpty(value?.ToString());

            if (parameter?.ToString() == "Inverse" || parameter?.ToString() == "Invert")
                hasValue = !hasValue;

            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Visibility.Collapsed;

            // Handle different input types
            int count = 0;

            if (value is int intValue)
            {
                count = intValue;
            }
            else if (value is string stringValue)
            {
                count = string.IsNullOrEmpty(stringValue) ? 0 : 1;
            }
            else if (value is System.Collections.ICollection collection)
            {
                count = collection.Count;
            }
            else if (int.TryParse(value.ToString(), out int parsedValue))
            {
                count = parsedValue;
            }

            bool isVisible = count > 0;

            if (parameter?.ToString() == "Inverse" || parameter?.ToString() == "Invert")
                isVisible = !isVisible;

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Additional converter for product quantities
    public class InventoryStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is InventoryProduct inventory)
            {
                return $"{inventory.QuantityAvailable} of {inventory.QuantityTotal} available";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
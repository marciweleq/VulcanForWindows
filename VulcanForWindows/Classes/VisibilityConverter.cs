﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Converters;

public class VisibilityConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool swap = false;
        if (bool.TryParse( (string)parameter,out var s))
            swap = s;
        if (value is bool b)
        {
            return (swap ? !b : b) ? Visibility.Visible : Visibility.Collapsed;
        }

        // Return null if the value is not a DateTime
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        // ConvertBack is not used in this example
        throw new NotImplementedException();
    }
}


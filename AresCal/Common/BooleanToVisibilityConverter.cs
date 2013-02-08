#region header

// AresCal - BooleanToVisibilityConverter.cs
// 
// Alistair J. R. Young
// Arkane Systems
// 
// Copyright Arkane Systems 2012-2013.  All rights reserved.
// 
// Licensed and made available under MS-PL: http://opensource.org/licenses/ms-pl .
// 
// Created: 2013-02-04 2:15 PM

#endregion

using System;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace ArkaneSystems.AresCal.Common
{
    /// <summary>
    ///     Value converter that translates true to <see cref="Visibility.Visible" /> and false to
    ///     <see cref="Visibility.Collapsed" />.
    /// </summary>
    public sealed class BooleanToVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (value is bool && (bool) value) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is Visibility && (Visibility) value == Visibility.Visible;
        }

        #endregion
    }
}

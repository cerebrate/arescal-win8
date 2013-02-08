#region header

// AresCal - BooleanNegationConverter.cs
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

using Windows.UI.Xaml.Data;

namespace ArkaneSystems.AresCal.Common
{
    /// <summary>
    ///     Value converter that translates true to false and vice versa.
    /// </summary>
    public sealed class BooleanNegationConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return !(value is bool && (bool) value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return !(value is bool && (bool) value);
        }

        #endregion
    }
}

//-------------------------------------------------------------------------------------------------
// <copyright file="ObservableObject.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Abstract base class for bindable objects.
// </summary>
//-------------------------------------------------------------------------------------------------

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

public abstract class ObservableObject : INotifyPropertyChanging, INotifyPropertyChanged
{
    public event PropertyChangingEventHandler PropertyChanging;
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanging(string propertyName)
    {
        this.ValidateProperty(propertyName);

        PropertyChangingEventHandler handler = this.PropertyChanging;
        if (null != handler)
        {
            handler(this, new PropertyChangingEventArgs(propertyName));
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        this.ValidateProperty(propertyName);

        PropertyChangedEventHandler handler = this.PropertyChanged;
        if (null != handler)
        {
            handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    [Conditional("DEBUG")]
    private void ValidateProperty(string propertyName)
    {
        PropertyInfo property = this.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Debug.Assert(null != property);
    }
}

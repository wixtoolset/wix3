// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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

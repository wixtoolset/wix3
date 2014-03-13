//-------------------------------------------------------------------------------------------------
// <copyright file="RelayCommand.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Simple ICommand implementation.
// </summary>
//-------------------------------------------------------------------------------------------------

using System;
using System.Windows.Input;

public sealed class RelayCommand : ICommand
{
    private readonly Action<object> execute;
    private readonly Predicate<object> canExecute;


    public RelayCommand(Action<object> execute)
        : this(execute, null)
    {
    }

    public RelayCommand(Action<object> execute, Predicate<object> canExecute)
    {
        if (null == execute)
        {
            throw new ArgumentNullException("execute");
        }

        this.execute = execute;
        this.canExecute = canExecute;
    }

    public bool CanExecute(object parameter)
    {
        return (null == this.canExecute) || this.canExecute(parameter);
    }

    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public void Execute(object parameter)
    {
        this.execute(parameter);
    }
}

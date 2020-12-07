using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Chinook.AsyncWebView
{
	internal class WebViewCommand : ICommand
	{
		private readonly Action _execute;
		private readonly Func<bool> _canExecute;

		public WebViewCommand(Action execute, Func<bool> canExecute)
		{
			_execute = execute ?? throw new ArgumentNullException(nameof(execute));
			_canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
		}

		public event EventHandler CanExecuteChanged;

		public bool CanExecute(object parameter)
		{
			return _canExecute.Invoke();
		}

		public void Execute(object parameter)
		{
			_execute.Invoke();
		}

		public void RaiseCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}

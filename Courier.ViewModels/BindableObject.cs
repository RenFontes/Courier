using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Courier.ViewModels
{
	public class BindableObject : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public void PropertyChange(string propertyName)
		{
			VerifyProperty(propertyName);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

		[Conditional("DEBUG")]
		private void VerifyProperty(string propertyName)
		{
			var type = GetType();
			var info = type.GetProperty(propertyName);

			if (info == null)
			{
				var message = string.Format(CultureInfo.CurrentCulture, "{0} is not a public property of {1}", propertyName, type.FullName);
				//Modified this to throw an exception instead of a Debug.Fail to make it more unit test friendly
				throw new ArgumentOutOfRangeException(propertyName, message);
			}
		}

	}
}
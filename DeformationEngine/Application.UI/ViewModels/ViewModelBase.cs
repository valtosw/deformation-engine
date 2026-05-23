using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Application.UI.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        #region Events

        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Protected Logic

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            var handler = PropertyChanged;

            if (handler is not null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);

            return true;
        }

        #endregion
    }
}
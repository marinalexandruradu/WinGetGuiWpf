using System;
using System.ComponentModel;

namespace WinGetGuiWpf.ViewModels
{
    public class UpgradeStatusItem : INotifyPropertyChanged
    {
        private string _packageName;
        private string _status;
        private string _message;
        private string _fullOutput = "";

        public string PackageName
        {
            get => _packageName;
            set
            {
                if (_packageName != value)
                {
                    _packageName = value;
                    OnPropertyChanged(nameof(PackageName));
                }
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                if (_message != value)
                {
                    _message = value;
                    OnPropertyChanged(nameof(Message));
                }
            }
        }

        public string FullOutput
        {
            get => _fullOutput;
            set
            {
                if (_fullOutput != value)
                {
                    _fullOutput = value;
                    OnPropertyChanged(nameof(FullOutput));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

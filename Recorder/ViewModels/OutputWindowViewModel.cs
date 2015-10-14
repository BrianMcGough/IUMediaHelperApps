﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using JetBrains.Annotations;

namespace Recorder.ViewModels
{
    public class OutputWindowViewModel : INotifyPropertyChanged, IClosing
    {
        private readonly UserControlsViewModel _parent;
        private bool _clear;

        private string _text;
        private Visibility _visibility;


        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                OnPropertyChanged();
            }
        }

        public bool Clear
        {
            get { return _clear; }
            set
            {
                _clear = value;
                OnPropertyChanged();
            }
        }

        public Visibility Visibility
        {
            get { return _visibility; }
            set
            {
                _visibility = value;
                OnPropertyChanged();
                
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;


        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public void StartOutput(string header)
        {
            Visibility = Visibility.Visible;

            if (Clear)
            {
                Text = string.Empty;
            }

            Text += $"{header}\n\n";
        }

        public bool CancelWindowClose()
        {
            Visibility = Visibility.Hidden;
            return true;
        }
    }
}
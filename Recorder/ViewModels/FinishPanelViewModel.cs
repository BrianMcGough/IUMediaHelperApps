﻿using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Common.UserInterface.Commands;
using Recorder.Models;

namespace Recorder.ViewModels
{
    public class FinishPanelViewModel : AbstractPanelViewModel
    {
        private ICommand _backCommand;
        private ICommand _combineCommand;
        private string _commandLabelText;

        public FinishPanelViewModel(UserControlsViewModel parent, ObjectModel objectModel) : base(parent, objectModel)
        {
            ActionButton = new ActionButtonModel
            {
                ButtonCaption = "3",
                LabelCaption = "Combine",
                Scale = 1,
                ButtonCommand = new RelayCommand(async param => await parent.ShowPanel<FinishPanelViewModel>())
            };
            Combiner.PropertyChanged += CombinerPropertyChangedHandler;
            CommandLabelText = "Combine Parts";
        }

        public string CommandLabelText
        {
            get { return _commandLabelText; }
            set
            {
                _commandLabelText = value;
                OnPropertyChanged();
            }
        }

        public override bool IsEnabled => GetEnabledState();
        public override string BackButtonText => "Continue recording";
        public override string NextButtonText => "";

        public override string Instructions
            => "Click Combine Parts to generate your file and move it to the output folder";

        public override ICommand NextCommand => null;

        public override ICommand BackCommand
        {
            get
            {
                return _backCommand
                       ??
                       (_backCommand = new RelayCommand(async param => await Parent.ShowPanel<RecordPanelViewModel>()));
            }
        }

        public ICommand CombineCommand
        {
            get
            {
                return _combineCommand ?? (_combineCommand = new AsyncRelayCommand(
                    async param => await DoCombine(), 
                    param => Combiner.OkToProcess().IsValid));
            }
        }

        private void CombinerPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            CommandLabelText = Combiner.Combining ? "Combining Parts" : "Combine Parts";
            OnPropertyChanged(nameof(CombineCommand));
            OnPropertyChanged(nameof(IsEnabled));
        }

#pragma warning disable 1998
        public override async Task Initialize()
#pragma warning restore 1998
        {
        }

        private async Task DoCombine()
        {
            OnPropertyChanged(nameof(IsEnabled));
            OnPropertyChanged(nameof(CombineCommand));
            await Combiner.Combine();
        }

        private bool GetEnabledState()
        {
            if (ObjectModel.PartsPresent() == false)
            {
                return false;
            }

            if (Recorder.Recording)
            {
                return false;
            }

            if (Combiner.Combining)
            {
                return false;
            }

            return true;
        }
    }
}
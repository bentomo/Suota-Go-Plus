﻿using Prism.Commands;
using Prism.Events;
using suota_pgp.Model;
using suota_pgp.Services;
using System.Collections.ObjectModel;

namespace suota_pgp
{
    public class DeviceInfoViewModel : ViewModelBase
    {
        private IEventAggregator _aggregator;
        private IBleManager _bleManager;
        private IFileManager _fileService;

        private AppState _appState;
        public AppState AppState
        {
            get => _appState;
            private set
            {
                if (SetProperty(ref _appState, value))
                {
                    GetDeviceInfoCommand.RaiseCanExecuteChanged();
                    RestoreCommand.RaiseCanExecuteChanged();
                    SaveCommand.RaiseCanExecuteChanged();
                    ScanCommand.RaiseCanExecuteChanged();
                    StopScanCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private ErrorState _errorState;
        public ErrorState ErrorState
        {
            get => _errorState;
            set
            {
                if (SetProperty(ref _errorState, value))
                {
                    GetDeviceInfoCommand.RaiseCanExecuteChanged();
                    RestoreCommand.RaiseCanExecuteChanged();
                    SaveCommand.RaiseCanExecuteChanged();
                    ScanCommand.RaiseCanExecuteChanged();
                    StopScanCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<GoPlus> Devices { get; private set; }

        private GoPlus _selectedDevice;
        public GoPlus SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (SetProperty(ref _selectedDevice, value))
                {
                    GetDeviceInfoCommand.RaiseCanExecuteChanged();
                    RestoreCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public DelegateCommand GetDeviceInfoCommand { get; private set; }

        public DelegateCommand SaveCommand { get; private set; }

        public DelegateCommand ScanCommand { get; private set; }

        public DelegateCommand StopScanCommand { get; private set; }

        public DelegateCommand RestoreCommand { get; private set; }

        public DeviceInfoViewModel(IEventAggregator aggregator,
                                   IBleManager bleService, 
                                   IFileManager fileService,
                                   IStateManager stateManager)
        {
            _aggregator = aggregator;
            _bleManager = bleService;
            _fileService = fileService;

            Devices = new ObservableCollection<GoPlus>();

            GetDeviceInfoCommand = new DelegateCommand(GetDeviceInfo, GetDeviceInfoCanExecute);
            RestoreCommand = new DelegateCommand(Restore, RestoreCanExecute);
            SaveCommand = new DelegateCommand(Save, SaveCanExecute);
            ScanCommand = new DelegateCommand(Scan, ScanCanExecute);
            StopScanCommand = new DelegateCommand(StopScan, StopScanCanExecute);

            AppState = stateManager.State;
            ErrorState = stateManager.ErrorState;

            _aggregator.GetEvent<PrismEvents.AppStateChangedEvent>().Subscribe(OnAppStateChanged, ThreadOption.UIThread);
            _aggregator.GetEvent<PrismEvents.GoPlusFoundEvent>().Subscribe(OnGoPlusFound, ThreadOption.UIThread);
            _aggregator.GetEvent<PrismEvents.ErrorStateChangedEvent>().Subscribe(OnErrorStateChanged, ThreadOption.UIThread);
        }

        protected void Clear()
        {
            SelectedDevice = null;
            Devices.Clear();
        }

        private async void GetDeviceInfo()
        {
            await _bleManager.GetDeviceInfo(SelectedDevice);
            SaveCommand.RaiseCanExecuteChanged();
        }

        private bool GetDeviceInfoCanExecute()
        {
            return AppState == AppState.Idle && 
                   !ErrorState.HasFlag(ErrorState.BluetoothDisabled) &&
                   !ErrorState.HasFlag(ErrorState.LocationUnauthorized) &&
                   SelectedDevice != null;
        }

        private void Save()
        {
            _fileService.Save(SelectedDevice);
        }

        private bool SaveCanExecute()
        {
            return AppState == AppState.Idle &&
                   !ErrorState.HasFlag(ErrorState.StorageUnauthorized) &&
                   SelectedDevice != null &&
                   SelectedDevice.IsComplete;
        }

        private void Scan()
        {
            Clear();
            _bleManager.Scan();
        }

        private bool ScanCanExecute()
        {
            return AppState == AppState.Idle &&
                   !ErrorState.HasFlag(ErrorState.BluetoothDisabled) &&
                   !ErrorState.HasFlag(ErrorState.LocationUnauthorized);
        }

        private void StopScan()
        {
            _bleManager.StopScan();
        }

        private bool StopScanCanExecute()
        {
            return AppState == AppState.Scanning &&
                   !ErrorState.HasFlag(ErrorState.BluetoothDisabled) &&
                   !ErrorState.HasFlag(ErrorState.LocationUnauthorized);
        }

        private void Restore()
        {
            _bleManager.RestoreDevice(SelectedDevice);
        }

        private bool RestoreCanExecute()
        {
            return AppState == AppState.Idle &&
                   !ErrorState.HasFlag(ErrorState.BluetoothDisabled) &&
                   !ErrorState.HasFlag(ErrorState.LocationUnauthorized) &&
                   SelectedDevice != null;
        }

        #region Events

        private void OnAppStateChanged(AppState state)
        {
            AppState = state;
        }

        private void OnErrorStateChanged(ErrorState state)
        {
            ErrorState = state;
        }

        private void OnGoPlusFound(GoPlus pgp)
        {
            if (_isViewActive && pgp != null)
            {
                Devices.Add(pgp);
            }
        }

        #endregion
    }
}

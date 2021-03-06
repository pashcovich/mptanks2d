﻿using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Input;
using EmptyKeys.UserInterface.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPTanks.Client.Backend.UI.Binders
{
    public class ConnectingToServerPage : BinderBase
    {
        #region Stuff that is there for the UI Layer and not the user
        
        private string _addrLabel;
        public string AddressLabel
        {
            get
            {
                return _addrLabel;
            }
            private set
            {
                SetProperty(ref _addrLabel, value, nameof(AddressLabel));
            }
        }
        
        private Visibility _failureAreaVisibility;
        public Visibility FailureAreaVisibility
        {
            get
            {
                return _failureAreaVisibility;
            }
            set
            {
                SetProperty(ref _failureAreaVisibility, value, nameof(FailureAreaVisibility));
            }
        }

        private Visibility _cancelAreaVisibility;
        public Visibility CancelAreaVisibility
        {
            get
            {
                return _cancelAreaVisibility;
            }
            set
            {
                SetProperty(ref _cancelAreaVisibility, value, nameof(CancelAreaVisibility));
            }
        }
        #endregion

        private bool _connectFailed;
        /// <summary>
        /// Whether the connection has failed
        /// </summary>
        public bool HasConnectionFailed
        {
            get
            {
                return _connectFailed;
            }
            set
            {
                _connectFailed = value;
                if (_connectFailed)
                {
                    FailureAreaVisibility = Visibility.Visible;
                    CancelAreaVisibility = Visibility.Collapsed;
                }
                else
                {
                    FailureAreaVisibility = Visibility.Collapsed;
                    CancelAreaVisibility = Visibility.Visible;
                }
            }
        }

        private string _failureReason = "null";
        /// <summary>
        /// The reason that the connection failed, if it did.
        /// </summary>
        public string FailureReason
        {
            get
            {
                return _failureReason;
            }
            set
            {
                SetProperty(ref _failureReason, value, nameof(FailureReason));
            }
        }

        private string _connAddr = "null";
        /// <summary>
        /// The connection address
        /// </summary>
        public string ConnectionAddress
        {
            get
            {
                return _connAddr;
            }
            set
            {
                _connAddr = value;
                AddressLabel =
                    Strings.ClientMenus.ConnectingToServerAddress +
                    " " + ConnectionAddress + ":" + Port;
            }
        }

        private int _port;
        /// <summary>
        /// The port that is being connected to
        /// </summary>
        public int Port
        {
            get { return _port; }
            set
            {
                _port = value;
                AddressLabel =
                    Strings.ClientMenus.ConnectingToServerAddress +
                    " " + ConnectionAddress + ":" + Port;
            }
        }

        #region Events
        public event EventHandler OnCancelPressed = delegate { };
        public event EventHandler OnReturnToMenuPressed = delegate { };

        private ICommand _cancelCommand;
        public ICommand CancelButtonCommand
        {
            get
            {
                return _cancelCommand;
            }
            set
            {
                SetProperty(ref _cancelCommand, value, nameof(CancelButtonCommand));
            }
        }

        private ICommand _returnCommand;
        public ICommand ReturnButtonCommand
        {
            get
            {
                return _returnCommand;
            }
            set
            {
                SetProperty(ref _returnCommand, value, nameof(ReturnButtonCommand));
            }
        }
        #endregion

        private EventArgs _args = new EventArgs();
        public ConnectingToServerPage()
        {
            HasConnectionFailed = false;
            CancelButtonCommand = new RelayCommand((obj) => OnCancelPressed(obj, _args));
            ReturnButtonCommand = new RelayCommand((obj) => OnReturnToMenuPressed(obj, _args));
        }
    }
}

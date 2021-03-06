﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MS-PL license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.Logging;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;

namespace Playground.Core.ViewModels
{
    public class SplitMasterViewModel : MvxNavigationViewModel
    {
        public SplitMasterViewModel(IMvxLogProvider logProvider, IMvxNavigationService navigationService) : base(logProvider, navigationService)
        {
            OpenDetailCommand = new MvxAsyncCommand(async () => await NavigationService.Navigate<SplitDetailViewModel>().ConfigureAwait(false));

            OpenDetailNavCommand = new MvxAsyncCommand(async () => await NavigationService.Navigate<SplitDetailNavViewModel>().ConfigureAwait(false));

            ShowRootViewModel = new MvxAsyncCommand(async () => await NavigationService.Navigate<RootViewModel>().ConfigureAwait(false));
        }

        public string PaneText => "Text for the Master Pane";

        public IMvxAsyncCommand OpenDetailCommand { get; private set; }

        public IMvxAsyncCommand OpenDetailNavCommand { get; private set; }

        public IMvxAsyncCommand ShowRootViewModel { get; private set; }

        public override ValueTask ViewAppeared()
        {
            return base.ViewAppeared();
        }
    }
}

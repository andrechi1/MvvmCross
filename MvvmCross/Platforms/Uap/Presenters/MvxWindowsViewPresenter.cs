﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MS-PL license.
// See the LICENSE file in the project root for more information.

using System;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using MvvmCross.Exceptions;
using MvvmCross.Logging;
using MvvmCross.Navigation;
using MvvmCross.Platforms.Uap.Presenters.Attributes;
using MvvmCross.Platforms.Uap.Views;
using MvvmCross.ViewModels;
using MvvmCross.Presenters;
using MvvmCross.Views;
using MvvmCross.Presenters.Attributes;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml;
using System.Linq;

namespace MvvmCross.Platforms.Uap.Presenters
{
    public class MvxWindowsViewPresenter
        : MvxAttributeViewPresenter, IMvxWindowsViewPresenter
    {
        protected readonly IMvxWindowsFrame _rootFrame;

        public MvxWindowsViewPresenter(IMvxWindowsFrame rootFrame)
        {
            _rootFrame = rootFrame;

            SystemNavigationManager.GetForCurrentView().BackRequested += BackButtonOnBackRequested;
        }

        private IMvxViewModelLoader _viewModelLoader;
        public IMvxViewModelLoader ViewModelLoader
        {
            get
            {
                if (_viewModelLoader == null)
                    _viewModelLoader = Mvx.IoCProvider.Resolve<IMvxViewModelLoader>();
                return _viewModelLoader;
            }
            set
            {
                _viewModelLoader = value;
            }
        }

        public override void RegisterAttributeTypes()
        {
            AttributeTypesToActionsDictionary.Register<MvxPagePresentationAttribute>(ShowPage, ClosePage);
            AttributeTypesToActionsDictionary.Register<MvxSplitViewPresentationAttribute>(ShowSplitView, CloseSplitView);
            AttributeTypesToActionsDictionary.Register<MvxRegionPresentationAttribute>(ShowRegionView, CloseRegionView);
            AttributeTypesToActionsDictionary.Register<MvxDialogViewPresentationAttribute>(ShowDialog, CloseDialog);
        }

        public override MvxBasePresentationAttribute CreatePresentationAttribute(Type viewModelType, Type viewType)
        {
            MvxLog.Instance.Trace($"PresentationAttribute not found for {viewType.Name}. Assuming new page presentation");
            return new MvxPagePresentationAttribute() { ViewType = viewType, ViewModelType = viewModelType };
        }

        protected virtual async void BackButtonOnBackRequested(object sender, BackRequestedEventArgs backRequestedEventArgs)
        {
            if (backRequestedEventArgs.Handled)
                return;

            var currentView = _rootFrame.Content as IMvxView;
            if (currentView == null)
            {
                MvxLog.Instance.Warn("Ignoring close for viewmodel - rootframe has no current page");
                return;
            }

            var navigationService = Mvx.IoCProvider.Resolve<IMvxNavigationService>();

            backRequestedEventArgs.Handled = await navigationService.Close(currentView.ViewModel).ConfigureAwait(false);
        }

        protected virtual string GetRequestText(MvxViewModelRequest request)
        {
            var requestTranslator = Mvx.IoCProvider.Resolve<IMvxWindowsViewModelRequestTranslator>();
            string requestText = string.Empty;
            if (request is MvxViewModelInstanceRequest)
            {
                requestText = requestTranslator.GetRequestTextWithKeyFor(((MvxViewModelInstanceRequest)request).ViewModelInstance);
            }
            else
            {
                requestText = requestTranslator.GetRequestTextFor(request);
            }

            return requestText;
        }

        protected virtual void HandleBackButtonVisibility()
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                _rootFrame.CanGoBack ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
        }

        protected virtual Task<bool> ShowSplitView(Type viewType, MvxSplitViewPresentationAttribute attribute, MvxViewModelRequest request)
        {
            var viewsContainer = Mvx.IoCProvider.Resolve<IMvxViewsContainer>();

            if (_rootFrame.Content is MvxWindowsPage currentPage)
            {
                var splitView = currentPage.Content.FindControl<SplitView>();
                if (splitView == null)
                {
                    return Task.FromResult(true);
                }

                if (attribute.Position == SplitPanePosition.Content)
                {
                    var nestedFrame = splitView.Content as Frame;
                    if (nestedFrame == null)
                    {
                        nestedFrame = new Frame();
                        splitView.Content = nestedFrame;
                    }
                    var requestText = GetRequestText(request);
                    nestedFrame.Navigate(viewType, requestText);
                }
                else if (attribute.Position == SplitPanePosition.Pane)
                {
                    var nestedFrame = splitView.Pane as Frame;
                    if (nestedFrame == null)
                    {
                        nestedFrame = new Frame();
                        splitView.Pane = nestedFrame;
                    }
                    var requestText = GetRequestText(request);
                    nestedFrame.Navigate(viewType, requestText);
                }
            }
            return Task.FromResult(true);
        }

        protected virtual Task<bool> CloseSplitView(IMvxViewModel viewModel, MvxSplitViewPresentationAttribute attribute)
        {
            return ClosePage(viewModel, attribute);
        }

        protected virtual Task<bool> ShowRegionView(Type viewType, MvxRegionPresentationAttribute attribute, MvxViewModelRequest request)
        {
            if (viewType.HasRegionAttribute())
            {
                var requestText = GetRequestText(request);

                var containerView = _rootFrame.UnderlyingControl.FindControl<Frame>(viewType.GetRegionName());

                if (containerView != null)
                {
                    containerView.Navigate(viewType, requestText);
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(true);
        }

        protected virtual Task<bool> CloseRegionView(IMvxViewModel viewModel, MvxRegionPresentationAttribute attribute)
        {
            var viewFinder = Mvx.IoCProvider.Resolve<IMvxViewsContainer>();
            var viewType = viewFinder.GetViewType(viewModel.GetType());
            if (viewType.HasRegionAttribute())
            {
                var containerView = _rootFrame.UnderlyingControl?.FindControl<Frame>(viewType.GetRegionName());

                if (containerView == null)
                    throw new MvxException($"Region '{viewType.GetRegionName()}' not found in view '{viewType}'");

                if (containerView.CanGoBack)
                {
                    containerView.GoBack();
                    return Task.FromResult(true);
                }
            }

            return ClosePage(viewModel, attribute);
        }

        protected virtual Task<bool> ClosePage(IMvxViewModel viewModel, MvxBasePresentationAttribute attribute)
        {
            var currentView = _rootFrame.Content as IMvxView;
            if (currentView == null)
            {
                MvxLog.Instance.Warn("Ignoring close for viewmodel - rootframe has no current page");
                return Task.FromResult(false);
            }

            if (currentView.ViewModel != viewModel)
            {
                MvxLog.Instance.Warn("Ignoring close for viewmodel - rootframe's current page is not the view for the requested viewmodel");
                return Task.FromResult(false);
            }

            if (!_rootFrame.CanGoBack)
            {
                MvxLog.Instance.Warn("Ignoring close for viewmodel - rootframe refuses to go back");
                return Task.FromResult(false);
            }

            _rootFrame.GoBack();

            HandleBackButtonVisibility();

            return Task.FromResult(true);
        }

        protected virtual Task<bool> ShowPage(Type viewType, MvxBasePresentationAttribute attribute, MvxViewModelRequest request)
        {
            try
            {
                var requestText = GetRequestText(request);
                var viewsContainer = Mvx.IoCProvider.Resolve<IMvxViewsContainer>();

                _rootFrame.Navigate(viewType, requestText); //Frame won't allow serialization of it's nav-state if it gets a non-simple type as a nav param

                HandleBackButtonVisibility();
                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                MvxLog.Instance.Trace("Error seen during navigation request to {0} - error {1}", request.ViewModelType.Name,
                    exception.ToLongString());
                return Task.FromResult(false);
            }
        }

        protected virtual async Task<bool> ShowDialog(Type viewType, MvxDialogViewPresentationAttribute attribute, MvxViewModelRequest request)
        {
            try
            {
                var contentDialog = (ContentDialog)CreateControl(viewType, request, attribute);
                if (contentDialog != null)
                {
                    await contentDialog.ShowAsync(attribute.Placement);
                    return true;
                }

                return false;
            }
            catch (Exception exception)
            {
                MvxLog.Instance.Trace("Error seen during navigation request to {0} - error {1}", request.ViewModelType.Name,
                    exception.ToLongString());
                return false;
            }
        }

        public virtual Control CreateControl(Type viewType, MvxViewModelRequest request, MvxBasePresentationAttribute attribute)
        {
            try
            {
                var control = Activator.CreateInstance(viewType) as Control;
                if (control is IMvxView mvxControl)
                {
                    if (request is MvxViewModelInstanceRequest instanceRequest)
                        mvxControl.ViewModel = instanceRequest.ViewModelInstance;
                    else
                        mvxControl.ViewModel = ViewModelLoader.LoadViewModel(request, null);
                }

                return control;
            }
            catch (Exception ex)
            {
                throw new MvxException(ex, $"Cannot create Control '{viewType.FullName}'. Are you use the wrong base class?");
            }
        }

        protected virtual Task<bool> CloseDialog(IMvxViewModel viewModel, MvxBasePresentationAttribute attribute)
        {
            var popups = VisualTreeHelper.GetOpenPopups(Window.Current).FirstOrDefault(p =>
            {
                if (attribute.ViewType.IsAssignableFrom(p.Child.GetType())
                    && p.Child is IMvxWindowsContentDialog dialog)
                {
                    return dialog.ViewModel == viewModel;
                }
                return false;
            });

            (popups?.Child as ContentDialog)?.Hide();

            return Task.FromResult(true);
        }
    }
}

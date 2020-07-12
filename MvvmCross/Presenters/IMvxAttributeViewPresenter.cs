﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MS-PL license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MvvmCross.Presenters.Attributes;
using MvvmCross.ViewModels;
using MvvmCross.Views;

namespace MvvmCross.Presenters
{
    public interface IMvxAttributeViewPresenter : IMvxViewPresenter
    {
        IMvxViewModelTypeFinder ViewModelTypeFinder { get; set; }
        IMvxViewsContainer ViewsContainer { get; set; }
        IDictionary<Type, MvxPresentationAttributeAction> AttributeTypesToActionsDictionary { get; }
        void RegisterAttributeTypes();

        //TODO: Maybe move those to helper class
        ValueTask<MvxBasePresentationAttribute?> GetPresentationAttribute(MvxViewModelRequest request);
        ValueTask<MvxBasePresentationAttribute?> CreatePresentationAttribute(Type viewModelType, Type viewType);
        ValueTask<MvxBasePresentationAttribute?> GetOverridePresentationAttribute(MvxViewModelRequest request, Type viewType);
    }
}

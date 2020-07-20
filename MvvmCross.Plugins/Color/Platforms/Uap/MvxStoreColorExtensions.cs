﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MS-PL license.
// See the LICENSE file in the project root for more information.

namespace MvvmCross.Plugin.Color.Platforms.Uap
{
    public static class MvxStoreColorExtensions
    {
        public static Microsoft.UI.Color ToNativeColor(this System.Drawing.Color color)
        {
            var windowsColor = Microsoft.UI.Color.FromArgb(color.A, color.R, color.G, color.B);
            return windowsColor;
        }
    }
}

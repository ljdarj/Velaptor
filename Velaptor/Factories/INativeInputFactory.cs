﻿// <copyright file="INativeInputFactory.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

using Silk.NET.Input;

namespace Velaptor.Factories;

/// <summary>
/// Creates an input object that gives access to native input hardware.
/// </summary>
internal interface INativeInputFactory
{
    /// <summary>
    /// Creates a new input object.
    /// </summary>
    /// <returns>The input object to gain access to the native input hardware.</returns>
    IInputContext CreateInput();
}

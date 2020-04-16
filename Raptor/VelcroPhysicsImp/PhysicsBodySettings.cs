﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Raptor.VelcroPhysicsImp
{
    /// <summary>
    /// Holds settings for a <see cref="VelcroBody"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PhysicsBodySettings
    {
        #region Props
        /// <summary>
        /// The X vertices of the body's shape.
        /// </summary>
        public ReadOnlyCollection<float> XVertices { get; private set; } = new ReadOnlyCollection<float>(Array.Empty<float>());

        /// <summary>
        /// The X vertices of the body's shape.
        /// </summary>
        public ReadOnlyCollection<float> YVertices { get; private set; } = new ReadOnlyCollection<float>(Array.Empty<float>());

        /// <summary>
        /// The X coordinate of the body's location.
        /// </summary>
        public float XPosition { get; set; }

        /// <summary>
        /// The Y coordinate of the body's location.
        /// </summary>
        public float YPosition { get; set; }

        /// <summary>
        /// Gets or sets the angle of the body in degrees.
        /// </summary>
        public float Angle { get; set; }

        /// <summary>
        /// Gets or sets the density of the body.
        /// </summary>
        public float Density { get; set; }

        /// <summary>
        /// Gets or sets the friction of the body.
        /// </summary>
        public float Friction { get; set; }

        /// <summary>
        /// Gets or sets the restitution(bounciness) of the body.
        /// </summary>
        public float Restitution { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the body is static and will not move.
        /// </summary>
        public bool IsStatic { get; set; }
        #endregion


        #region Public Methods
        public void SetXVertices(float[] xVertices) => XVertices = new ReadOnlyCollection<float>(xVertices);


        public void SetYVertices(float[] yVertices) => YVertices = new ReadOnlyCollection<float>(yVertices);
        #endregion
    }
}

﻿// <copyright file="GLFWMonitorsTests.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace RaptorTests.OpenGL
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using Moq;
    using OpenTK.Windowing.GraphicsLibraryFramework;
    using Raptor;
    using Raptor.Hardware;
    using Raptor.OpenGL;
    using Xunit;

    /// <summary>
    /// Tests the <see cref="GLFWMonitors"/> class.
    /// </summary>
    public unsafe class GLFWMonitorsTests : IDisposable
    {
        private readonly Mock<IGLFWInvoker> mockGLFWInvoker;
        private readonly Mock<IPlatform> mockPlatform;
        private readonly VideoMode* videoModeHandleA;
        private readonly VideoMode* videoModeHandleB;
        private readonly Monitor monitorA;
        private readonly Monitor monitorB;
        private VideoMode videoModeA;
        private VideoMode videoModeB;
        private IntPtr monitorHandleA;
        private IntPtr monitorHandleB;

        /// <summary>
        /// Initializes a new instance of the <see cref="GLFWMonitorsTests"/> class.
        /// </summary>
        public unsafe GLFWMonitorsTests()
        {
            this.mockPlatform = new Mock<IPlatform>();
            this.mockPlatform.SetupGet(p => p.CurrentPlatform).Returns(OSPlatform.Windows);

            this.videoModeA = new VideoMode()
            {
                Width = 1,
                Height = 2,
                RedBits = 3,
                GreenBits = 4,
                BlueBits = 5,
                RefreshRate = 6,
            };

            this.videoModeB = new VideoMode()
            {
                Width = 11,
                Height = 22,
                RedBits = 33,
                GreenBits = 44,
                BlueBits = 55,
                RefreshRate = 66,
            };

            fixed (VideoMode* pVideoModeA = &this.videoModeA)
            {
                this.videoModeHandleA = pVideoModeA;
            }

            fixed (VideoMode* pVideoModeB = &this.videoModeB)
            {
                this.videoModeHandleB = pVideoModeB;
            }

            this.monitorA = default;
            this.monitorB = default;

            fixed (Monitor* pMonitorA = &this.monitorA)
            {
                this.monitorHandleA = (IntPtr)pMonitorA;
            }

            fixed (Monitor* pMonitorB = &this.monitorB)
            {
                this.monitorHandleB = (IntPtr)pMonitorB;
            }

            this.mockGLFWInvoker = new Mock<IGLFWInvoker>();
            this.mockGLFWInvoker.Setup(m => m.GetMonitors()).Returns(() =>
            {
                return new[] { this.monitorHandleA, this.monitorHandleB };
            });

            this.mockGLFWInvoker.Setup(m => m.GetVideoMode(this.monitorHandleA)).Returns((IntPtr)this.videoModeHandleA);
            this.mockGLFWInvoker.Setup(m => m.GetVideoMode(this.monitorHandleB)).Returns((IntPtr)this.videoModeHandleB);

            this.mockGLFWInvoker.Setup(m => m.GetMonitorContentScale(this.monitorHandleA))
                .Returns(new Vector2(7, 8));

            this.mockGLFWInvoker.Setup(m => m.GetMonitorContentScale(this.monitorHandleB))
                .Returns(new Vector2(77, 88));

            var pointers = new List<IntPtr>()
            {
                this.monitorHandleA,
                this.monitorHandleB,
                (IntPtr)this.videoModeHandleA,
                (IntPtr)this.videoModeHandleB,
            };

            GC.KeepAlive(this.monitorA);
            GC.KeepAlive(this.monitorB);
            GC.KeepAlive(this.videoModeA);
            GC.KeepAlive(this.videoModeB);
            GC.KeepAlive(this.monitorHandleA);
            GC.KeepAlive(this.monitorHandleB);
        }

        #region Constructor Tests
        [Fact]
        public void Ctor_WhenInvoked_InitializesGLFW()
        {
            // Act
            var monitors = new GLFWMonitors(this.mockGLFWInvoker.Object, this.mockPlatform.Object);

            // Assert
            this.mockGLFWInvoker.Verify(m => m.Init(), Times.Once());
        }

        [Fact]
        public void Ctor_WhenInvoked_SetsMonitorCallback()
        {
            // Act
            var monitors = new GLFWMonitors(this.mockGLFWInvoker.Object, this.mockPlatform.Object);

            // Assert
            this.mockGLFWInvoker.Verify(m => m.SetMonitorCallback(It.IsAny<GLFWCallbacks.MonitorCallback>()), Times.Once());
        }

        [Fact]
        public void Ctor_WhenInvoked_SystemMonitorsRefreshed()
        {
            // Arrange
            var expectedMonitorA = new SystemMonitor(this.mockPlatform.Object)
            {
                IsMain = true,
                Width = 1,
                Height = 2,
                RedBitDepth = 3,
                GreenBitDepth = 4,
                BlueBitDepth = 5,
                RefreshRate = 6,
                HorizontalScale = 7,
                VerticalScale = 8,
            };

            var expectedMonitorB = new SystemMonitor(this.mockPlatform.Object)
            {
                IsMain = false,
                Width = 11,
                Height = 22,
                RedBitDepth = 33,
                GreenBitDepth = 44,
                BlueBitDepth = 55,
                RefreshRate = 66,
                HorizontalScale = 77,
                VerticalScale = 88,
            };

            // Act
            var monitors = new GLFWMonitors(this.mockGLFWInvoker.Object, this.mockPlatform.Object);
            var actual = monitors.SystemMonitors;

            // Assert
            Assert.Equal(expectedMonitorA, actual[0]);
            Assert.Equal(expectedMonitorB, actual[1]);
        }

        public void Dispose()
        {
            this.monitorHandleA = IntPtr.Zero;
            this.monitorHandleB = IntPtr.Zero;

            GC.Collect();
        }
        #endregion
    }
}

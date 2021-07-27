﻿// <copyright file="Sound.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace Velaptor.Audio
{
    using System;
#if DEBUG
    using System.Diagnostics;
#endif
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using OpenTK.Audio.OpenAL;
    using Velaptor.Factories;
    using Velaptor.NativeInterop.OpenAL;
    using IOPath = System.IO.Path;
    using TKALFormat = OpenTK.Audio.OpenAL.ALFormat;
    using VelaptorALSourcei = NativeInterop.OpenAL.ALSourcei;
    using VelaptorALSourceb = NativeInterop.OpenAL.ALSourceb;
    using VelaptorALSourcef = NativeInterop.OpenAL.ALSourcef;

    /// <summary>
    /// A single sound that can be played, paused etc.
    /// </summary>
    public class Sound : ISound
    {
        private const string IsDisposedExceptionMessage = "The sound is disposed.  You must create another sound instance.";

        // NOTE: This warning is ignored due to the implementation of the IAudioManager being a singleton.
        // This AudioManager implementation as a singleton is being managed by the IoC container class.
        // Disposing of the audio manager when any sound is disposed would cause issues with how the
        // audio manager implementation is suppose to behave.
        private readonly IAudioDeviceManager audioManager;
        private readonly ISoundDecoder<float> oggDecoder;
        private readonly ISoundDecoder<byte> mp3Decoder;
        private readonly IALInvoker alInvoker;
        private uint srcId;
        private uint bufferId;
        private float totalSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="Sound"/> class.
        /// </summary>
        /// <param name="filePath">The path to the sound file..</param>
        [ExcludeFromCodeCoverage]
        public Sound(string filePath)
        {
            Path = filePath;

            this.alInvoker = new OpenTKALInvoker();

            this.alInvoker.ErrorCallback += ErrorCallback;

            this.oggDecoder = IoC.Container.GetInstance<ISoundDecoder<float>>();
            this.mp3Decoder = IoC.Container.GetInstance<ISoundDecoder<byte>>();

            this.audioManager = AudioDeviceManagerFactory.CreateDeviceManager();
            this.audioManager.DeviceChanged += AudioManager_DeviceChanged;
            Init();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sound"/> class.
        /// </summary>
        /// <param name="filePath">The path to the sound file.</param>
        /// <param name="alInvoker">Provides access to OpenAL.</param>
        /// <param name="audioManager">Manages audio related operations.</param>
        /// <param name="oggDecoder">Decodes OGG audio files.</param>
        /// <param name="mp3Decoder">Decodes MP3 audio files.</param>
        /// <param name="soundPathResolver">Resolves paths to sound content.</param>
        internal Sound(string filePath, IALInvoker alInvoker, IAudioDeviceManager audioManager, ISoundDecoder<float> oggDecoder, ISoundDecoder<byte> mp3Decoder)
        {
            Path = filePath;

            this.alInvoker = alInvoker;
            this.alInvoker.ErrorCallback += ErrorCallback;

            this.oggDecoder = oggDecoder;
            this.mp3Decoder = mp3Decoder;

            this.audioManager = audioManager;
            this.audioManager.DeviceChanged += AudioManager_DeviceChanged;

            Init();
        }

        /// <inheritdoc/>
        public string Name => IOPath.GetFileNameWithoutExtension(Path);

        /// <inheritdoc/>
        public string Path { get; private set; }

        /// <inheritdoc/>
        public float Volume
        {
            get
            {
                if (Unloaded)
                {
                    throw new Exception(IsDisposedExceptionMessage);
                }

                // Get the current volume between 0.0 and 1.0
                var volume = this.alInvoker.GetSource(this.srcId, VelaptorALSourcef.Gain);

                // Change the range to be between 0 and 100
                return volume * 100f;
            }
            set
            {
                if (Unloaded)
                {
                    throw new Exception(IsDisposedExceptionMessage);
                }

                // Make sure that the incoming value stays between 0 and 100
                value = value > 100f ? 100f : value;
                value = value < 0f ? 0f : value;

                // Convert the value to be between 0 and 1.
                // This is the excepted range by OpenAL
                value /= 100f;

                this.alInvoker.Source(this.srcId, VelaptorALSourcef.Gain, (float)Math.Round(value, 4));
            }
        }

        /// <inheritdoc/>
        public float TimePositionMilliseconds => TimePositionSeconds * 1000f;

        /// <inheritdoc/>
        public float TimePositionSeconds
        {
            get
            {
                if (Unloaded)
                {
                    throw new Exception(IsDisposedExceptionMessage);
                }

                return this.alInvoker.GetSource(this.srcId, VelaptorALSourcef.SecOffset);
            }
        }

        /// <inheritdoc/>
        public float TimePositionMinutes => TimePositionSeconds / 60f;

        /// <inheritdoc/>
        public TimeSpan TimePosition
        {
            get
            {
                var seconds = TimePositionSeconds;

                return new TimeSpan(0, 0, (int)seconds);
            }
        }

        /// <inheritdoc/>
        public bool IsLooping
        {
            get
            {
                if (Unloaded)
                {
                    throw new Exception(IsDisposedExceptionMessage);
                }

                return this.alInvoker.GetSource(this.srcId, VelaptorALSourceb.Looping);
            }
            set
            {
                if (Unloaded)
                {
                    throw new Exception(IsDisposedExceptionMessage);
                }

                this.alInvoker.Source(this.srcId, VelaptorALSourceb.Looping, value);
            }
        }

        /// <inheritdoc/>
        public bool Unloaded { get; private set; }

        /// <inheritdoc/>
        public void PlaySound()
        {
            if (Unloaded)
            {
                throw new Exception(IsDisposedExceptionMessage);
            }

            this.alInvoker.SourcePlay(this.srcId);
        }

        /// <inheritdoc/>
        public void PauseSound()
        {
            if (Unloaded)
            {
                throw new Exception(IsDisposedExceptionMessage);
            }

            this.alInvoker.SourcePause(this.srcId);
        }

        /// <inheritdoc/>
        public void StopSound()
        {
            if (Unloaded)
            {
                throw new Exception(IsDisposedExceptionMessage);
            }

            this.alInvoker.SourceStop(this.srcId);
        }

        /// <inheritdoc/>
        public void Reset()
        {
            if (Unloaded)
            {
                throw new Exception(IsDisposedExceptionMessage);
            }

            this.alInvoker.SourceRewind(this.srcId);
        }

        /// <inheritdoc/>
        public void SetTimePosition(float seconds)
        {
            if (Unloaded)
            {
                throw new Exception(IsDisposedExceptionMessage);
            }

            // Prevent negative numbers
            seconds = seconds < 0f ? 0.0f : seconds;

            seconds = seconds > this.totalSeconds ? this.totalSeconds : seconds;

            this.alInvoker.Source(this.srcId, VelaptorALSourcef.SecOffset, seconds);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to dispose of managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!Unloaded)
            {
                if (disposing)
                {
                    this.oggDecoder.Dispose();
                    this.mp3Decoder.Dispose();
                    this.audioManager.DeviceChanged -= AudioManager_DeviceChanged;
                }

                UnloadSoundData();

                this.alInvoker.ErrorCallback -= ErrorCallback;

                Unloaded = true;
            }
        }

        /// <summary>
        /// Maps the given audio <paramref name="format"/> to the <see cref="VelaptorALFormat"/> type equivalent.
        /// </summary>
        /// <param name="format">The format to convert.</param>
        /// <returns>The <see cref="VelaptorALFormat"/> result.</returns>
        // TODO: Remove this.  Not needed anymore
        //private static VelaptorALFormat MapFormat(AudioFormat format) => format switch
        //{
        //    VelaptorALFormat.Mono8 => TKALFormat.Mono8,
        //    VelaptorALFormat.Mono16 => TKALFormat.Mono16,
        //    VelaptorALFormat.Mono32Float => TKALFormat.MonoFloat32Ext,
        //    VelaptorALFormat.Stereo8 => TKALFormat.Stereo8,
        //    VelaptorALFormat.Stereo16 => TKALFormat.Stereo16,
        //    // TODO: This one might be an issue.  It does not seem to be supported in SILKS's version of the ALFormat enum
        //    VelaptorALFormat.StereoFloat32 => TKALFormat.StereoFloat32Ext,
        //    _ => throw new Exception("Invalid or unknown audio format."),
        //};

        /// <summary>
        /// Initializes the sound.
        /// </summary>
        private void Init()
        {
            if (!this.audioManager.IsInitialized)
            {
                this.audioManager.InitDevice();
            }

            (this.srcId, this.bufferId) = this.audioManager.InitSound();

            var extension = IOPath.GetExtension(Path);

            switch (extension)
            {
                case ".ogg":
                    var oggData = this.oggDecoder.LoadData(Path);

                    this.totalSeconds = oggData.TotalSeconds;

                    UploadOggData(oggData);
                    break;
                case ".mp3":
                    var mp3Data = this.mp3Decoder.LoadData(Path);

                    this.totalSeconds = mp3Data.TotalSeconds;

                    UploadMp3Data(mp3Data);
                    break;
                default:
                    throw new Exception($"The file extension '{extension}' is not supported file type.");
            }
        }

        /// <summary>
        /// Uploads Ogg audio data to the sound card.
        /// </summary>
        /// <param name="data">The ogg related sound data to upload.</param>
        private void UploadOggData(SoundData<float> data)
        {
            SoundSource soundSrc;
            soundSrc.SourceId = this.srcId;
            soundSrc.TotalSeconds = data.TotalSeconds;
            soundSrc.SampleRate = data.SampleRate;

            this.alInvoker.BufferData(
                this.bufferId,
                data.Format,
                data.BufferData.ToArray(),
                data.BufferData.Count * sizeof(float),
                data.SampleRate);

            // Bind the buffer to the source
            this.alInvoker.Source(this.srcId, VelaptorALSourcei.Buffer, (int)this.bufferId);

            this.audioManager.UpdateSoundSource(soundSrc);
        }

        /// <summary>
        /// Uploads MP3 audio data to the sound card.
        /// </summary>
        /// <param name="data">The mp3 related sound data to upload.</param>
        private void UploadMp3Data(SoundData<byte> data)
        {
            SoundSource soundSrc;
            soundSrc.SourceId = this.srcId;
            soundSrc.TotalSeconds = data.TotalSeconds;
            soundSrc.SampleRate = data.SampleRate;

            this.alInvoker.BufferData(
                this.bufferId,
                data.Format,
                data.BufferData.ToArray(),
                data.BufferData.Count,
                data.SampleRate);

            // Bind the buffer to the source
            this.alInvoker.Source(this.srcId, VelaptorALSourcei.Buffer, (int)this.bufferId);

            // TODO: Call audio manager update sound source
        }

        /// <summary>
        /// Unloads the sound data from the card.
        /// </summary>
        private void UnloadSoundData()
        {
            if (this.srcId <= 0)
            {
                return;
            }

            this.alInvoker.DeleteSource(this.srcId);

            if (this.bufferId != 0)
            {
                this.alInvoker.DeleteBuffer(this.bufferId);
            }
        }

        /// <summary>
        /// Invoked when the audio device has been changed.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Contains various event related information.</param>
        private void AudioManager_DeviceChanged(object? sender, EventArgs e) => Init();

        /// <summary>
        /// The callback invoked when an OpenAL error occurs.
        /// </summary>
        /// <param name="errorMsg">The OpenAL message.</param>
        [ExcludeFromCodeCoverage]
        private void ErrorCallback(string errorMsg)
        {
            // TODO: Create a custom audio exception type here
            throw new Exception(errorMsg);
        }
    }
}
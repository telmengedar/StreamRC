using System;
using System.IO;
using System.Net;
using NightlyCode.Core.Logs;

namespace StreamRC.Streaming.Cache {


    /// <summary>
    /// source for an image
    /// </summary>
    public interface IImageSource {

        /// <summary>
        /// key under which to store image
        /// </summary>
        string Key { get; }

        /// <summary>
        /// access to image data
        /// </summary>
        System.IO.Stream Data { get; }

    }
}
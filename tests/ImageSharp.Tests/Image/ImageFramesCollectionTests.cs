﻿using System;
using System.Collections.Generic;
using System.Text;

using SixLabors.ImageSharp.PixelFormats;

namespace SixLabors.ImageSharp.Tests
{
    public abstract partial class ImageFramesCollectionTests : IDisposable
    {
        protected Image<Rgba32> Image { get; }
        protected ImageFrameCollection<Rgba32> Collection { get; }

        public ImageFramesCollectionTests()
        {
            // Needed to get English exception messages, which are checked in several tests.
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");

            this.Image = new Image<Rgba32>(10, 10);
            this.Collection = new ImageFrameCollection<Rgba32>(this.Image, 10, 10, default(Rgba32));
        }

        public void Dispose()
        {
            this.Image.Dispose();
            this.Collection.Dispose();
        }
    }
}

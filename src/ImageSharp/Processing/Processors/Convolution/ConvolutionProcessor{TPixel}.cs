﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.ParallelUtils;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Primitives;
using SixLabors.Primitives;

namespace SixLabors.ImageSharp.Processing.Processors.Convolution
{
    /// <summary>
    /// Defines a processor that uses a 2 dimensional matrix to perform convolution against an image.
    /// </summary>
    /// <typeparam name="TPixel">The pixel format.</typeparam>
    internal class ConvolutionProcessor<TPixel> : ImageProcessor<TPixel>
        where TPixel : struct, IPixel<TPixel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConvolutionProcessor{TPixel}"/> class.
        /// </summary>
        /// <param name="kernelXY">The 2d gradient operator.</param>
        /// <param name="preserveAlpha">Whether the convolution filter is applied to alpha as well as the color channels.</param>
        public ConvolutionProcessor(in DenseMatrix<float> kernelXY, bool preserveAlpha)
        {
            this.KernelXY = kernelXY;
            this.PreserveAlpha = preserveAlpha;
        }

        /// <summary>
        /// Gets the 2d gradient operator.
        /// </summary>
        public DenseMatrix<float> KernelXY { get; }

        /// <summary>
        /// Gets a value indicating whether the convolution filter is applied to alpha as well as the color channels.
        /// </summary>
        public bool PreserveAlpha { get; }

        /// <inheritdoc/>
        protected override void OnFrameApply(ImageFrame<TPixel> source, Rectangle sourceRectangle, Configuration configuration)
        {
            DenseMatrix<float> matrix = this.KernelXY;
            bool preserveAlpha = this.PreserveAlpha;

            var interest = Rectangle.Intersect(sourceRectangle, source.Bounds());
            int startY = interest.Y;
            int endY = interest.Bottom;
            int startX = interest.X;
            int endX = interest.Right;
            int maxY = endY - 1;
            int maxX = endX - 1;

            using (Buffer2D<TPixel> targetPixels = configuration.MemoryAllocator.Allocate2D<TPixel>(source.Size()))
            {
                source.CopyTo(targetPixels);

                var workingRectangle = Rectangle.FromLTRB(startX, startY, endX, endY);
                int width = workingRectangle.Width;

                ParallelHelper.IterateRowsWithTempBuffer<Vector4>(
                    workingRectangle,
                    configuration,
                    (rows, vectorBuffer) =>
                        {
                            Span<Vector4> vectorSpan = vectorBuffer.Span;
                            int length = vectorSpan.Length;
                            ref Vector4 vectorSpanRef = ref MemoryMarshal.GetReference(vectorSpan);

                            for (int y = rows.Min; y < rows.Max; y++)
                            {
                                Span<TPixel> targetRowSpan = targetPixels.GetRowSpan(y).Slice(startX);
                                PixelOperations<TPixel>.Instance.ToVector4(configuration, targetRowSpan.Slice(0, length), vectorSpan);

                                if (preserveAlpha)
                                {
                                    for (int x = 0; x < width; x++)
                                    {
                                        DenseMatrixUtils.Convolve3(
                                            in matrix,
                                            source.PixelBuffer,
                                            ref vectorSpanRef,
                                            y,
                                            x,
                                            startY,
                                            maxY,
                                            startX,
                                            maxX);
                                    }
                                }
                                else
                                {
                                    for (int x = 0; x < width; x++)
                                    {
                                        DenseMatrixUtils.Convolve4(
                                            in matrix,
                                            source.PixelBuffer,
                                            ref vectorSpanRef,
                                            y,
                                            x,
                                            startY,
                                            maxY,
                                            startX,
                                            maxX);
                                    }
                                }

                                PixelOperations<TPixel>.Instance.FromVector4Destructive(configuration, vectorSpan, targetRowSpan);
                            }
                        });

                Buffer2D<TPixel>.SwapOrCopyContent(source.PixelBuffer, targetPixels);
            }
        }
    }
}
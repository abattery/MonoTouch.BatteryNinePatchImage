/*
 * 
 * Copyright (C) 2013 Jae sung Chung <jaesung.chung@classestudio.com>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *
 */
using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Battery.Touch.Images
{
	public class BatteryNinePatchImage
	{
		private class LineSegment
		{
			public int Start { get; set; }
			public int End { get; set; }

			public int Length { get { return End - Start + 1; } }

			public bool Stretch { get; set; }
		}

		private const uint BLACK_PIXEL_FULL_ALPHA = 0x000000FF;
		private List<LineSegment> _verticalLineSegments = new List<LineSegment>();
		private List<LineSegment> _horizontalLineSegments = new List<LineSegment>();

		public int Width, Height;

		private CGImage dataImage;

		public BatteryNinePatchImage(UIImage image)
		{
			dataImage = image.CGImage;
			IntPtr dataPointer = Marshal.AllocHGlobal(dataImage.Width * dataImage.Height * 4);
			
			CGBitmapContext context = new CGBitmapContext(dataPointer, dataImage.Width, dataImage.Height,
			                                              dataImage.BitsPerComponent, dataImage.BytesPerRow, dataImage.ColorSpace,
			                                              CGImageAlphaInfo.PremultipliedFirst);
			
			context.SetFillColorWithColor(UIColor.White.CGColor);
			context.FillRect(new RectangleF(0, 0, dataImage.Width, dataImage.Height));
			context.DrawImage(new RectangleF(0, 0, dataImage.Width, dataImage.Height), dataImage);
		
			unsafe
			{
				LineSegment lineSegment = null;
				uint* imagePointer = (uint*) (void*) dataPointer;
				for (int xx = 0; xx < dataImage.Width - 1; xx++, imagePointer++)
				{
					if (xx == 0)
					{
						continue;
					}
					uint thisValue = *imagePointer;
					if (lineSegment != null && (thisValue == BLACK_PIXEL_FULL_ALPHA) == lineSegment.Stretch)
					{
						lineSegment.End = xx;
					}
					else
					{
						lineSegment = new LineSegment();
						_horizontalLineSegments.Add (lineSegment);
						lineSegment.Start = xx;
						lineSegment.End = xx;
						lineSegment.Stretch = thisValue == BLACK_PIXEL_FULL_ALPHA;
					}
				}
				lineSegment = null;
				imagePointer = (uint*) (void*) dataPointer;
				for (int xx = 0; xx < dataImage.Height - 1; xx++, imagePointer += dataImage.Width)
				{
					if (xx == 0)
					{
						continue;
					}
					uint thisValue = *imagePointer;
					if (lineSegment != null && (thisValue == BLACK_PIXEL_FULL_ALPHA) == lineSegment.Stretch)
					{
						lineSegment.End = xx;
					}
					else
					{
						lineSegment = new LineSegment();
						lineSegment.Start = xx;
						lineSegment.End = xx;
						lineSegment.Stretch = thisValue == BLACK_PIXEL_FULL_ALPHA;
						_verticalLineSegments.Add (lineSegment);
					}
				}
			}
			Marshal.FreeHGlobal(dataPointer);
		}

		public UIImage CreateImage (float width, float height)
		{
			var rects = new List<RectangleF>();
			var newRects = new List<RectangleF>();

			var width_nostretch = _horizontalLineSegments.Where (p => !p.Stretch).Sum (p => p.Length);
			var width_stretch = _horizontalLineSegments.Where (p => p.Stretch).Sum (p => p.Length);
			var height_nostretch = _verticalLineSegments.Where (p => !p.Stretch).Sum (p => p.Length);
			var height_stretch = _verticalLineSegments.Where (p => p.Stretch).Sum (p => p.Length);
			var x = 1;
			var y = 1;
			var x_position = .0f;
			var y_position = .0f;

			RectangleF renderedRect;
 			
			for (var v = 0; v < _verticalLineSegments.Count; v++) 
			{
				var vv = _verticalLineSegments [v];
				x = 1;
				x_position = .0f;
				for (var h = 0; h < _horizontalLineSegments.Count; h++) {
					var hh = _horizontalLineSegments [h];

					var rect = new RectangleF((float)x, (float)y, (float) hh.Length, (float) vv.Length);;
					rects.Add (rect);

					renderedRect = new RectangleF();

					if (hh.Stretch) {
						renderedRect.Width = (float)hh.Length * (float)(width - width_nostretch) / (float)width_stretch;
					} else {
						renderedRect.Width = (float)hh.Length;
					}
					renderedRect.X = x_position;

					if (vv.Stretch) {
						renderedRect.Height = (float)vv.Length * (float) (height - height_nostretch) / (float)height_stretch;
					} else {
						renderedRect.Height = (float)vv.Length;
					}
					renderedRect.Y = y_position;
					newRects.Add (renderedRect);

					x += _horizontalLineSegments [h].Length;
					x_position += renderedRect.Width;
				}

				y_position += renderedRect.Height;
				y += _verticalLineSegments [v].Length;
			}

			var size = new SizeF(width, height);
			
			UIGraphics.BeginImageContext(size);
			
			var rectF = new RectangleF(new Point(0, 0), size);

			for (var p = 0; p < rects.Count; p++) {
				renderedRect = newRects[p];
				var rect = rects[p];
				var fillRect = new RectangleF(Math.Min ((float)Math.Floor(renderedRect.X), size.Width), Math.Min ((float)Math.Floor(renderedRect.Y), size.Height), Math.Min ((float)Math.Ceiling(renderedRect.Width), size.Width), Math.Min ((float)Math.Ceiling(renderedRect.Height), size.Height));

				var image = UIImage.FromImage(dataImage.WithImageInRect(new RectangleF(rect.X, rect.Y, rect.Width, rect.Height)));
				image.Draw (fillRect);
			}

			var newImage = UIGraphics.GetImageFromCurrentImageContext();
			
			UIGraphics.EndImageContext();

			return newImage;
		}
	}
}


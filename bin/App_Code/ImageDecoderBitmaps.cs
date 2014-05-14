/////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006, Frank Blumenberg
// 
// See License.txt for complete licensing and attribution information.
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
// THE SOFTWARE.
// 
/////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////
//
// This code contains code from SimplePsd class library by Igor Tolmachev.
// http://www.codeproject.com/csharp/simplepsd.asp
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace PhotoshopFiles
{
    public class ImageDecoder
    {
        public ImageDecoder()
        {

        }

        /////////////////////////////////////////////////////////////////////////// 

#if !TEST
        private struct PixelData
        {
            public byte Blue;
            public byte Green;
            public byte Red;
            public byte Alpha;
        }
#endif

        /////////////////////////////////////////////////////////////////////////// 

        public static Bitmap DecodeImage(PsdFile psdFile)
        {
            Bitmap bitmap = new Bitmap(psdFile.Columns, psdFile.Rows, PixelFormat.Format32bppArgb);

#if TEST
      for (int y = 0; y < psdFile.Rows; y++)
      {
        int rowIndex = y * psdFile.Columns;

        for (int x = 0; x < psdFile.Columns; x++)
        {
          int pos = rowIndex + x;

          Color pixelColor=GetColor(psdFile,pos);

          bitmap.SetPixel(x, y, pixelColor);
        }
      }

#else

            Rectangle r = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bd = bitmap.LockBits(r, ImageLockMode.ReadWrite, bitmap.PixelFormat);

            unsafe
            {
                byte* pCurrRowPixel = (byte*)bd.Scan0.ToPointer();

                for (int y = 0; y < psdFile.Rows; y++)
                {
                    int rowIndex = y * psdFile.Columns;
                    PixelData* pCurrPixel = (PixelData*)pCurrRowPixel;
                    for (int x = 0; x < psdFile.Columns; x++)
                    {
                        int pos = rowIndex + x;

                        Color pixelColor = GetColor(psdFile, pos);

                        pCurrPixel->Alpha = 255;
                        pCurrPixel->Red = pixelColor.R;
                        pCurrPixel->Green = pixelColor.G;
                        pCurrPixel->Blue = pixelColor.B;

                        pCurrPixel += 1;
                    }
                    pCurrRowPixel += bd.Stride;
                }
            }

            bitmap.UnlockBits(bd);
#endif

            return bitmap;
        }

        /////////////////////////////////////////////////////////////////////////// 

        private static Color GetColor(PsdFile psdFile, int pos)
        {
            Color c = Color.White;

            switch (psdFile.ColorMode)
            {
                case PsdFile.ColorModes.RGB:
                    c = Color.FromArgb(psdFile.ImageData[0][pos],
                                       psdFile.ImageData[1][pos],
                                       psdFile.ImageData[2][pos]);
                    break;
                case PsdFile.ColorModes.CMYK:
                    c = CMYKToRGB(psdFile.ImageData[0][pos],
                                  psdFile.ImageData[1][pos],
                                  psdFile.ImageData[2][pos],
                                  psdFile.ImageData[3][pos]);
                    break;
                case PsdFile.ColorModes.Multichannel:
                    c = CMYKToRGB(psdFile.ImageData[0][pos],
                                  psdFile.ImageData[1][pos],
                                  psdFile.ImageData[2][pos],
                                  0);
                    break;
                case PsdFile.ColorModes.Grayscale:
                case PsdFile.ColorModes.Duotone:
                    c = Color.FromArgb(psdFile.ImageData[0][pos],
                                       psdFile.ImageData[0][pos],
                                       psdFile.ImageData[0][pos]);
                    break;
                case PsdFile.ColorModes.Indexed:
                    {
                        int index = (int)psdFile.ImageData[0][pos];
                        c = Color.FromArgb((int)psdFile.ColorModeData[index],
                                         psdFile.ColorModeData[index + 256],
                                         psdFile.ColorModeData[index + 2 * 256]);
                    }
                    break;
                case PsdFile.ColorModes.Lab:
                    {
                        c = LabToRGB(psdFile.ImageData[0][pos],
                                     psdFile.ImageData[1][pos],
                                     psdFile.ImageData[2][pos]);
                    }
                    break;
            }

            return c;
        }

        /////////////////////////////////////////////////////////////////////////// 

        public static Bitmap DecodeImage(Layer layer)
        {
            if (layer.Rect.Width == 0 || layer.Rect.Height == 0)
            {
                return null;
            }

            Bitmap bitmap = new Bitmap(layer.Rect.Width, layer.Rect.Height, PixelFormat.Format32bppArgb);

#if TEST
      for (int y = 0; y < layer.Rect.Height; y++)
      {
        int rowIndex = y * layer.Rect.Width;

        for (int x = 0; x < layer.Rect.Width; x++)
        {
          int pos = rowIndex + x;

          //Color pixelColor=GetColor(psdFile,pos);
          Color pixelColor = Color.FromArgb(x % 255, Color.ForestGreen);// 255, 128, 0);

          bitmap.SetPixel(x, y, pixelColor);
        }
      }

#else

            Rectangle r = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bd = bitmap.LockBits(r, ImageLockMode.ReadWrite, bitmap.PixelFormat);

            unsafe
            {
                byte* pCurrRowPixel = (byte*)bd.Scan0.ToPointer();

                for (int y = 0; y < layer.Rect.Height; y++)
                {
                    int rowIndex = y * layer.Rect.Width;
                    PixelData* pCurrPixel = (PixelData*)pCurrRowPixel;
                    for (int x = 0; x < layer.Rect.Width; x++)
                    {
                        int pos = rowIndex + x;

                        Color pixelColor = GetColor(layer, pos);

                        if (layer.SortedChannels.ContainsKey(-2))
                        {
                            int maskAlpha = GetColor(layer.MaskData, x, y);
                            int oldAlpha = pixelColor.A;

                            int newAlpha = (oldAlpha * maskAlpha) / 255;
                            pixelColor = Color.FromArgb(newAlpha, pixelColor);
                        }

                        pCurrPixel->Alpha = pixelColor.A;
                        pCurrPixel->Red = pixelColor.R;
                        pCurrPixel->Green = pixelColor.G;
                        pCurrPixel->Blue = pixelColor.B;

                        pCurrPixel += 1;
                    }
                    pCurrRowPixel += bd.Stride;
                }
            }

            bitmap.UnlockBits(bd);
#endif

            return bitmap;
        }

        /////////////////////////////////////////////////////////////////////////// 

        private static Color GetColor(Layer layer, int pos)
        {
            Color c = Color.White;

            switch (layer.PsdFile.ColorMode)
            {
                case PsdFile.ColorModes.RGB:
                    c = Color.FromArgb(layer.SortedChannels[0].ImageData[pos],
                                       layer.SortedChannels[1].ImageData[pos],
                                       layer.SortedChannels[2].ImageData[pos]);
                    break;
                case PsdFile.ColorModes.CMYK:
                    c = CMYKToRGB(layer.SortedChannels[0].ImageData[pos],
                                  layer.SortedChannels[1].ImageData[pos],
                                  layer.SortedChannels[2].ImageData[pos],
                                  layer.SortedChannels[3].ImageData[pos]);
                    break;
                case PsdFile.ColorModes.Multichannel:
                    c = CMYKToRGB(layer.SortedChannels[0].ImageData[pos],
                                  layer.SortedChannels[1].ImageData[pos],
                                  layer.SortedChannels[2].ImageData[pos],
                                  0);
                    break;
                case PsdFile.ColorModes.Grayscale:
                case PsdFile.ColorModes.Duotone:
                    c = Color.FromArgb(layer.SortedChannels[0].ImageData[pos],
                                       layer.SortedChannels[0].ImageData[pos],
                                       layer.SortedChannels[0].ImageData[pos]);
                    break;
                case PsdFile.ColorModes.Indexed:
                    {
                        int index = (int)layer.SortedChannels[0].ImageData[pos];
                        c = Color.FromArgb((int)layer.PsdFile.ColorModeData[index],
                                         layer.PsdFile.ColorModeData[index + 256],
                                         layer.PsdFile.ColorModeData[index + 2 * 256]);
                    }
                    break;
                case PsdFile.ColorModes.Lab:
                    {
                        c = LabToRGB(layer.SortedChannels[0].ImageData[pos],
                                     layer.SortedChannels[1].ImageData[pos],
                                     layer.SortedChannels[2].ImageData[pos]);
                    }
                    break;
            }

            if (layer.SortedChannels.ContainsKey(-1))
                c = Color.FromArgb(layer.SortedChannels[-1].ImageData[pos], c);

            return c;
        }

        /////////////////////////////////////////////////////////////////////////// 

        private static int GetColor(Layer.Mask mask, int x, int y)
        {
            int c = 255;

            if (mask.PositionIsRelative)
            {
                x -= mask.Rect.X;
                y -= mask.Rect.Y;
            }
            else
            {
                x = (x + mask.Layer.Rect.X) - mask.Rect.X;
                y = (y + mask.Layer.Rect.Y) - mask.Rect.Y;
            }

            if (y >= 0 && y < mask.Rect.Height &&
                 x >= 0 && x < mask.Rect.Width)
            {
                int pos = y * mask.Rect.Width + x;
                if (pos < mask.ImageData.Length)
                    c = mask.ImageData[pos];
                else
                    c = 255;
            }

            return c;
        }

        /////////////////////////////////////////////////////////////////////////// 

        public static Bitmap DecodeImage(Layer.Mask mask)
        {
            Layer layer = mask.Layer;

            if (mask.Rect.Width == 0 || mask.Rect.Height == 0)
            {
                return null;
            }

            Bitmap bitmap = new Bitmap(mask.Rect.Width, mask.Rect.Height, PixelFormat.Format32bppArgb);

            Rectangle r = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bd = bitmap.LockBits(r, ImageLockMode.ReadWrite, bitmap.PixelFormat);

            unsafe
            {
                byte* pCurrRowPixel = (byte*)bd.Scan0.ToPointer();

                for (int y = 0; y < mask.Rect.Height; y++)
                {
                    int rowIndex = y * mask.Rect.Width;
                    PixelData* pCurrPixel = (PixelData*)pCurrRowPixel;
                    for (int x = 0; x < mask.Rect.Width; x++)
                    {
                        int pos = rowIndex + x;

                        Color pixelColor = Color.FromArgb(mask.ImageData[pos], mask.ImageData[pos], mask.ImageData[pos]);

                        pCurrPixel->Alpha = 255;
                        pCurrPixel->Red = pixelColor.R;
                        pCurrPixel->Green = pixelColor.G;
                        pCurrPixel->Blue = pixelColor.B;

                        pCurrPixel += 1;
                    }
                    pCurrRowPixel += bd.Stride;
                }
            }

            bitmap.UnlockBits(bd);

            return bitmap;
        }

        /////////////////////////////////////////////////////////////////////////// 

        private static Color LabToRGB(byte lb, byte ab, byte bb)
        {
            double exL, exA, exB;

            exL = (double)lb;
            exA = (double)ab;
            exB = (double)bb;

            double L_coef, a_coef, b_coef;
            L_coef = 256.0 / 100.0;
            a_coef = 256.0 / 256.0;
            b_coef = 256.0 / 256.0;

            int L = (int)(exL / L_coef);
            int a = (int)(exA / a_coef - 128.0);
            int b = (int)(exB / b_coef - 128.0);

            // For the conversion we first convert values to XYZ and then to RGB
            // Standards used Observer = 2, Illuminant = D65

            const double ref_X = 95.047;
            const double ref_Y = 100.000;
            const double ref_Z = 108.883;

            double var_Y = ((double)L + 16.0) / 116.0;
            double var_X = (double)a / 500.0 + var_Y;
            double var_Z = var_Y - (double)b / 200.0;

            if (Math.Pow(var_Y, 3) > 0.008856)
                var_Y = Math.Pow(var_Y, 3);
            else
                var_Y = (var_Y - 16 / 116) / 7.787;

            if (Math.Pow(var_X, 3) > 0.008856)
                var_X = Math.Pow(var_X, 3);
            else
                var_X = (var_X - 16 / 116) / 7.787;

            if (Math.Pow(var_Z, 3) > 0.008856)
                var_Z = Math.Pow(var_Z, 3);
            else
                var_Z = (var_Z - 16 / 116) / 7.787;

            double X = ref_X * var_X;
            double Y = ref_Y * var_Y;
            double Z = ref_Z * var_Z;

            return XYZToRGB(X, Y, Z);
        }

        ////////////////////////////////////////////////////////////////////////////

        private static Color XYZToRGB(double X, double Y, double Z)
        {
            // Standards used Observer = 2, Illuminant = D65
            // ref_X = 95.047, ref_Y = 100.000, ref_Z = 108.883

            double var_X = X / 100.0;
            double var_Y = Y / 100.0;
            double var_Z = Z / 100.0;

            double var_R = var_X * 3.2406 + var_Y * (-1.5372) + var_Z * (-0.4986);
            double var_G = var_X * (-0.9689) + var_Y * 1.8758 + var_Z * 0.0415;
            double var_B = var_X * 0.0557 + var_Y * (-0.2040) + var_Z * 1.0570;

            if (var_R > 0.0031308)
                var_R = 1.055 * (Math.Pow(var_R, 1 / 2.4)) - 0.055;
            else
                var_R = 12.92 * var_R;

            if (var_G > 0.0031308)
                var_G = 1.055 * (Math.Pow(var_G, 1 / 2.4)) - 0.055;
            else
                var_G = 12.92 * var_G;

            if (var_B > 0.0031308)
                var_B = 1.055 * (Math.Pow(var_B, 1 / 2.4)) - 0.055;
            else
                var_B = 12.92 * var_B;

            int nRed = (int)(var_R * 256.0);
            int nGreen = (int)(var_G * 256.0);
            int nBlue = (int)(var_B * 256.0);

            if (nRed < 0) nRed = 0;
            else if (nRed > 255) nRed = 255;
            if (nGreen < 0) nGreen = 0;
            else if (nGreen > 255) nGreen = 255;
            if (nBlue < 0) nBlue = 0;
            else if (nBlue > 255) nBlue = 255;

            return Color.FromArgb(nRed, nGreen, nBlue);
        }

        ///////////////////////////////////////////////////////////////////////////////
        //
        // The algorithms for these routines were taken from:
        //     http://www.neuro.sfc.keio.ac.jp/~aly/polygon/info/color-space-faq.html
        //
        // RGB --> CMYK                              CMYK --> RGB
        // ---------------------------------------   --------------------------------------------
        // Black   = minimum(1-Red,1-Green,1-Blue)   Red   = 1-minimum(1,Cyan*(1-Black)+Black)
        // Cyan    = (1-Red-Black)/(1-Black)         Green = 1-minimum(1,Magenta*(1-Black)+Black)
        // Magenta = (1-Green-Black)/(1-Black)       Blue  = 1-minimum(1,Yellow*(1-Black)+Black)
        // Yellow  = (1-Blue-Black)/(1-Black)
        //

        private static Color CMYKToRGB(byte c, byte m, byte y, byte k)
        {
            double C, M, Y, K;

            double exC, exM, exY, exK;
            double dMaxColours = Math.Pow(2, 8);

            exC = (double)c;
            exM = (double)m;
            exY = (double)y;
            exK = (double)k;

            C = (1.0 - exC / dMaxColours);
            M = (1.0 - exM / dMaxColours);
            Y = (1.0 - exY / dMaxColours);
            K = (1.0 - exK / dMaxColours);

            int nRed = (int)((1.0 - (C * (1 - K) + K)) * 255);
            int nGreen = (int)((1.0 - (M * (1 - K) + K)) * 255);
            int nBlue = (int)((1.0 - (Y * (1 - K) + K)) * 255);

            if (nRed < 0) nRed = 0;
            else if (nRed > 255) nRed = 255;
            if (nGreen < 0) nGreen = 0;
            else if (nGreen > 255) nGreen = 255;
            if (nBlue < 0) nBlue = 0;
            else if (nBlue > 255) nBlue = 255;

            return Color.FromArgb(nRed, nGreen, nBlue);
        }
    }
}
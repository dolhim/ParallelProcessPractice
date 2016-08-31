//#define USEPFOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// 영상 처리 관련 네임스페이스.
/// </summary>
namespace BitmapChipMaker
{
    /// <summary>
    /// 비트맵 저장/로드 도구
    /// </summary>
    public class BitmapBuffer
    {
        /// <summary>
        ///  Buffer Data Bitmap 으로 변환한다.
        /// </summary>
        /// <param name="src">byte 배열</param>
        /// <param name="width">너비</param>
        /// <param name="height">높이</param>
        /// <returns>비트맵</returns>
        public static Bitmap GetBitmapRGB(byte[] src, int width, int height)
        {
            Bitmap map = null;

            ////  Bitmap 생성
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            ////  lock it to get the BitmapData Object
            BitmapData bitmapData = null;

            try
            {
                bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);

                ////  copy the bytes
                for (int offsetH = 0; offsetH < bitmapData.Height; ++offsetH)
                {
                    Marshal.Copy(
                        src,
                        bitmapData.Width * 3 * offsetH,
                        bitmapData.Scan0 + (bitmapData.Stride * offsetH),
                        bitmapData.Width * 3);
                }

                map = bitmap;
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("에러 : " + ex.Message);
            }
            finally
            {
                ////  never forget to unlock the bitmap
                bitmap.UnlockBits(bitmapData);
            }
            return map;
        }

        /// <summary>
        /// 비트맵의 특정 부분에 대하여 RGB 배열로 변환한다.
        /// </summary>
        /// <param name="img">비트맵</param>
        /// <param name="startX">시작 위치 X</param>
        /// <param name="startY">시작 위치 Y</param>
        /// <param name="w">너비</param>
        /// <param name="h">높이</param>
        /// <returns></returns>
        public static byte[] bitmapToRGBBuffer(string path, int startX, int startY, int w, int h)
        {
            byte[] imgall = new byte[w * h * 3];
            byte[] buff = new byte[w * 3];

            // 영상 읽어오기
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                                    bufferSize: imgall.Length, useAsync: false))
            using (var img = (Bitmap)Image.FromStream(fs))
            {
#if USEPFOR 
                // unstable - do not use
                Parallel.For(0, h, i =>
#else
                for (int i = 0; i < h; ++i)
#endif
                {
                    Rectangle rect = new Rectangle(startX, startY + i, w, 1); // 한 줄 씩
                    BitmapData bmpDataL = null;

                    try
                    {
                        bmpDataL = img.LockBits(rect, ImageLockMode.ReadOnly, img.PixelFormat);
                        Marshal.Copy(bmpDataL.Scan0, buff, 0, w * 3);

                        for (int j = 0; j < w; ++j)
                        {
                            imgall[(i * img.Width * 3) + j * 3 + 0] = buff[j * 3 + 0];
                            imgall[(i * img.Width * 3) + j * 3 + 1] = buff[j * 3 + 1];
                            imgall[(i * img.Width * 3) + j * 3 + 2] = buff[j * 3 + 2];
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("-- Error bitmapToRGBBuffer --");
#if USEPFOR
                        return;
#else
                        return null;
#endif
                    }
                    finally
                    {
                        img.UnlockBits(bmpDataL);
                    }
#if USEPFOR
                }); // Parallel.For
#else
                }   // For
#endif
            }
            return imgall;
        }

#if false
        /// <summary>
        /// R 칼라 분해능 상수값
        /// </summary>
        public const float cR = .2989f;
        /// <summary>
        /// G 칼라 분해능 상수값
        /// </summary>
        public const float cG = .5870f;
        /// <summary>
        /// B 칼라 분해능 상수값
        /// </summary>
        public const float cB = .1140f;


        /// <summary>
        /// RGB 영상의 일부분을 Grayscale로 변환하여 배열로 반환한다.
        /// </summary>
        /// <param name="src">입력 RGB 영상</param>
        /// <param name="startX">영상에서 좌상단 위치 X</param>
        /// <param name="startY">영상에서 좌상단 위치 Y</param>
        /// <param name="w">변환할 영상 너비</param>
        /// <param name="h">변환할 영상 높이</param>
        /// <returns></returns>
        public static byte[] GetArrayGray(Bitmap src, int startX, int startY, int w, int h)
        {
            return bitmapToGrayBuffer(src, startX, startY, w, h);
        }


        /// <summary>
        /// Gray 배열로 반환한다.
        /// </summary>
        /// <param name="src">비트맵</param>
        /// <returns>byte 배열</returns>
        public static byte[] GetArrayGray(Bitmap src)
        {
            if (src.PixelFormat == PixelFormat.Format8bppIndexed)
                return bitmapToBuffer(src);
            else
                return bitmapToGrayBuffer(src);
        }


        /// <summary>
        /// 비트맵을 배열로 변환한다.
        /// </summary>
        /// <param name="src">비트맵</param>
        /// <returns>byte 배열</returns>
        public static byte[] GetArray(Bitmap src)
        {
            if (src.PixelFormat == PixelFormat.Format8bppIndexed)
                return bitmapToBuffer(src);
            else
                return RgbBitmapToBuffer(src);
        }


        /// <summary>
        /// 배열을 비트맵으로 변환한다.
        /// </summary>
        /// <param name="src">byte 배열</param>
        /// <param name="width">영상 너비</param>
        /// <param name="height">영상 높이</param>
        /// <returns>비트맵</returns>
        public static Bitmap GetBitmap(byte[] src, int width, int height)
        {
            return byteToBitmap(src, width, height);
        }


        /// <summary>
        /// RGB 비트맵을 RGB 배열로 변환한다.
        /// </summary>
        /// <param name="image">비트맵</param>
        /// <returns>byte 배열</returns>
        public static byte[] getRGB(Bitmap image)
        {
            var img = image;
            byte[] imgall = new byte[img.Width * img.Height * 3];
            byte[] buff = new byte[img.Width * 3];
            for (int i = 0; i < img.Height; ++i)
            {
                var bmpData = new BitmapData();
                var rect = new Rectangle(0, i, img.Width, 1);

                try
                {
                    bmpData = img.LockBits(rect, ImageLockMode.ReadOnly, img.PixelFormat);
                    Marshal.Copy(bmpData.Scan0, buff, 0, img.Width * 3);

                    for (int j = 0; j < img.Width; ++j)
                    {
                        imgall[(i * img.Width * 3) + j * 3 + 0] = buff[j * 3 + 0];
                        imgall[(i * img.Width * 3) + j * 3 + 1] = buff[j * 3 + 1];
                        imgall[(i * img.Width * 3) + j * 3 + 2] = buff[j * 3 + 2];
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("-- getRGB Error --");
                    Console.WriteLine(ex.Message);
                    return null;
                }
                finally
                {
                    img.UnlockBits(bmpData);
                }
            }
            return imgall;
        }

        /// <summary>
        /// 비트맵을 8bpp Indexed Gray로 변환한다.
        /// </summary>
        /// <param name="original">원본 비트맵</param>
        /// <returns>결과 비트맵</returns>
        public static Bitmap GetGrayBitmap(Bitmap original)
        {
            var arr = bitmapToGrayBuffer(original);
            var ret = byteToBitmap(arr, original.Width, original.Height);

            return ret;
        }

        /// <summary>
        /// 비트맵을 배열로 변환한다.
        /// </summary>
        /// <param name="img">비트맵</param>
        /// <returns>byte 배열</returns>
        private static byte[] bitmapToBuffer(Bitmap img)
        {
            byte[] imgall = new byte[img.Width * img.Height];
            {
                byte[] buff = new byte[img.Width * 3];
                for (int i = 0; i < img.Height; ++i)
                {
                    var rect = new Rectangle(0, i, img.Width, 1);
                    var bmpData = new BitmapData();
                    try
                    {
                        bmpData = img.LockBits(rect, ImageLockMode.ReadOnly, img.PixelFormat);

                        Marshal.Copy(bmpData.Scan0, buff, 0, img.Width * 3);

                        for (int j = 0; j < img.Width; ++j)
                        {
                            imgall[i * img.Width + j] = buff[j];
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("-- bitmapToBuffer Error --");
                        Console.WriteLine(ex.Message);
                        return null;
                    }
                    finally
                    {
                        img.UnlockBits(bmpData);
                    }
                }
            }
            return imgall;
        }

        /// <summary>
        /// 8bpp가 아닌 그레이 영상의 버퍼를 가져온다. (r, g, b가 같은 값일 때 사용한다.)
        /// </summary>
        /// <param name="img">입력 영상</param>
        /// <returns>버퍼</returns>
        private static byte[] RgbBitmapToBuffer(Bitmap img)
        {
            byte[] imgall = new byte[img.Width * img.Height];
            {
                byte[] buff = new byte[img.Width * 3];
                for (int i = 0; i < img.Height; ++i)
                {
                    var rect = new Rectangle(0, i, img.Width, 1);
                    var bmpData = new BitmapData();

                    try
                    {
                        bmpData = img.LockBits(rect, ImageLockMode.ReadOnly, img.PixelFormat);
                        Marshal.Copy(bmpData.Scan0, buff, 0, img.Width * 3);

                        for (int j = 0; j < img.Width; ++j)
                        {
                            byte g1 = buff[j * 3 + 2];
                            byte g2 = buff[j * 3 + 1];
                            byte g3 = buff[j * 3 + 0];
                            float gray = (g1 + g2 + g3) / 3;

                            imgall[i * img.Width + j] = (byte)Math.Ceiling(gray);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("-- RgbBitmapToBuffer Error --");
                        Console.WriteLine(ex.Message);
                        return null;
                    }
                    finally
                    {
                        img.UnlockBits(bmpData);
                    }
                }
            }
            return imgall;
        }

        /// <summary>
        /// 비트맵을 배열로 변환한다.
        /// </summary>
        /// <param name="img">비트맵</param>
        /// <returns>byte 배열</returns>
        private static byte[] bitmapToGrayBuffer(Bitmap img)
        {
            byte[] imgall = new byte[img.Width * img.Height];
            {
                byte[] buff = new byte[img.Width * 3];
                for (int i = 0; i < img.Height; ++i)
                {
                    Rectangle rect = new Rectangle(0, i, img.Width, 1);
                    BitmapData bmpDataL = null;

                    try
                    {
                        bmpDataL = img.LockBits(rect, ImageLockMode.ReadOnly, img.PixelFormat);
                        Marshal.Copy(bmpDataL.Scan0, buff, 0, img.Width * 3);

                        for (int j = 0; j < img.Width; ++j)
                        {
                            byte cr = buff[j * 3 + 2];
                            byte cg = buff[j * 3 + 1];
                            byte cb = buff[j * 3 + 0];
                            float gray = (cR * cr + cG * cg + cB * cb);

                            imgall[i * img.Width + j] = (byte)Math.Ceiling(gray - 0.5);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("-- bitmapToGrayBuffer Error --");
                        Console.WriteLine(ex.Message);
                        return null;
                    }
                    finally
                    {
                        img.UnlockBits(bmpDataL);
                    }
                }
            }
            return imgall;
        }

        /// <summary>
        /// 비트맵의 특정 부분에 대하여 gray 배열로 변환한다.
        /// </summary>
        /// <param name="img">비트맵</param>
        /// <param name="startX">시작 위치 X</param>
        /// <param name="startY">시작 위치 Y</param>
        /// <param name="w">너비</param>
        /// <param name="h">높이</param>
        /// <returns></returns>
        private static byte[] bitmapToGrayBuffer(Bitmap img, int startX, int startY, int w, int h)
        {
            byte[] imgall = new byte[w * h];
            {
                byte[] buff = new byte[w * 3];
                for (int i = 0; i < h; ++i)
                {
                    Rectangle rect = new Rectangle(startX, startY + i, w, 1); // 한 줄 씩
                    BitmapData bmpDataL = null;

                    try
                    {
                        bmpDataL = img.LockBits(rect, ImageLockMode.ReadOnly, img.PixelFormat);
                        Marshal.Copy(bmpDataL.Scan0, buff, 0, w * 3);

                        for (int j = 0; j < w; ++j)
                        {
                            byte cr = buff[j * 3 + 2];
                            byte cg = buff[j * 3 + 1];
                            byte cb = buff[j * 3 + 0];
                            float gray = (cR * cr + cG * cg + cB * cb);

                            imgall[i * w + j] = (byte)Math.Ceiling(gray - 0.5);
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("-- Error bitmapToGrayBuffer --");
                        return null;
                    }
                    finally
                    {
                        img.UnlockBits(bmpDataL);
                    }
                }
            }
            return imgall;
        }

        /// <summary>
        /// byte 배열을 8bpp indexed (gray) 비트맵으로 저장한다.
        /// </summary>
        /// <param name="src">byte 배열</param>
        /// <param name="width">너비</param>
        /// <param name="height">높이</param>
        /// <returns>비트맵</returns>
        private static Bitmap byteToBitmap(byte[] src, int width, int height)
        {
            ////  Buffer Data Bitmap 으로 변환
            Bitmap map = null;

            ////  8 Bitmap 생성
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

            ColorPalette palette = bitmap.Palette;

            ////  grayscale 팔레트 생성
            for (int i = 0; i < 256; i++)
            {
                palette.Entries[i] = Color.FromArgb((int)255, i, i, i);
            }

            ////  assign to bmp
            bitmap.Palette = palette;

            ////  lock it to get the BitmapData Object
            BitmapData bitmapData = null;

            try
            {
                bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format8bppIndexed);

                ////  copy the bytes
                for (int offsetH = 0; offsetH < bitmapData.Height; ++offsetH)
                {
                    Marshal.Copy(
                        src,
                        bitmapData.Width * offsetH,
                        bitmapData.Scan0 + (bitmapData.Stride * offsetH),
                        bitmapData.Width);
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("에러 : " + ex.Message);
            }
            finally
            {
                ////  never forget to unlock the bitmap
                bitmap.UnlockBits(bitmapData);
                map = bitmap;
            }
            return map;
        }

        /// <summary>
        /// double 배열을 영상으로 저장한다.
        /// </summary>
        /// <param name="data">영상 픽셀 데이터</param>
        /// <param name="w">영상 너비</param>
        /// <param name="h">영상 높이</param>
        /// <param name="path">저장 경로</param>
        public static void SaveBmpTest(double[] data, int w, int h, string path)
        {
            var testdata = Array.ConvertAll(data, element => Convert.ToByte(element));
            var testBitmap = BitmapBuffer.GetBitmap(testdata, h, w);
            testBitmap.Save(path);
        }
    }

    /// <summary>
    /// 영상 리사이즈 도구
    /// </summary>
    public class ImageResizer
    {
        /// <summary>
        /// 영상의 사이즈를 축소하고, 영상점 파일도 변환한다.
        /// 영상점 파일을 함께 변환하려면 영상 파일과 이름이 같아야한다.
        /// </summary>
        /// <param name="srcImage">원본 이미지 경로</param>
        /// <param name="dstDir">결과 이미지 폴더 경로</param>
        /// <param name="ratio">축소 비율. 기본값은 0.5으로 영상을 1/2로 축소한다.</param>
        /// <param name="bAddExt">저장 파일명 다음에 비율 정보를 추가한다. false 면 동일한 파일명으로 저장한다.</param>
        /// <returns>변환된 결과 영상 경로</returns>
        public static string Save(string srcImage, string dstDir, double ratio = 0.5, bool bAddExt = true)
        {
            var ret = string.Empty;
            var filename = Path.GetFileName(srcImage);

            if (!Directory.Exists(dstDir))
                Directory.CreateDirectory(dstDir);

            if (!dstDir.EndsWith(@"\"))
                dstDir += @"\";

            try
            {
                // 영상 변환
                {
                    using (var image = new Bitmap(srcImage))
                    {
                        using (var resized = ImageResizer.ResizeImageRatio(image, ratio))
                        {
                            ret = dstDir + filename;

                            if (bAddExt)
                                ret = Path.ChangeExtension(ret, string.Format("({0}){1}", ratio, Path.GetExtension(srcImage)));

                            resized.Save(ret, image.RawFormat);
                        }
                    }
                }

                // 영상점 파일 좌표 변환
                {
                    var ip = Path.ChangeExtension(srcImage, ".ip");
                    var resizedPath = dstDir + Path.GetFileName(ip);

                    if (bAddExt)
                        resizedPath = Path.ChangeExtension(resizedPath, string.Format("({0}){1}", ratio, Path.GetExtension(ip)));

                    ImageResizer.ResizeImagePoint(ip, resizedPath, ratio);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            //Console.WriteLine("영상 및 IP 파일이 저장되었습니다. 위치:" + srcImage);
            return ret;
        }

        /// <summary>
        /// 영상을 리사이즈한다.
        /// </summary>
        /// <param name="imgToResize">영상</param>
        /// <param name="ratio">변환 비율 (%)</param>
        /// <returns>변환된 영상</returns>
        public static Bitmap ResizeImageRatio(Bitmap imgToResize, double ratio)
        {
            var width = (int)(imgToResize.Width * ratio);
            var height = (int)(imgToResize.Height * ratio);

            var b = new Bitmap(width, height, imgToResize.PixelFormat);

            try
            {
                if (b.PixelFormat == PixelFormat.Format8bppIndexed)
                    b = ConvertTo24(b);

                using (Graphics g = Graphics.FromImage((Image)b))
                {
                    //g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                    g.DrawImage(imgToResize, 0, 0, width, height);
                    g.Dispose();
                }
            }
            catch
            {
                Console.WriteLine("[Error] Bitmap could not be resized");
            }
            return b;
        }

        /// <summary>
        /// 24bpp rgb로 변환한다.
        /// </summary>
        /// <param name="bmpIn">영상</param>
        /// <returns>변환된 영상</returns>
        public static Bitmap ConvertTo24(Bitmap bmpIn)
        {
            Bitmap converted = new Bitmap(bmpIn.Width, bmpIn.Height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(converted))
            {
                // Prevent DPI conversion
                g.PageUnit = GraphicsUnit.Pixel;
                // Draw the image
                g.DrawImageUnscaled(bmpIn, 0, 0);
                g.Dispose();
            }
            return converted;
        }

        /// <summary>
        /// 영상의 영상점을 변환한다.
        /// </summary>
        /// <param name="filename">영상 경로</param>
        /// <param name="dstname">영상 대상 경로</param>
        /// <param name="ratio">변환 비율 (%)</param>
        public static void ResizeImagePoint(string filename, string dstname, double ratio)
        {
            var result = new System.Text.StringBuilder();

            var line = string.Empty;
            var newLine = string.Empty;

            if (!File.Exists(filename))
                return;

            using (var streamReader = new StreamReader(filename))
            {
                while ((line = streamReader.ReadLine()) != null)
                {
                    var tok = new char[] { '\t', ' ' };
                    var words = line.Split(tok, StringSplitOptions.RemoveEmptyEntries);

                    if (words.Length == 4) // 'GCP ID' 'Usage' 'X' 'Y'
                    {
                        var x = Convert.ToDouble(words[2]);
                        var y = Convert.ToDouble(words[3]);

                        x = x * ratio;
                        y = y * ratio;

                        newLine = string.Format("{0}\t{1}\t{2}\t{3}\n",
                            words[0], words[1], x, y);

                        result.Append(newLine);
                    }
                    else if (words.Length == 2) // 'X' 'Y'
                    {
                        var x = Convert.ToDouble(words[0]);
                        var y = Convert.ToDouble(words[1]);

                        x = x * ratio;
                        y = y * ratio;

                        newLine = string.Format("{0}\t{1}\n", x, y);
                        result.Append(newLine);
                    }
                    else if (words.Length == 3) // 'GCP ID' 'X' 'Y'
                    {
                        var x = Convert.ToDouble(words[1]);
                        var y = Convert.ToDouble(words[2]);

                        x = x * ratio;
                        y = y * ratio;

                        newLine = string.Format("{0}\t{1}\n", x, y);
                        result.Append(newLine);
                    }
                }
            }

            using (var fileStream = new FileStream(dstname, FileMode.Create))
            {
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    streamWriter.Write(result);
                    streamWriter.Close();
                    fileStream.Close();
                }
            }
        }
#endif
    }

    /// <summary>
    /// 칩 저장 도구
    /// </summary>
    public class ChipMaker
    {
        /// <summary>
        /// RGB byte[]를 RGB 영상으로 저장한다.
        /// </summary>
        /// <param name="srcPath">원본 영상</param>
        /// <param name="srcW">원본 너비</param>
        /// <param name="srcH">원본 높이</param>
        /// <param name="dstDir">저장 경로</param>
        /// <param name="chipX">원본에서 저장할 시작 X</param>
        /// <param name="chipY">원본에서 저장할 시작 Y</param>
        /// <param name="chipW">저장 너비</param>
        /// <param name="chipH">저장 높이</param>
        /// <returns>성공 여부</returns>
        public static bool SaveChipRGB(string srcPath, int srcW, int srcH, byte[] dstArr,
                                       string dstDir, int chipX, int chipY, int chipW, int chipH)
        { 
            var sw1 = System.Diagnostics.Stopwatch.StartNew();

            var bmpArr = BitmapBuffer.bitmapToRGBBuffer(srcPath, chipX, chipY, chipW, chipH);
            System.Diagnostics.Debug.WriteLine(chipY + "bitmapToRGBBuffer : " + sw1.ElapsedMilliseconds);

            /// 다수의 스레드 내에서 한 배열에 쓰기
            Array.Copy(bmpArr, 0, dstArr, (chipX + (chipW * chipY)) * 3, bmpArr.Length);

            // 조각이미지 저장
            if (true)
            { 
                var chip = BitmapBuffer.GetBitmapRGB(bmpArr, chipW, chipH);
                System.Diagnostics.Debug.WriteLine(chipY + "GetBitmapRGB : " + sw1.ElapsedMilliseconds);
                chip.Save(dstDir, ImageFormat.Tiff);
                chip.Dispose();
            }

            System.Diagnostics.Debug.WriteLine(chipY + "Save : " + sw1.ElapsedMilliseconds);
            return true;
        }

#if false
        /// <summary>
        /// 컬러 칩을 저장한다.
        /// </summary>
        /// <param name="srcPath">원본 영상</param>
        /// <param name="srcW">원본 너비</param>
        /// <param name="srcH">원본 높이</param>
        /// <param name="dstDir">저장 경로</param>
        /// <param name="list">칩 위치(칩의 중심이 될 위치)</param>
        /// <param name="chipSize">칩 크기</param>
        /// <returns>성공 여부</returns>
        public static bool SaveChipRGB(string srcPath, int srcW, int srcH, string dstDir, Point pt, int chipW, int chipH)
        {
            var bmpArr = BitmapBuffer.bitmapToRGBBuffer(srcPath, pt.X, pt.Y, chipW, chipH);

            // byte
            var chipArr = new byte[chipW * chipH * 3];
            var chipPt = new Point((int)pt.X, (int)pt.Y);

            var chipPath = dstDir;

            CreateChip(bmpArr, srcW, srcH, chipPt, ref chipArr, chipW, chipH);

            var chip = BitmapBuffer.GetBitmapRGB(chipArr, chipW, chipH);
            chip.Save(chipPath, ImageFormat.Tiff);
            chip.Dispose();
            return true;
        }

        /// <summary>
        /// gray 칩을 저장한다.
        /// </summary>
        /// <param name="srcPath">원본 경로</param>
        /// <param name="dstDir">저장 디렉토리</param>
        /// <param name="pt">칩 위치 (칩의 중앙이 될 좌표)</param>
        /// <param name="chipSize">칩 크기</param>
        /// <returns>성공 여부</returns>
        public static bool SaveChip(string srcPath, string dstDir, Point pt, int chipSize)
        {
            var bmp = new Bitmap(srcPath);
            var bmpArr = BitmapBuffer.GetArrayGray(bmp);

            var chipArr = new byte[chipSize * chipSize];
            var chipPt = new Point((int)pt.X, (int)pt.Y);

            var chipPath = dstDir;

            CreateChip(bmpArr, bmp.Width, chipPt, ref chipArr, chipSize);

            var chip = BitmapBuffer.GetBitmap(chipArr, chipSize, chipSize);
            chip.Save(chipPath, ImageFormat.Tiff);
            chip.Dispose();
            return true;
        }

        /// <summary>
        /// 픽셀 데이터에서 칩으로 생성할 부분만 추출한다. 
        /// 입력받은 배열과 사이즈를 이용하여 픽셀 당 비트 수(bpp)를 계산한다.
        /// </summary>
        /// <param name="src">원본 데이터</param>
        /// <param name="srcWidth">원본 너비</param>
        /// <param name="srcHeight">원본 높이</param>
        /// <param name="pt">칩 위치</param>
        /// <param name="dst">저장 경로</param>
        /// <param name="W">칩 크기</param>
        /// <param name="H">칩 크기</param>
        private static void CreateChip(byte[] src, int srcWidth, int srcHeight, Point pt, ref byte[] dst, int W, int H)
        {
            var ret = new byte[src.Length];
            var pixelSize = src.Length / (W * H); // bpp 계산
            //var chip0 = (pt.Y - (H / 2)) * srcWidth * pixelSize + (pt.X - (W / 2)) * pixelSize; // 선택1 : 칩 중앙 
            var chip0 = 0;                         // 선택2 : 칩 좌상단

            if (chip0 < 0) return;

            try
            {
                for (var j = 0; j < H; ++j)
                {
                    var readStart = chip0 + srcWidth * j * pixelSize;
                    var dstStart = W * j * pixelSize;

                    // 한 줄 씩 복사
                    Buffer.BlockCopy(src, readStart, ret, dstStart, W * pixelSize);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("-- BitmapChipMaker.cs CreateChip Method(line 774) Error --");
                Console.WriteLine(ex.Message);
            }
            dst = ret;
        }

        /// <summary>
        /// 픽셀 데이터에서 칩으로 생성할 부분만 추출한다. 픽셀 당 비트 수가 8byte이여야함.
        /// </summary>
        /// <param name="src">원본 데이터</param>
        /// <param name="srcWidth">원본 너비</param>
        /// <param name="pt">시작 위치</param>
        /// <param name="dst">저장 경로</param>
        /// <param name="size">칩 크기</param>
        private static void CreateChip(byte[] src, int srcWidth, Point pt, ref byte[] dst, int size)
        {
            var ret = new byte[src.Length];
            var chip0 = (pt.Y - size / 2) * srcWidth + (pt.X - size / 2);

            if (chip0 < 0)
                return;

            for (var j = 0; j < size; ++j)
            {
                var readStart = chip0 + srcWidth * j;
                var dstStart = size * j;

                // 한 줄 씩 복사
                Buffer.BlockCopy(src, readStart, ret, dstStart, size);
            }
            dst = ret;
        }
#endif
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using HalconDotNet;

namespace ImageProcessLib
{
    public static class Utilities
    {
        public static bool ImageArrayToHObject(byte[] imageArray, int imageWidth, int imageHeight, out HObject ho_image)
        {
            HOperatorSet.GenEmptyObj(out ho_image);

            if (imageArray != null)
            {
                try
                {
                    unsafe
                    {
                        fixed (byte* bPointer = imageArray)
                        {
                            HOperatorSet.GenImage1(out ho_image, "byte", imageWidth, imageHeight, (IntPtr)bPointer);

                        }
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                ho_image = null;
                return false;
            }
        }


        //public bool ImageArrayToHimage(byte[] imageArray, int imageWidth, int imageHeight, out HObject ho_image)
        //{
        //    HOperatorSet.GenEmptyObj(out ho_image);
        //    bool result = false;
        //    if (imageArray != null)
        //    {
        //        try
        //        {
        //            IntPtr intPtr = Marshal.AllocHGlobal(imageArray.Length);
        //            Marshal.Copy(imageArray, 0, intPtr, imageWidth * imageHeight);
        //            HOperatorSet.GenImage1(out ho_image, "byte", imageWidth, imageHeight, intPtr);
        //            Marshal.FreeHGlobal(intPtr);
        //            result = true;
        //        }
        //        catch (Exception)
        //        {

        //            throw;
        //        }


        //    }
        //    return result;
        //}
        //图像类型已固定使用byte
        public static void ImgPtrToHobject(int width, int height, UInt64 imgptr, out HObject ho_image)
        {
            HOperatorSet.GenImage1(out ho_image, "byte", width, height, imgptr);
        }

        /// <summary>
        /// Labview图像转换为halcon图像，因labview图像有板边，线宽比图像宽，故分为线宽与实际宽度两个参数
        /// </summary>
        /// <param name="width">labview图像宽度</param>
        /// <param name="height">labview图像高度</param>
        /// <param name="lineWidth">labview图像指针线宽</param>
        /// <param name="imgptr">labview图像指针</param>
        /// <param name="ho_image">halcon图像</param>
        public static void ImgPtrToHobject(int width, int height, int lineWidth, UInt64 imgptr, out HObject ho_image)
        {
            HOperatorSet.GenEmptyObj(out HObject ho_img);
            HOperatorSet.GenEmptyObj(out ho_image);
            HOperatorSet.GenImage1(out ho_img, "byte", lineWidth, height, imgptr);
            HOperatorSet.CropPart(ho_img, out ho_image, 0, 0, width, height);
            ho_img.Dispose();
        }

        //图像类型已固定使用byte
        public static bool HoImageToImageArray(HObject ho_image, out byte[] ImageArray, out int width, out int height)
        {
            bool result = false;
            ImageArray = null;
            width = 0;
            height = 0;
            unsafe
            {
                if (ho_image != null)
                {
                    HOperatorSet.GetImagePointer1(ho_image, out HTuple pointer, out HTuple type, out HTuple W, out HTuple H);
                    width = W;
                    height = H;
                    ImageArray = new byte[width * height];
                    if (type == "byte")
                    {
                        IntPtr p = pointer;
                        Marshal.Copy(p, ImageArray, 0, width * height);
                        Marshal.FreeHGlobal(p);
                        result = true;
                    }
                    ho_image.Dispose();
                }

            }

            return result;
        }

    }

}

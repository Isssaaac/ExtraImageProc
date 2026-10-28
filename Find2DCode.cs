using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HalconDotNet;

namespace WOVE.FindCode
{
   public static class Find2DCode
    {
        private static string[] typeStr={ "Data Matrix ECC 200","Aztec Code","GS1 Aztec Code","GS1 DataMatrix","GS1 QR Code", "Micro QR Code","PDF417","QR Code"};
        // 'Aztec Code', 'Data Matrix ECC 200', 'GS1 Aztec Code', 'GS1 DataMatrix', 'GS1 QR Code', 'Micro QR Code', 'PDF417', 'QR Code'
        public enum CodeType
        {
            Data_atrix_ECC_200, Aztec_Code,GS1_Aztec_Code, GS1_DataMatrix, GS1_QR_Code, Micro_QR_Code, PDF417, QR_Code
        }
        public static int FindCode(HObject ho_Image, CodeType codeType, out List<Symbol2DCode> Code, int codeNum)
        {
            int result = -999;
            Code = new List<Symbol2DCode>();
            codeNum = 0;
            if (ho_Image == null) return -999;
            HOperatorSet.GenEmptyObj(out HObject ho_SymbolXLDs);
            HOperatorSet.GenEmptyObj(out HObject ho_SymbolXLD);
            HTuple hv_DataCodeHandle = null, hv_ResultHandles = null, hv_i = null;
            HTuple hv_DecodedDataStrings = null;

            HTuple hv_Raw, hv_Col;
            try
            {
                HOperatorSet.CreateDataCode2dModel(typeStr[(int)codeType], new HTuple(), new HTuple(),
                                                    out hv_DataCodeHandle);
                //HOperatorSet.SetDataCode2dParam(hv_DataCodeHandle, "persistence", 1);
                //HOperatorSet.SetDataCode2dParam(hv_DataCodeHandle, "default_parameters", "maximum_recognition");

                HOperatorSet.FindDataCode2d(ho_Image, out ho_SymbolXLDs, hv_DataCodeHandle, "stop_after_result_num",
                codeNum, out hv_ResultHandles, out hv_DecodedDataStrings);
                HOperatorSet.ClearDataCode2dModel(hv_DataCodeHandle);
                hv_DataCodeHandle = null;
                result = -998;
                int num = hv_ResultHandles.Length;
                for (hv_i = 0; hv_i < num; hv_i = hv_i + 1)
                {
                    ho_SymbolXLD.Dispose();
                    Symbol2DCode code = new Symbol2DCode();
                    HOperatorSet.SelectObj(ho_SymbolXLDs, out ho_SymbolXLD, hv_i + 1);//图像Index从1开始
                    result = -997;
                    HOperatorSet.GetContourXld(ho_SymbolXLD, out hv_Raw, out hv_Col);
                    HOperatorSet.SmallestRectangle2Xld(ho_SymbolXLD, out HTuple hv_rectRow, out HTuple hv_rectCol, out HTuple hv_phi, out HTuple hv_len1,
                        out HTuple hv_len2);
                    code.Rect_R = hv_rectRow;
                    code.Rect_C = hv_rectCol;
                    code.Rect_phi = hv_phi;
                    code.Rect_len1 = hv_len1;
                    code.Rect_len2 = hv_len2;
                    code.CodeString = hv_DecodedDataStrings.SArr[hv_i];//HTuple的Index从0开始

                    Code.Add(code);
                    result = -996;
                }
              
                if (hv_DecodedDataStrings.Length > 0)
                {
                    result = 0;
                }
                else
                {
                    result = -995;
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (hv_DataCodeHandle!=null)
                {
                    HOperatorSet.ClearDataCode2dModel(hv_DataCodeHandle);
                }
                ho_SymbolXLD.Dispose();
                ho_SymbolXLDs.Dispose();
                //ho_Image.Dispose();
            }
            return result;
        }
        public static int FindCode(HObject ho_Image,CodeType codeType , int codeNum, rect rect, out string[] Codes, out contour[] contours)
        {
            int result = -999;
            HTuple hv_Row, hv_Col;
            Codes = new string[0];
            contours = new contour[0];
            if (ho_Image == null) return -999;
            
            HOperatorSet.GenEmptyObj(out HObject ho_SymbolXLDs);
            HOperatorSet.GenEmptyObj(out HObject ho_SymbolXLD);
            HOperatorSet.GenEmptyObj(out HObject ho_rectangle);
            HOperatorSet.GenEmptyObj(out HObject ho_imageReduced);
            HTuple hv_DataCodeHandle = null, hv_ResultHandles = null;
            HTuple hv_DecodedDataStrings = null;
            
            if ((rect.rb_x<=rect.lt_x)||(rect.rb_y<=rect.lt_y))
            {
                ho_imageReduced.Dispose();
                ho_rectangle.Dispose();
                ho_SymbolXLD.Dispose();
                ho_SymbolXLDs.Dispose();
                return -990;
            }
            try
            {
                HOperatorSet.GenRectangle1(out ho_rectangle, rect.lt_y, rect.lt_x, rect.rb_y, rect.rb_x);
                HOperatorSet.ReduceDomain(ho_Image, ho_rectangle, out ho_imageReduced);

                HOperatorSet.CreateDataCode2dModel(typeStr[(int)codeType], new HTuple(), new HTuple(),
                                                    out hv_DataCodeHandle);
                HOperatorSet.SetDataCode2dParam(hv_DataCodeHandle, "persistence", 1);
                HOperatorSet.SetDataCode2dParam(hv_DataCodeHandle, "default_parameters", "maximum_recognition");

                HOperatorSet.FindDataCode2d(ho_imageReduced, out ho_SymbolXLDs, hv_DataCodeHandle, "stop_after_result_num",
                codeNum, out hv_ResultHandles, out hv_DecodedDataStrings);
                HOperatorSet.ClearDataCode2dModel(hv_DataCodeHandle);
                hv_DataCodeHandle = null;
                result = -998;
                int num = hv_ResultHandles.Length;
                Codes = hv_DecodedDataStrings;
                contours = new contour[num];
                for (int i = 0; i < num; i++)
                {
                    ho_SymbolXLD.Dispose();                   
                    HOperatorSet.SelectObj(ho_SymbolXLDs, out ho_SymbolXLD, i + 1);//图像Index从1开始
                    result = -997;
                    HOperatorSet.GetContourXld(ho_SymbolXLD, out hv_Row, out hv_Col);
                    contours[i].cols = hv_Col;
                    contours[i].rows = hv_Row;
                    result = -996;
                }
                result = 0;
            }
            catch (Exception ex)
            {
                throw ex;                
            }
            finally
            {
                if (hv_DataCodeHandle!=null)
                {
                    HOperatorSet.ClearDataCode2dModel(hv_DataCodeHandle);
                }
                ho_imageReduced.Dispose();
                ho_rectangle.Dispose();
                ho_SymbolXLD.Dispose();
                ho_SymbolXLDs.Dispose();
                //ho_Image.Dispose();
            }
            return result;
        }

        public static int FindBarcode(HObject ho_image,rect rect, out string[] Barcodes,out contour[] contours)
        {
            int result = -999;
            Barcodes = new string[0];
            contours = new contour[0];
            if (ho_image == null) return -999;
            HTuple hv_handle=null;
            HOperatorSet.GenEmptyObj(out HObject ho_rectangle);
            HOperatorSet.GenEmptyObj(out HObject ho_imageReduced);
            HOperatorSet.GenEmptyObj(out HObject ho_symbolRegions);
            if ((rect.rb_x <= rect.lt_x) || (rect.rb_y <= rect.lt_y))
            {
                ho_rectangle.Dispose();
                ho_imageReduced.Dispose();
                ho_symbolRegions.Dispose();
                return -990;
            }
            try
            {
                HOperatorSet.CreateBarCodeModel(new HTuple(), new HTuple(), out  hv_handle);
                HOperatorSet.GenRectangle1(out  ho_rectangle, rect.lt_y, rect.lt_x, rect.rb_y, rect.rb_x);
                result = -998;
                HTuple hv_decodedDataStrings = new HTuple();
                HOperatorSet.ReduceDomain(ho_image, ho_rectangle, out ho_imageReduced);
                result = -997;
                HOperatorSet.FindBarCode(ho_imageReduced, out ho_symbolRegions, hv_handle, "auto", out hv_decodedDataStrings);
                HOperatorSet.ClearBarCodeModel(hv_handle);
                hv_handle = null;
                result = -996;
                Barcodes = hv_decodedDataStrings;
                HOperatorSet.CountObj(ho_symbolRegions, out HTuple hv_num);
                result = -995;
                if (hv_num > 0)
                {
                    contours = new contour[hv_num];
                    for (int i = 0; i < hv_num; i++)
                    {
                        HOperatorSet.GenEmptyObj(out HObject ho_indexContour);
                        HOperatorSet.SelectObj(ho_symbolRegions, out ho_indexContour, i + 1);
                        HOperatorSet.GetRegionContour(ho_indexContour, out HTuple hv_conRows, out HTuple hv_conCols);
                        contours[i].rows = hv_conRows;
                        contours[i].cols = hv_conCols;
                        ho_indexContour.Dispose();
                    }

                }
                result = 0;
            }
            catch (Exception ex)
            {

                throw ex;
            }
            finally
            {
                if (hv_handle!=null)
                {
                    HOperatorSet.ClearBarCodeModel(hv_handle);
                }
                //ho_image.Dispose();
                ho_rectangle.Dispose();
                ho_imageReduced.Dispose();
                ho_symbolRegions.Dispose();
            }
            return result;
        }
    }
    public struct Symbol2DCode
    {
        public string CodeString;
        public double Rect_R;
        public double Rect_C;
        public double Rect_len1;
        public double Rect_len2;
        public double Rect_phi;
    }
}

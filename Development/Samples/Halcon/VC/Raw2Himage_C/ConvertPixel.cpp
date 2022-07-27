/***************************************************************************************************
* 
* 版权信息：版权所有 (c) 2016, 杭州海康威视数字技术股份有限公司, 保留所有权利
* 
* 文件名称：ConvertPixel.cpp
* 摘    要：数据格式转换
*
* 当前版本：1.0.0.0
* 作    者：杨伟杰
* 日    期：2017-11-07
* 备    注：新建
***************************************************************************************************/
#include "stdafx.h"
#include "ConvertPixel.h"

using namespace Halcon;

/************************************************************************
 *  @fn     ConvertToRGB()
 *  @brief  转换为RGB格式数据
 *  @param  pHandle                [IN]           句柄
 *  @param  pSrc                   [IN]           源数据
 *  @param  nHeight                [IN]           图像高度
 *  @param  nWidth                 [IN]           图像宽度
 *  @param  enGvspPixelType        [OUT]          转换后数据格式
 *  @param  pBuf                   [OUT]          转换后数据
 *  @return 成功，返回STATUS_OK；错误，返回STATUS_ERROR 
 ************************************************************************/
Status ConvertToRGB(void *pHandle, unsigned char *pSrc, int nHeight, int nWidth,
                    MvGvspPixelType nPixelType, unsigned char *pDst)
{
    if(NULL == pHandle || NULL == pSrc || NULL == pDst)
    {
        return MV_E_PARAMETER;
    }

    MV_CC_PIXEL_CONVERT_PARAM stPixelConvertParam;
    int nRet = STATUS_OK;

    stPixelConvertParam.pSrcData = pSrc;//源数据
    if (NULL == stPixelConvertParam.pSrcData)
    {
        return STATUS_ERROR;
    }

    stPixelConvertParam.nWidth = nWidth;//图像宽度
    stPixelConvertParam.nHeight = nHeight;//图像高度
    stPixelConvertParam.enSrcPixelType = nPixelType;//源数据的格式
    stPixelConvertParam.nSrcDataLen = (unsigned int)(nWidth * nHeight * ((((unsigned int)nPixelType) >> 16) & 0x00ff) >> 3);

    stPixelConvertParam.nDstBufferSize = (unsigned int)(nWidth * nHeight * ((((unsigned int)PixelType_Gvsp_RGB8_Packed) >> 16) & 0x00ff) >> 3);
    stPixelConvertParam.pDstBuffer = pDst;//转换后的数据
    stPixelConvertParam.enDstPixelType = PixelType_Gvsp_RGB8_Packed;
    stPixelConvertParam.nDstBufferSize = nWidth * nHeight * 3;

    nRet = MV_CC_ConvertPixelType(pHandle,&stPixelConvertParam);//格式转换
    if (STATUS_OK != nRet)
    {
        return STATUS_ERROR;
    }

    return STATUS_OK;
}

/************************************************************************
 *  @fn     ConvertRGBToHalcon()
 *  @brief  转换为RGB格式数据
 *  @param  pHandle                [IN]           句柄
 *  @param  Hobj                   [OUT]          转换后的输出Hobject数据
 *  @param  nHeight                [IN]           图像高度
 *  @param  nWidth                 [IN]           图像宽度
 *  @param  nPixelType             [IN]           源数据格式
 *  @param  pData                  [IN]           源数据
 *  @param  pData                  [IN]           存储红绿蓝色源数据的缓冲区
 *  @return 成功，返回STATUS_OK；错误，返回STATUS_ERROR 
 ************************************************************************/
Status ConvertRGBToHalcon(Halcon::Hobject *Hobj, int nHeight, int nWidth,
                          MvGvspPixelType nPixelType, unsigned char *pData, unsigned char *pDataSeparate)
{
    if(NULL == Hobj || NULL == pData || NULL == pDataSeparate)
    {
        return MV_E_PARAMETER;
    }

    unsigned char *dataRed = pDataSeparate;
    unsigned char *dataGreen = pDataSeparate + (nWidth * nHeight);
    unsigned char *dataBlue = pDataSeparate +(2 * nWidth * nHeight);
    int nSupWidth = (nWidth + 3)& 0xfffffffc;  //宽度补齐为4的倍数

    for (int row = 0; row <nHeight; row++)
    {
        unsigned char* ptr = &pData[row * nWidth * 3];
        for (int col = 0; col < nWidth; col++)
        {
            dataRed[row * nSupWidth + col] = ptr[3 * col];
            dataGreen[row * nSupWidth + col] = ptr[3 * col + 1];
            dataBlue[row * nSupWidth + col] = ptr[3 * col + 2];
        }
    }

    gen_image3(Hobj, "byte", nWidth, nHeight, (Hlong)(dataRed), (Hlong)(dataGreen), (Hlong)(dataBlue));

    return STATUS_OK;
}

Status ConvertMono8ToHalcon(Halcon::Hobject *Hobj, int nHeight, int nWidth, MvGvspPixelType nPixelType, unsigned char *pData)
{
    if(NULL == Hobj || NULL == pData)
    {
        return MV_E_PARAMETER;
    }

    gen_image1(Hobj, "byte", nWidth, nHeight, (Hlong)pData);
    return STATUS_OK;
}

/************************************************************************
 *  @fn     IsColorPixelFormat()
 *  @brief  判断是否是彩色格式
 *  @param  enType                [IN]            像素格式
 *  @return 彩色，返回true；黑白，返回false 
 ************************************************************************/
bool IsColorPixelFormat(MvGvspPixelType enType)
{
    switch(enType)
    {
        case PixelType_Gvsp_RGB8_Packed:
        case PixelType_Gvsp_BGR8_Packed:
        case PixelType_Gvsp_RGBA8_Packed:
        case PixelType_Gvsp_BGRA8_Packed:
        case PixelType_Gvsp_YUV422_Packed:
        case PixelType_Gvsp_YUV422_YUYV_Packed:
        case PixelType_Gvsp_BayerGR8:
        case PixelType_Gvsp_BayerRG8:
        case PixelType_Gvsp_BayerGB8:
        case PixelType_Gvsp_BayerBG8:
        case PixelType_Gvsp_BayerGB10:
        case PixelType_Gvsp_BayerGB10_Packed:
        case PixelType_Gvsp_BayerBG10:
        case PixelType_Gvsp_BayerBG10_Packed:
        case PixelType_Gvsp_BayerRG10:
        case PixelType_Gvsp_BayerRG10_Packed:
        case PixelType_Gvsp_BayerGR10:
        case PixelType_Gvsp_BayerGR10_Packed:
        case PixelType_Gvsp_BayerGB12:
        case PixelType_Gvsp_BayerGB12_Packed:
        case PixelType_Gvsp_BayerBG12:
        case PixelType_Gvsp_BayerBG12_Packed:
        case PixelType_Gvsp_BayerRG12:
        case PixelType_Gvsp_BayerRG12_Packed:
        case PixelType_Gvsp_BayerGR12:
        case PixelType_Gvsp_BayerGR12_Packed:
            return true;
        default:
            return false;
    }
}

/************************************************************************
 *  @fn     IsMonoPixelFormat()
 *  @brief  判断是否是黑白格式
 *  @param  enType                [IN]            像素格式
 *  @return 黑白，返回true；彩色，返回false 
 ************************************************************************/
bool IsMonoPixelFormat(MvGvspPixelType enType)
{
    switch(enType)
    {
        case PixelType_Gvsp_Mono8:
        case PixelType_Gvsp_Mono10:
        case PixelType_Gvsp_Mono10_Packed:
        case PixelType_Gvsp_Mono12:
        case PixelType_Gvsp_Mono12_Packed:
            return true;
        default:
            return false;
    }
}

/** @fn     ConvertToMono8(IN OUT unsigned char *pInData, IN OUT unsigned char *pOutData,IN unsigned int nDataSize, 
                                    IN OUT MV_FRAME_OUT_INFO_EX* pFrameInfo,MvGvspPixelType enGvspPixelType)
 *  @brief  转换像素格式为Mono8
 *  @param  pInData           [IN][OUT]   - 输入数据缓存
 *  @param  pOutData           [IN][OUT]   - 待输出数据缓存
 *  @param  nDataSize       [IN]        - 缓存大小
 *  @param  pFrameInfo      [IN][OUT]   - 待输出的帧信息
 *  
 *  @return 成功，返回MV_OK；失败，返回错误码
 */
Status ConvertToMono8(void *pHandle, IN OUT unsigned char *pInData, IN OUT unsigned char *pOutData,IN int nHeight,IN int nWidth,
                               MvGvspPixelType nPixelType)
{
    int nRet = STATUS_OK;

    if ( NULL == pInData || NULL == pOutData || NULL == pHandle)
    {
        return MV_E_PARAMETER;
    }

    MV_CC_PIXEL_CONVERT_PARAM stPixelConvertParam;

    stPixelConvertParam.pSrcData = pInData;//源数据
    if (NULL == stPixelConvertParam.pSrcData)
    {
        return STATUS_ERROR;
    }

    stPixelConvertParam.nWidth = nWidth;//图像宽度
    stPixelConvertParam.nHeight = nHeight;//图像高度
    stPixelConvertParam.enSrcPixelType = nPixelType;//源数据的格式
    stPixelConvertParam.nSrcDataLen = (unsigned int)(nWidth * nHeight * ((((unsigned int)nPixelType) >> 16) & 0x00ff) >> 3);

    stPixelConvertParam.nDstBufferSize = (unsigned int)(nWidth * nHeight * ((((unsigned int)PixelType_Gvsp_RGB8_Packed) >> 16) & 0x00ff) >> 3);
    stPixelConvertParam.pDstBuffer = pOutData;//转换后的数据
    stPixelConvertParam.enDstPixelType = PixelType_Gvsp_Mono8;
    stPixelConvertParam.nDstBufferSize = nWidth * nHeight * 3;

    nRet = MV_CC_ConvertPixelType(pHandle,&stPixelConvertParam);//格式转换
    if (STATUS_OK != nRet)
    {
        return STATUS_ERROR;
    }

    return nRet;
}

/** @fn     HalconDisplay(HTuple *hWindow, const Halcon::Hobject &Hobj, HTuple hImageWidth, HTuple hImageHeight)
 *  @brief  Halcon图像显示
 *  @param  hWindow               [IN]        - 输入数据缓存
 *  @param  Hobj                  [IN]        - Halcon格式数据
 *  @param  hImageWidth           [IN]        - 图像宽
 *  @param  hImageHeight          [IN]        - 图像高
 *  @return 成功，返回STATUS_OK；
 */
Status HalconDisplay(HTuple *hWindow, const Halcon::Hobject &Hobj, const Halcon::HTuple &hImageHeight,const Halcon::HTuple &hImageWidth)
{
    if(NULL == hWindow)
    {
        return MV_E_PARAMETER;
    }
    // ch: 显示 || en:display
    set_part(*hWindow, 0, 0, hImageHeight - 1, hImageWidth - 1);
    disp_obj((const Halcon::Hobject)Hobj, *hWindow);
    return STATUS_OK;
}

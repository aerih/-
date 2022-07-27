#include <HalconCpp.h>
#include <HalconCDefs.h>
#include <HProto.h>
#include <HWindow.h>
#include <stdio.h>
#include "MvCameraControl.h"
#include "PixelType.h"

/*函数返回码定义*/
typedef int Status;
#define STATUS_OK               0
#define STATUS_ERROR            -1


using namespace Halcon;

/** @fn     ConvertPixelTypeToMono8(IN OUT unsigned char *pInData, IN OUT unsigned char *pOutData,IN unsigned int nDataSize, 
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
                            MvGvspPixelType nPixelType);

/************************************************************************
 *  @fn     ConvertToRGB()
 *  @brief  转换为RGB格式数据
 *  @param  pHandle                [IN]           句柄
 *  @param  pSrc                   [IN]           源数据
 *  @param  nHeight                [IN]           图像高度
 *  @param  nWidth                 [IN]           图像宽度
 *  @param  enGvspPixelType        [IN]           源数据格式
 *  @param  pDst                   [OUT]          转换后数据
 *  @return 成功，返回STATUS_OK；错误，返回STATUS_ERROR 
 ************************************************************************/
Status ConvertToRGB(void *pHandle, unsigned char *pSrc, int nHeight, int nWidth, MvGvspPixelType nPixelType, unsigned char *pDst);

/************************************************************************
 *  @fn     ConvertRGBToHalcon()
 *  @brief  转换为RGB格式数据
 *  @param  pHandle                [IN]           句柄
 *  @param  Hobj                   [OUT]          转换后的输出Hobject数据
 *  @param  nHeight                [IN]           图像高度
 *  @param  nWidth                 [IN]           图像宽度
 *  @param  nPixelType             [IN]           源数据格式
 *  @param  pData                  [IN]           源数据
 *  @return 成功，返回STATUS_OK；错误，返回STATUS_ERROR 
 ************************************************************************/
Status ConvertMono8ToHalcon(Halcon::Hobject *Hobj, int nHeight, int nWidth, MvGvspPixelType nPixelType, unsigned char *pData);

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
                          MvGvspPixelType nPixelType, unsigned char *pData, unsigned char *pDataSeparate);

/************************************************************************
 *  @fn     IsColorPixelFormat()
 *  @brief  判断是否是彩色格式
 *  @param  enType                [IN]            像素格式
 *  @return 彩色，返回true；黑白，返回false 
 ************************************************************************/
bool IsColorPixelFormat(MvGvspPixelType enType);

/************************************************************************
 *  @fn     IsMonoPixelFormat()
 *  @brief  判断是否是黑白格式
 *  @param  enType                [IN]            像素格式
 *  @return 黑白，返回true；彩色，返回false 
 ************************************************************************/
bool IsMonoPixelFormat(MvGvspPixelType enType);

/** @fn     HalconDisplay(HTuple *hWindow, const Halcon::Hobject &Hobj, HTuple hImageWidth, HTuple hImageHeight)
 *  @brief  Halcon图像显示
 *  @param  hWindow               [IN]        - 输入数据缓存
 *  @param  Hobj                  [IN]        - Halcon格式数据
 *  @return 成功，返回STATUS_OK；
 */
Status HalconDisplay(HTuple *hWindow, const Halcon::Hobject &Hobj, const Halcon::HTuple &hImageWidth, const Halcon::HTuple &hImageHeight);

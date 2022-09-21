/*
    FITS Rating Tool
    Copyright (C) 2022 TheCyberBrick
    
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using FitsRatingTool.FitsLoader.Models;
using FitsRatingTool.FitsLoader.Native;
using System.Runtime.InteropServices;
using System.Text;
using static FitsRatingTool.FitsLoader.Native.INativeFitsLoader;

public class NativeFitsLoaderBootstrapper : INativeFitsLoader
{
    #region Interface
    public FitsHandle LoadFit(string file, long maxInputSize, int maxWidth, int maxHeight) => LoadFitNative(file, maxInputSize, maxWidth, maxHeight);

    public void CloseFitFile(FitsHandle handle) => CloseFitFileNative(handle);

    public bool ReadHeaderRecord(FitsHandle handle, int index, StringBuilder keyword, uint nkeyword, StringBuilder value, uint nvalue, StringBuilder comment, uint ncomment) => ReadHeaderRecordNative(handle, index, keyword, nkeyword, value, nvalue, comment, ncomment);

    public void FreeFit(FitsHandle handle) => FreeFitNative(handle);



    public FitsImageDataHandle LoadImageData(FitsHandle handle, FitsImageLoaderParameters parameters, uint[] histogram, uint histogramSize) => LoadImageDataNative(handle, parameters, histogram, histogramSize);

    public bool UnloadImageData(FitsImageDataHandle handle) => UnloadImageDataNative(handle);

    public bool IsImageDataLoaded(FitsImageDataHandle handle) => IsImageDataLoadedNative(handle);

    public void FreeImageData(FitsImageDataHandle handle) => FreeImageDataNative(handle);



    public FitsStatisticsHandle ComputeStatistics(FitsHandle fitsHandle, FitsImageDataHandle dataHandle, StatisticsProgressCallback? callback) => ComputeStatisticsNative(fitsHandle, dataHandle, callback);

    public bool GetPhotometry(FitsStatisticsHandle handle, int src_start, int src_n, int dst_start, PhotometryObject[] photometry) => GetPhotometryNative(handle, src_start, src_n, dst_start, photometry);

    public void FreeStatistics(FitsStatisticsHandle handle) => FreeStatisticsNative(handle);



    public ImageStretchParameters ComputeStretch(FitsHandle fitsHandle, FitsImageDataHandle dataHandle) => ComputeStretchNative(fitsHandle, dataHandle);



    public FitsImageHandle ProcessImage(FitsHandle fitsHandle, FitsImageDataHandle dataHandle, FitsImageHandle imgHandle, bool computeStrechParams, FitsImageLoaderParameters parameters, uint[] histogram, uint histogramSize) => ProcessImageNative(fitsHandle, dataHandle, imgHandle, computeStrechParams, parameters, histogram, histogramSize);

    public void FreeImage(FitsImageHandle imgHandle) => FreeImageNative(imgHandle);
    #endregion

    #region P/Invoke
    [DllImport(@"NativeFitsLoader", EntryPoint = "LoadFit", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern FitsHandle LoadFitNative([MarshalAs(UnmanagedType.LPStr)] string file, long maxInputSize, int maxWidth, int maxHeight);

    [DllImport(@"NativeFitsLoader", EntryPoint = "CloseFitFile", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern void CloseFitFileNative(FitsHandle handle);

    [DllImport(@"NativeFitsLoader", EntryPoint = "ReadHeaderRecord", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern bool ReadHeaderRecordNative(FitsHandle handle, int index, [MarshalAs(UnmanagedType.LPStr)] StringBuilder keyword, uint nkeyword, [MarshalAs(UnmanagedType.LPStr)] StringBuilder value, uint nvalue, [MarshalAs(UnmanagedType.LPStr)] StringBuilder comment, uint ncomment);

    [DllImport(@"NativeFitsLoader", EntryPoint = "FreeFit", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern void FreeFitNative(FitsHandle handle);



    [DllImport(@"NativeFitsLoader", EntryPoint = "LoadImageData", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern FitsImageDataHandle LoadImageDataNative(FitsHandle handle, FitsImageLoaderParameters parameters, [In, Out] uint[] histogram, uint histogramSize);

    [DllImport(@"NativeFitsLoader", EntryPoint = "UnloadImageData", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern bool UnloadImageDataNative(FitsImageDataHandle handle);

    [DllImport(@"NativeFitsLoader", EntryPoint = "IsImageDataLoaded", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern bool IsImageDataLoadedNative(FitsImageDataHandle handle);

    [DllImport(@"NativeFitsLoader", EntryPoint = "FreeImageData", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern void FreeImageDataNative(FitsImageDataHandle handle);



    [DllImport(@"NativeFitsLoader", EntryPoint = "ComputeStatistics", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern FitsStatisticsHandle ComputeStatisticsNative(FitsHandle fitsHandle, FitsImageDataHandle dataHandle, StatisticsProgressCallback? callback);

    [DllImport(@"NativeFitsLoader", EntryPoint = "GetPhotometry", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern bool GetPhotometryNative(FitsStatisticsHandle handle, int src_start, int src_n, int dst_start, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Struct)][In, Out] PhotometryObject[] photometry);

    [DllImport(@"NativeFitsLoader", EntryPoint = "FreeStatistics", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern void FreeStatisticsNative(FitsStatisticsHandle handle);



    [DllImport(@"NativeFitsLoader", EntryPoint = "ComputeStretch", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern ImageStretchParameters ComputeStretchNative(FitsHandle fitsHandle, FitsImageDataHandle dataHandle);



    [DllImport(@"NativeFitsLoader", EntryPoint = "ProcessImage", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern FitsImageHandle ProcessImageNative(FitsHandle fitsHandle, FitsImageDataHandle dataHandle, FitsImageHandle imgHandle, bool computeStrechParams, FitsImageLoaderParameters parameters, [In, Out] uint[] histogram, uint histogramSize);

    [DllImport(@"NativeFitsLoader", EntryPoint = "FreeImage", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern void FreeImageNative(FitsImageHandle imgHandle);
    #endregion
}

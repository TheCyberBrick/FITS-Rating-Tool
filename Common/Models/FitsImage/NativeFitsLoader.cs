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

using System.Runtime.InteropServices;
using System.Text;

namespace FitsRatingTool.Common.Models.FitsImage
{
    internal static class NativeFitsLoader
    {
#pragma warning disable 0649 // Fields are set in native code
        internal readonly struct FitsImageHandle
        {
            public readonly IntPtr data;
        }

        internal readonly struct FitsImageDataHandle
        {
            public readonly byte valid;
            public readonly IntPtr image;
            public readonly IntPtr data;
        };

        internal readonly struct FitsHandle
        {
            public readonly byte valid;
            public readonly FitsImageDim inDim;
            public readonly FitsImageDim outDim;
            public readonly int header_records;
            public readonly int max_header_keyword_size;
            public readonly int max_header_value_size;
            public readonly int max_header_comment_size;
            public readonly IntPtr info;
        };

        internal readonly struct FitsStatisticsHandle
        {
            public readonly byte valid;
            public readonly IntPtr catalog;
            public readonly int count;
            public readonly PhotometryStatistics statistics;
        };
#pragma warning restore 0649


        [DllImport(@"FitsLoader.dll", EntryPoint = "LoadFit", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern FitsHandle LoadFit([MarshalAs(UnmanagedType.LPStr)] string file, long maxInputSize, int maxWidth, int maxHeight);

        [DllImport(@"FitsLoader.dll", EntryPoint = "CloseFitFile", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void CloseFitFile(FitsHandle handle);

        [DllImport(@"FitsLoader.dll", EntryPoint = "ReadHeaderRecord", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern bool ReadHeaderRecord(FitsHandle handle, int index, [MarshalAs(UnmanagedType.LPStr)] StringBuilder keyword, uint nkeyword, [MarshalAs(UnmanagedType.LPStr)] StringBuilder value, uint nvalue, [MarshalAs(UnmanagedType.LPStr)] StringBuilder comment, uint ncomment);

        [DllImport(@"FitsLoader.dll", EntryPoint = "FreeFit", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void FreeFit(FitsHandle handle);



        [DllImport(@"FitsLoader.dll", EntryPoint = "LoadImageData", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern FitsImageDataHandle LoadImageData(FitsHandle handle, FitsImageLoaderParameters parameters, [In, Out] uint[] histogram, uint histogramSize);

        [DllImport(@"FitsLoader.dll", EntryPoint = "FreeImageData", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void FreeImageData(FitsImageDataHandle handle);



        [DllImport(@"FitsLoader.dll", EntryPoint = "ComputeStatistics", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern FitsStatisticsHandle ComputeStatistics(FitsHandle fitsHandle, FitsImageDataHandle dataHandle, StatisticsProgressCallback? callback);

        [DllImport(@"FitsLoader.dll", EntryPoint = "GetPhotometry", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern bool GetPhotometry(FitsStatisticsHandle handle, int src_start, int src_n, int dst_start, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Struct)][In, Out] PhotometryObject[] photometry);

        [DllImport(@"FitsLoader.dll", EntryPoint = "FreeStatistics", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void FreeStatistics(FitsStatisticsHandle handle);



        [DllImport(@"FitsLoader.dll", EntryPoint = "ComputeStretch", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern ImageStretchParameters ComputeStretch(FitsHandle fitsHandle, FitsImageDataHandle dataHandle);



        [DllImport(@"FitsLoader.dll", EntryPoint = "ProcessImage", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern FitsImageHandle ProcessImage(FitsHandle fitsHandle, FitsImageDataHandle dataHandle, FitsImageHandle imgHandle, bool computeStrechParams, FitsImageLoaderParameters parameters, [In, Out] uint[] histogram, uint histogramSize);

        [DllImport(@"FitsLoader.dll", EntryPoint = "FreeImage", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern void FreeImage(FitsImageHandle imgHandle);


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate bool StatisticsProgressCallback(PhotometryPhase phase, int nobj, int iobj, int nstars);
    }
}

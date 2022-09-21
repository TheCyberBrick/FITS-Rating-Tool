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
using System.Text;

namespace FitsRatingTool.FitsLoader.Native
{
    public interface INativeFitsLoader
    {
        FitsHandle LoadFit(string file, long maxInputSize, int maxWidth, int maxHeight);

        void CloseFitFile(FitsHandle handle);

        bool ReadHeaderRecord(FitsHandle handle, int index, StringBuilder keyword, uint nkeyword, StringBuilder value, uint nvalue, StringBuilder comment, uint ncomment);

        void FreeFit(FitsHandle handle);



        FitsImageDataHandle LoadImageData(FitsHandle handle, FitsImageLoaderParameters parameters, uint[] histogram, uint histogramSize);

        bool UnloadImageData(FitsImageDataHandle handle);

        bool IsImageDataLoaded(FitsImageDataHandle handle);

        void FreeImageData(FitsImageDataHandle handle);



        FitsStatisticsHandle ComputeStatistics(FitsHandle fitsHandle, FitsImageDataHandle dataHandle, StatisticsProgressCallback? callback);

        bool GetPhotometry(FitsStatisticsHandle handle, int src_start, int src_n, int dst_start, PhotometryObject[] photometry);

        void FreeStatistics(FitsStatisticsHandle handle);



        ImageStretchParameters ComputeStretch(FitsHandle fitsHandle, FitsImageDataHandle dataHandle);



        FitsImageHandle ProcessImage(FitsHandle fitsHandle, FitsImageDataHandle dataHandle, FitsImageHandle imgHandle, bool computeStrechParams, FitsImageLoaderParameters parameters, uint[] histogram, uint histogramSize);

        void FreeImage(FitsImageHandle imgHandle);


        delegate bool StatisticsProgressCallback(PhotometryPhase phase, int nobj, int iobj, int nstars);
    }
}
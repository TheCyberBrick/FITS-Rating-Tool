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
using System.Text;

namespace FitsRatingTool.Common.Models.FitsImage
{
    public class NativeFitsImage : IFitsImage
    {
        private class Reference : IDisposable
        {
            private NativeFitsImage image;

            private volatile bool disposed = false;

            public Reference(NativeFitsImage image)
            {
                this.image = image;
                Interlocked.Increment(ref image.refCount);
            }

            public void Dispose()
            {
                lock (image)
                {
                    if (!disposed && Interlocked.Decrement(ref image.refCount) <= 0)
                    {
                        image.Dispose();
                        disposed = true;
                    }
                }
            }
        }

        private int refCount = 0;

        private readonly INativeFitsLoader loader;

        private FitsHandle fitsHandle;
        private FitsImageDataHandle dataHandle;
        private FitsStatisticsHandle statisticsHandle;
        private FitsImageHandle imgHandle;

        private volatile bool disposed = false;

        public bool IsFileClosed { get; private set; }

        public bool IsImageDataValid { get; private set; }

        public bool IsStatisticsValid { get; private set; }

        public bool IsImageValid { get; private set; }

        public uint[] Histogram { get; private set; } = new uint[512];

        public uint[] StretchedHistogram { get; private set; } = new uint[512];

        private bool _alwaysUnloadImageData = false;
        public bool AlwaysUnloadImageData
        {
            get => _alwaysUnloadImageData;
            set
            {
                _alwaysUnloadImageData = value;
                if (value)
                {
                    UnloadImageData();
                }
            }
        }

        public string File { get; }

        private readonly Dictionary<string, FitsImageHeaderRecord> _header = new();
        public IReadOnlyDictionary<string, FitsImageHeaderRecord> Header { get => _header; }

        IEnumerable<IFitsImageHeaderRecord> IFitsImageMetadata.Header
        {
            get
            {
                foreach (var value in Header)
                {
                    yield return value.Value;
                }
            }
        }

        public FitsImageDim InDim => fitsHandle.inDim;

        public FitsImageDim OutDim => fitsHandle.outDim;

        internal NativeFitsImage(INativeFitsLoader loader, string file, FitsHandle fitsHandle)
        {
            this.loader = loader;
            File = file;
            this.fitsHandle = fitsHandle;

            for (int i = 0; i < fitsHandle.header_records; ++i)
            {
                StringBuilder keyword = new(fitsHandle.max_header_keyword_size);
                StringBuilder value = new(fitsHandle.max_header_value_size);
                StringBuilder comment = new(fitsHandle.max_header_comment_size);

                loader.ReadHeaderRecord(fitsHandle, i, keyword, (uint)keyword.Capacity + 1, value, (uint)value.Capacity + 1, comment, (uint)comment.Capacity + 1);

                string valueStr = value.ToString();
                if (valueStr.StartsWith("'") && valueStr.EndsWith("'"))
                {
                    valueStr = valueStr.Substring(1, valueStr.Length - 2);
                }
                valueStr = valueStr.Trim();

                string commentStr = comment.ToString();
                if (commentStr.StartsWith("'") && commentStr.EndsWith("'"))
                {
                    commentStr = commentStr.Substring(1, commentStr.Length - 2);
                }
                commentStr = commentStr.Trim();

                FitsImageHeaderRecord record = new(keyword.ToString(), valueStr, commentStr);

                // TODO Keywords are not unique
                _header.TryAdd(record.Keyword, record);
            }
        }

        ~NativeFitsImage()
        {
            Dispose(false);
        }

        public void CloseFile()
        {
            lock (this)
            {
                if (disposed)
                {
                    return;
                }

                if (fitsHandle.info.ToInt64() != 0)
                {
                    IsFileClosed = true;
                    loader.CloseFitFile(fitsHandle);
                }
            }
        }

        private void UpdateImageDataValid()
        {
            IsImageDataValid = dataHandle.image_ptr.ToInt64() != 0 && dataHandle.valid == 1 && loader.IsImageDataLoaded(dataHandle);
        }

        public bool LoadImageData(FitsImageLoaderParameters parameters)
        {
            lock (this)
            {
                if (disposed)
                {
                    return false;
                }

                IsImageDataValid = false;

                if (fitsHandle.info.ToInt64() == 0)
                {
                    throw new Exception("Already disposed");
                }

                if (dataHandle.image_ptr.ToInt64() != 0)
                {
                    loader.FreeImageData(dataHandle);
                    dataHandle = default;
                }

                uint[] newHistogram = new uint[Histogram.Length];
                dataHandle = loader.LoadImageData(fitsHandle, parameters, newHistogram, (uint)newHistogram.Length);

                UpdateImageDataValid();
                if (AlwaysUnloadImageData)
                {
                    UnloadImageData();
                }

                Histogram = newHistogram;

                return IsImageDataValid;
            }
        }

        public void UnloadImageData()
        {
            lock (this)
            {
                if (disposed)
                {
                    return;
                }

                if (dataHandle.image_ptr.ToInt64() != 0)
                {
                    loader.UnloadImageData(dataHandle);
                }
            }
        }

        public bool ComputeStatisticsAndPhotometry(IFitsImage.PhotometryCallback? callback = null)
        {
            FitsStatisticsHandle handle;

            lock (this)
            {
                if (disposed)
                {
                    return false;
                }

                IsStatisticsValid = false;

                if (fitsHandle.info.ToInt64() == 0)
                {
                    throw new ObjectDisposedException(nameof(NativeFitsImage));
                }

                if (dataHandle.image_ptr.ToInt64() == 0)
                {
                    return false;
                }

                if (statisticsHandle.catalog.ToInt64() != 0)
                {
                    loader.FreeStatistics(statisticsHandle);
                    statisticsHandle = default;
                }

                handle = statisticsHandle = loader.ComputeStatistics(fitsHandle, dataHandle, callback != null ? (phase, nobj, iobj, nstars) => callback(phase, nobj, iobj, nstars, false, null) : null);

                UpdateImageDataValid();
                if (AlwaysUnloadImageData)
                {
                    UnloadImageData();
                }

                if (!IsImageDataValid)
                {
                    return false;
                }

                IsStatisticsValid = handle.valid == 1;
            }

            if (callback != null)
            {
                callback.Invoke(PhotometryPhase.Completed, 0, 0, 0, handle.valid == 1, handle.valid == 1 ? handle.statistics : null);
            }

            return handle.valid == 1;
        }

        public bool GetStatistics(out PhotometryStatistics statistics)
        {
            lock (this)
            {
                if (disposed)
                {
                    statistics = default;
                    return false;
                }

                if (statisticsHandle.catalog.ToInt64() == 0)
                {
                    statistics = default;
                    return false;
                }

                statistics = statisticsHandle.statistics;

                return statisticsHandle.valid == 1;
            }
        }

        public bool GetPhotometry(out PhotometryObject[]? photometry)
        {
            lock (this)
            {
                if (disposed)
                {
                    photometry = default;
                    return false;
                }

                if (statisticsHandle.catalog.ToInt64() == 0)
                {
                    photometry = default;
                    return false;
                }

                photometry = new PhotometryObject[statisticsHandle.count];

                return loader.GetPhotometry(statisticsHandle, 0, statisticsHandle.count, 0, photometry);
            }
        }

        public bool ComputeStretch(out ImageStretchParameters parameters)
        {
            lock (this)
            {
                if (disposed)
                {
                    parameters = default;
                    return false;
                }

                if (fitsHandle.info.ToInt64() == 0)
                {
                    throw new ObjectDisposedException(nameof(NativeFitsImage));
                }

                if (dataHandle.image_ptr.ToInt64() == 0)
                {
                    parameters = default;
                    return false;
                }

                parameters = loader.ComputeStretch(fitsHandle, dataHandle);

                UpdateImageDataValid();
                if (AlwaysUnloadImageData)
                {
                    UnloadImageData();
                }

                return IsImageDataValid;
            }
        }

        bool IFitsImage.ProcessImage(bool computeStretch, FitsImageLoaderParameters parameters, out IFitsImageData? data)
        {
            lock (this)
            {
                if (disposed)
                {
                    data = null;
                    return false;
                }

                if (ProcessImage(computeStretch, parameters, out var nativeData))
                {
                    data = nativeData;
                    return true;
                }
            }

            data = null;

            return false;
        }

        public bool ProcessImage(bool computeStretch, FitsImageLoaderParameters parameters, out NativeFitsImageData data)
        {
            lock (this)
            {
                if (disposed)
                {
                    data = default;
                    return false;
                }

                IsImageValid = false;

                if (fitsHandle.info.ToInt64() == 0)
                {
                    throw new ObjectDisposedException(nameof(NativeFitsImage));
                }

                if (dataHandle.image_ptr.ToInt64() == 0)
                {
                    data = default;
                    return false;
                }

                if (imgHandle.data.ToInt64() != 0)
                {
                    loader.FreeImage(imgHandle);
                    imgHandle = default;
                }

                uint[] newHistogram = new uint[StretchedHistogram.Length];
                imgHandle = loader.ProcessImage(fitsHandle, dataHandle, imgHandle, computeStretch, parameters, newHistogram, (uint)newHistogram.Length);

                UpdateImageDataValid();
                if (AlwaysUnloadImageData)
                {
                    UnloadImageData();
                }

                if (!IsImageDataValid)
                {
                    data = default;
                    return false;
                }

                IsImageValid = true;

                StretchedHistogram = newHistogram;

                data = new NativeFitsImageData(imgHandle.data);
            }
            return true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (this)
            {
                IsImageDataValid = false;
                IsImageValid = false;

                if (!disposed)
                {
                    if (imgHandle.data.ToInt64() != 0)
                    {
                        loader.FreeImage(imgHandle);
                        imgHandle = default;
                    }

                    if (statisticsHandle.catalog.ToInt64() != 0)
                    {
                        loader.FreeStatistics(statisticsHandle);
                        statisticsHandle = default;
                    }

                    if (dataHandle.image_ptr.ToInt64() != 0)
                    {
                        loader.FreeImageData(dataHandle);
                        dataHandle = default;
                    }

                    loader.CloseFitFile(fitsHandle);
                    loader.FreeFit(fitsHandle);
                    fitsHandle = default;

                    disposed = true;
                }
            }
        }

        public IDisposable Ref()
        {
            return new Reference(this);
        }
    }
}

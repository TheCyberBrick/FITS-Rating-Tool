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

#include <string>
#include <vector>

#include "fitsloader.h"
#include "stretch.h"
#include "photometry.h"

struct FITSHandle
{
	bool valid;
	Loader::FITSImageDim inDim;
	Loader::FITSImageDim outDim;
	int header_records;
	int max_header_keyword_size;
	int max_header_value_size;
	int max_header_comment_size;
	Loader::FITSInfo* info;
};

struct FITSImageDataHandle
{
	bool valid;
	float* data_ptr;
	std::valarray<float>* image;
};

struct FITSImageHandle
{
	unsigned char* data_ptr;
};

struct FITSStatisticsHandle
{
	bool valid;

	Photometry::Catalog* catalog;
	int count;
	Photometry::Statistics statistics;
};

extern "C"
{
	__declspec(dllexport) FITSHandle LoadFit(const char* file, uint64_t max_input_size, uint32_t max_input_width, uint32_t max_input_height)
	{
		Loader::FITSInfo* fits = new Loader::FITSInfo(file, max_input_size, max_input_width, max_input_height);

		fits->ReadHeader();

		FITSHandle handle{};

		handle.info = nullptr;
		handle.valid = fits->valid();

		if (!handle.valid)
		{
			delete fits;
			return handle;
		}

		handle.inDim = fits->attributes().data.in_dim;
		handle.outDim = fits->attributes().data.out_dim;
		handle.header_records = static_cast<int>(fits->attributes().header.size());
		handle.max_header_keyword_size = FLEN_KEYWORD;
		handle.max_header_value_size = FLEN_VALUE;
		handle.max_header_comment_size = FLEN_COMMENT;
		handle.info = fits;

		return handle;
	}

	__declspec(dllexport) void CloseFitFile(FITSHandle handle)
	{
		if (handle.info != nullptr)
		{
			handle.info->CloseFile();
		}
	}

	__declspec(dllexport) bool ReadHeaderRecord(FITSHandle handle, int index, char* keyword, size_t nkeyword, char* value, size_t nvalue, char* comment, size_t ncomment)
	{
		if (index < 0 || index >= handle.info->attributes().header.size())
		{
			return false;
		}
		Loader::FITSHeaderRecord const& record = handle.info->attributes().header[index];
		strcpy_s(keyword, std::min(record.keyword.size() + 1, nkeyword), record.keyword.c_str());
		strcpy_s(value, std::min(record.value.size() + 1, nvalue), record.value.c_str());
		strcpy_s(comment, std::min(record.comment.size() + 1, ncomment), record.comment.c_str());
		return true;
	}

	__declspec(dllexport) void FreeFit(FITSHandle handle)
	{
		if (handle.info)
		{
			delete handle.info;
			handle.info = nullptr;
		}
	}

	__declspec(dllexport) bool ReadImage(FITSHandle handle, unsigned char* data, Loader::FITSImageLoaderParameters props)
	{
		return handle.info->ReadImage(data, props);
	}


	__declspec(dllexport) FITSImageDataHandle LoadImageData(FITSHandle handle, Loader::FITSImageLoaderParameters props, uint32_t* histogram, size_t histogram_size)
	{
		FITSImageDataHandle image_data_handle{ false, nullptr };
		if (handle.info != nullptr)
		{
			image_data_handle.data_ptr = nullptr;
			image_data_handle.image = new std::valarray<float>(handle.info->attributes().data.out_dim.n);

			image_data_handle.valid = handle.info->ReadImageUnprocessed<float>(*image_data_handle.image, props, histogram, histogram_size);

			if (!image_data_handle.valid || image_data_handle.image->size() == 0)
			{
				delete image_data_handle.image;
				image_data_handle.image = nullptr;
			}
			else
			{
				image_data_handle.data_ptr = &((*image_data_handle.image)[0]);
			}
		}
		return image_data_handle;
	}

	__declspec(dllexport) void FreeImageData(FITSImageDataHandle handle)
	{
		if (handle.image)
		{
			delete handle.image;
			handle.image = nullptr;
		}
	}

	__declspec(dllexport) FITSStatisticsHandle ComputeStatistics(FITSHandle fits_handle, FITSImageDataHandle data_handle, Photometry::Callback callback)
	{
		FITSStatisticsHandle handle{ false, nullptr, 0, { } };

		Photometry::Catalog* catalog;

		Photometry::Parameters params{};
		Photometry::Extractor extractor{ params };
		int status = 0;
		if (!extractor.Extract(*fits_handle.info, *data_handle.image, &catalog, &status, callback))
		{
			char err_msg[61];
			err_msg[0] = '\0';
			sep_get_errmsg(status, err_msg);
			delete catalog;
			return handle;
		}

		handle.valid = true;
		handle.catalog = catalog;
		handle.count = static_cast<int>(catalog->objects.size());
		handle.statistics = catalog->statistics;

		return handle;
	}

	__declspec(dllexport) bool GetPhotometry(FITSStatisticsHandle handle, int src_start, int src_n, int dst_start, Photometry::Object* photometry)
	{
		if (src_start < 0 || src_n < 0 || src_start + src_n > handle.catalog->objects.size())
		{
			return false;
		}
		for (int i = src_start; i < src_start + src_n; ++i)
		{
			photometry[dst_start + i - src_start] = handle.catalog->objects[i];
		}
		return true;
	}

	__declspec(dllexport) void FreeStatistics(FITSStatisticsHandle handle)
	{
		delete handle.catalog;
	}

	__declspec(dllexport) Processing::ImageStretchParameters ComputeStretch(FITSHandle fits_handle, FITSImageDataHandle data_handle)
	{
		Processing::ImageStretchParameters params;
		Processing::ComputeImageStretch<float>(*data_handle.image, fits_handle.info->attributes().data.out_dim, &params);
		return params;
	}

	__declspec(dllexport) FITSImageHandle ProcessImage(FITSHandle fits_handle, FITSImageDataHandle data_handle, FITSImageHandle image_handle, bool compute_stretch_params, Loader::FITSImageLoaderParameters props, uint32_t* histogram, size_t histogram_size)
	{
		if (image_handle.data_ptr == nullptr)
		{
			image_handle.data_ptr = new unsigned char[fits_handle.info->attributes().data.out_dim.nx * fits_handle.info->attributes().data.out_dim.ny * 4];
		}

		fits_handle.info->ProcessImage<float>(*data_handle.image, image_handle.data_ptr, compute_stretch_params, props, histogram, histogram_size);

		return image_handle;
	}

	__declspec(dllexport) void FreeImage(FITSImageHandle handle)
	{
		if (handle.data_ptr)
		{
			delete[] handle.data_ptr;
			handle.data_ptr = nullptr;
		}
	}

}

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

#include <stdio.h>
#include <cstdio>
#include <vector>
#include <numeric>
#include <limits>
#include <algorithm>
#include <cctype>

#include "fitsloader.h"
#include "fitsdataloader.h"
#include "hsv.h"

namespace Loader
{

	FITSInfo::FITSInfo(const char* file, size_t max_input_size, int max_input_width, int max_input_height) :
		m_file(file), m_fits_file(nullptr), max_input_size(max_input_size), max_input_width(max_input_width), max_input_height(max_input_height),
		m_attributes(), m_kernel_size(0), m_kernelStride(0), m_valid(false)
	{
		filter_map["l"] = FITSFilterType::L;
		filter_map["lum"] = FITSFilterType::L;
		filter_map["luminance"] = FITSFilterType::L;
		filter_map["r"] = FITSFilterType::R;
		filter_map["red"] = FITSFilterType::R;
		filter_map["g"] = FITSFilterType::G;
		filter_map["green"] = FITSFilterType::G;
		filter_map["b"] = FITSFilterType::B;
		filter_map["blue"] = FITSFilterType::B;

		cfa_map["rggb"] = {
			1.0f, 0.0f, 0.0f, 0.0f,
			0.0f, 0.5f, 0.5f, 0.0f,
			0.0f, 0.0f, 0.0f, 1.0f
		};
		cfa_map["bggr"] = {
			0.0f, 0.0f, 0.0f, 1.0f,
			0.0f, 0.5f, 0.5f, 0.0f,
			1.0f, 0.0f, 0.0f, 0.0f
		};
		cfa_map["gbrg"] = {
			0.0f, 0.0f, 1.0f, 0.0f,
			0.5f, 0.0f, 0.0f, 0.5f,
			0.0f, 1.0f, 0.0f, 0.0f
		};
		cfa_map["grbg"] = {
			0.0f, 1.0f, 0.0f, 0.0f,
			0.5f, 0.0f, 0.0f, 0.5f,
			0.0f, 0.0f, 1.0f, 0.0f
		};
	}

	FITSInfo::~FITSInfo()
	{
		CloseFile();
	}

	void FITSInfo::CloseFile()
	{
		if (m_fits_file != nullptr)
		{
			int status(0);
			fits_close_file(m_fits_file, &status);
			m_fits_file = nullptr;
		}
	}

	void FITSInfo::ReadHeader()
	{
		m_valid = false;

		int status;

		status = 0;
		if (fits_open_diskfile(&m_fits_file, m_file, READONLY, &status))
		{
			return;
		}

		int nhdu;
		if (fits_get_num_hdus(m_fits_file, &nhdu, &status))
		{
			return;
		}

		for (int hdu_num = 1; hdu_num <= nhdu; hdu_num++)
		{
			int hdu_type = 0;

			status = 0;
			if (fits_movabs_hdu(m_fits_file, hdu_num, &hdu_type, &status))
			{
				//EOF
				break;
			}

			if (hdu_type == IMAGE_HDU)
			{
				// ---- Read number of keys ----

				int num_keys;
				status = 0;
				if (fits_get_hdrspace(m_fits_file, &num_keys, NULL, &status))
				{
					break;
				}

				// ---- Read image type (negative = float/double) ----

				int img_type;
				status = 0;
				if (fits_get_img_type(m_fits_file, &img_type, &status))
				{
					break;
				}

				m_attributes.data.in_file_datatype = Loader::GetFITSDatatypeFromImgType(img_type);

				int img_equiv_type;
				status = 0;
				if (fits_get_img_equivtype(m_fits_file, &img_equiv_type, &status))
				{
					break;
				}

				m_attributes.data.in_memory_datatype = Loader::GetFITSDatatypeFromImgType(img_equiv_type);

				// ---- Read image dimensions ----

				int dim;
				status = 0;
				if (fits_get_img_dim(m_fits_file, &dim, &status))
				{
					break;
				}

				// ---- Read image size ----

				std::vector<long> size(dim);
				status = 0;
				if (fits_get_img_size(m_fits_file, dim, size.data(), &status) || size.size() < 2)
				{
					break;
				}

				// ---- Read bayer pattern ----

				std::string const bayer_pattern_keywords[] =
				{
					"BAYERPAT", "COLORTYP", "COLORTYPE"
				};

				for (auto& keyword : bayer_pattern_keywords)
				{
					if (ReadStringKeyword(keyword.c_str(), &m_attributes.instrument.bayer_pattern, &status) == 0)
					{
						break;
					}
				}

				std::transform(m_attributes.instrument.bayer_pattern.begin(), m_attributes.instrument.bayer_pattern.end(), m_attributes.instrument.bayer_pattern.begin(), std::tolower);

				if (cfa_map.find(m_attributes.instrument.bayer_pattern) != cfa_map.end())
				{
					m_attributes.instrument.cfa = FITSColorFilterArray(cfa_map[m_attributes.instrument.bayer_pattern]);
				}
				else
				{
					m_attributes.instrument.cfa = FITSColorFilterArray();
				}

				// ---- Read bayer pattern offset ----

				std::string const bayer_offset_x_keywords[] = {
					"XBAYROFF", "XBAYOFF", "BAYROFFX", "BAYOFFX"
				};

				for (auto& keyword : bayer_offset_x_keywords)
				{
					if (ReadIntKeyword(keyword.c_str(), &m_attributes.instrument.bayer_offset_x, &status) == 0)
					{
						break;
					}
				}

				std::string const bayer_offset_y_keywords[] = {
					"YBAYROFF", "YBAYOFF", "BAYROFFY", "BAYOFFY"
				};

				for (auto& keyword : bayer_offset_y_keywords)
				{
					if (ReadIntKeyword(keyword.c_str(), &m_attributes.instrument.bayer_offset_y, &status) == 0)
					{
						break;
					}
				}

				// ---- Store image dim ----

				m_attributes.data.in_dim.nx = static_cast<int>(size[0]);
				m_attributes.data.in_dim.ny = static_cast<int>(size[1]);
				m_attributes.data.in_dim.nc = size.size() == 3 ? 3 : 1;
				m_attributes.data.in_dim.n = static_cast<uint32_t>(m_attributes.data.in_dim.nx) * static_cast<uint32_t>(m_attributes.data.in_dim.ny) * static_cast<uint32_t>(m_attributes.data.in_dim.nc);

				int width = m_attributes.data.in_dim.nx;
				int height = m_attributes.data.in_dim.ny;

				if (m_attributes.data.in_dim.nc == 1 && !m_attributes.instrument.cfa.identity())
				{
					// bayered data, output will be half size
					width /= 2;
					height /= 2;
				}

				float downsample_x = static_cast<float>(width) / static_cast<float>(max_input_width);
				float downsample_y = static_cast<float>(height) / static_cast<float>(max_input_height);

				int constrained_dim = -1;

				// determine smallest kernel size that s.t. no pixels
				// are ignored when downsampling
				m_kernel_size = 0;
				if (downsample_x > downsample_y && downsample_x > 1)
				{
					m_kernel_size = static_cast<float>(m_attributes.data.in_dim.nx - max_input_width) / static_cast<float>(2 * (max_input_width + 1));
					constrained_dim = 0;
				}
				else if (downsample_y > downsample_x && downsample_y > 1)
				{
					m_kernel_size = static_cast<float>(m_attributes.data.in_dim.ny - max_input_height) / static_cast<float>(2 * (max_input_height + 1));
					constrained_dim = 1;
				}
				else
				{
					m_kernel_size = 0;
				}

				// determine smallest kernel stride s.t. the output
				// image fits within the min and max height
				m_kernelStride = 1 + 2 * static_cast<int>(ceil(m_kernel_size));
				if (m_kernel_size > 0)
				{
					if (constrained_dim == 0)
					{
						m_kernelStride = static_cast<int>(ceil((width - 2 * ceil(m_kernel_size)) / static_cast<float>(max_input_width)));
					}
					else if (constrained_dim == 1)
					{
						m_kernelStride = static_cast<int>(ceil((height - 2 * ceil(m_kernel_size)) / static_cast<float>(max_input_height)));
					}
				}

				int out_width = (width - 2 * static_cast<int>(ceil(m_kernel_size))) / m_kernelStride;
				int out_height = (height - 2 * static_cast<int>(ceil(m_kernel_size))) / m_kernelStride;

				m_attributes.data.out_dim.nx = out_width;
				m_attributes.data.out_dim.ny = out_height;
				if (m_attributes.data.in_dim.nc == 1 && m_attributes.instrument.cfa.identity())
				{
					m_attributes.data.out_dim.nc = 1;
				}
				else
				{
					m_attributes.data.out_dim.nc = 3;
				}
				m_attributes.data.out_dim.n = m_attributes.data.out_dim.nx * m_attributes.data.out_dim.ny * m_attributes.data.out_dim.nc;

				// ---- Read filter ----

				ReadStringKeyword("FILTER", &m_attributes.shot.filter, &status);
				if (m_attributes.shot.filter.length() == 0)
				{
					int filter_count = -1;

					std::string const filter_number_keywords[] = {
						"FILTNUM", "FILTNUMBER", "FILTNR", "FILTN",
						"FILTERNUM", "FILTERNUMBER", "FILTERNR", "FILTERN"
					};

					bool has_filter_number_keywrod = false;
					for (auto& keyword : filter_number_keywords)
					{
						has_filter_number_keywrod |= ReadIntKeyword(keyword.c_str(), &filter_count, &status) == 0;
					}

					if (!has_filter_number_keywrod)
					{
						// No filter count specified, assume 1
						filter_count = 1;
					}

					if (filter_count == 1)
					{
						std::string filter = "";

						std::string const filter_keywords[] = {
							"FILT0", "FILT-0", "FILTER0", "FILTER-0",
							"FILT1", "FILT-1", "FILTER1", "FILTER-1",
							"FILT2", "FILT-2", "FILTER2", "FILTER-2",
							"FILTER", "FILT"
						};

						for (auto& keyword : filter_keywords)
						{
							if (ReadStringKeyword(keyword.c_str(), &filter, &status) == 0)
							{
								m_attributes.shot.filter = filter;
								break;
							}
						}
					}
				}

				std::transform(m_attributes.shot.filter.begin(), m_attributes.shot.filter.end(), m_attributes.shot.filter.begin(), std::tolower);

				if (filter_map.find(m_attributes.shot.filter) != filter_map.end())
				{
					m_attributes.shot.filter_type = filter_map[m_attributes.shot.filter];
				}

				// ---- Read exposure time ----

				if (ReadFloatKeyword("EXPOSURE", &m_attributes.shot.exposure, &status) != 0)
				{
					if (ReadFloatKeyword("EXPTIME", &m_attributes.shot.exposure, &status) != 0)
					{
						ReadFloatKeyword("EXP", &m_attributes.shot.exposure, &status);
					}
				}

				// ---- Read focal length ----

				if (ReadFloatKeyword("FOCALLEN", &m_attributes.instrument.focal_length, &status) != 0)
				{
					ReadFloatKeyword("FOCALLENGTH", &m_attributes.instrument.focal_length, &status);
				}

				// ---- Read gain ----

				ReadFloatKeyword("GAIN", &m_attributes.shot.gain, &status);

				// ---- Read aperture ----

				ReadFloatKeyword("APERTURE", &m_attributes.instrument.aperture, &status);

				// ---- Read RA & DEC ----

				ReadStringKeyword("OBJCTDEC", &m_attributes.object.object_dec, &status);
				ReadStringKeyword("OBJCTRA", &m_attributes.object.object_ra, &status);

				// ---- Read date ----

				if (ReadDateKeyword("DATE-OBS", &m_attributes.shot.date, &status) != 0)
				{
					ReadDateKeyword("DATE", &m_attributes.shot.date, &status);
				}

				// ---- Read entire header into map ----

				m_attributes.header.clear();

				char keyword[FLEN_KEYWORD];
				char value[FLEN_VALUE];
				char comment[FLEN_COMMENT];

				for (int key = 1; key <= num_keys; key++)
				{
					status = 0;

					if (fits_read_keyn(m_fits_file, key, keyword, value, comment, &status))
					{
						continue;
					}

					m_attributes.header.push_back({ std::string(keyword), std::string(value), std::string(comment) });
				}

				m_valid = true;

				break;
			}
		}
	}

	bool FITSInfo::ReadImage(unsigned char* data, FITSImageLoaderParameters props)
	{
		Loader::FITSDatatype in_memory_datatype = m_attributes.data.in_memory_datatype;

		if (!m_valid || (static_cast<size_t>(m_attributes.data.in_dim.n) * in_memory_datatype.size > max_input_size))
		{
			return false;
		}

		if (in_memory_datatype.fits_datatype == TFLOAT || in_memory_datatype.fits_datatype == TDOUBLE)
		{
			return ReadImage<float, float>(in_memory_datatype.fits_datatype, false, data, props);
		}
		else if (in_memory_datatype.size == 1)
		{
			if (in_memory_datatype.is_signed)
			{
				return ReadImage<int8_t, uint8_t>(in_memory_datatype.fits_datatype, true, data, props);
			}
			else
			{
				return ReadImage<uint8_t, uint8_t>(in_memory_datatype.fits_datatype, false, data, props);
			}
		}
		else if (in_memory_datatype.size == 2)
		{
			if (in_memory_datatype.is_signed)
			{
				return ReadImage<int16_t, uint16_t>(in_memory_datatype.fits_datatype, true, data, props);
			}
			else
			{
				return ReadImage<uint16_t, uint16_t>(in_memory_datatype.fits_datatype, false, data, props);
			}
		}
		else if (in_memory_datatype.size >= 4)
		{
			if (in_memory_datatype.is_signed)
			{
				return ReadImage<int32_t, uint32_t>(in_memory_datatype.fits_datatype, true, data, props);
			}
			else
			{
				return ReadImage<uint32_t, uint32_t>(in_memory_datatype.fits_datatype, false, data, props);
			}
		}
		else
		{
			return ReadImage<float, float>(in_memory_datatype.fits_datatype, false, data, props);
		}
	}


	template<typename T_OUT>
	bool FITSInfo::ReadImageUnprocessed(std::valarray<T_OUT>& data, FITSImageLoaderParameters props, uint32_t* histogram, size_t histogram_size)
	{
		Loader::FITSDatatype in_memory_datatype = m_attributes.data.in_memory_datatype;

		if (!m_valid || (static_cast<size_t>(m_attributes.data.in_dim.n) * in_memory_datatype.size > max_input_size))
		{
			return false;
		}

		bool is_input_float_type = false;
		T_OUT min_value = 0;

		bool success;

		if (in_memory_datatype.fits_datatype == TFLOAT || in_memory_datatype.fits_datatype == TDOUBLE)
		{
			success = ReadImage<float, T_OUT>(in_memory_datatype.fits_datatype, false, data, props);
			is_input_float_type = true;
		}
		else if (in_memory_datatype.size == 1)
		{
			if (in_memory_datatype.is_signed)
			{
				success = ReadImage<int8_t, T_OUT>(in_memory_datatype.fits_datatype, true, data, props);
				min_value = static_cast<T_OUT>(std::numeric_limits<int8_t>::min());
			}
			else
			{
				success = ReadImage<uint8_t, T_OUT>(in_memory_datatype.fits_datatype, false, data, props);
				min_value = static_cast<T_OUT>(std::numeric_limits<uint8_t>::min());
			}
		}
		else if (in_memory_datatype.size == 2)
		{
			if (in_memory_datatype.is_signed)
			{
				success = ReadImage<int16_t, T_OUT>(in_memory_datatype.fits_datatype, true, data, props);
				min_value = static_cast<T_OUT>(std::numeric_limits<int16_t>::min());
			}
			else
			{
				success = ReadImage<uint16_t, T_OUT>(in_memory_datatype.fits_datatype, false, data, props);
				min_value = static_cast<T_OUT>(std::numeric_limits<uint16_t>::min());
			}
		}
		else if (in_memory_datatype.size >= 4)
		{
			if (in_memory_datatype.is_signed)
			{
				success = ReadImage<int32_t, T_OUT>(in_memory_datatype.fits_datatype, true, data, props);
				min_value = static_cast<T_OUT>(std::numeric_limits<int32_t>::min());
			}
			else
			{
				success = ReadImage<uint32_t, T_OUT>(in_memory_datatype.fits_datatype, false, data, props);
				min_value = static_cast<T_OUT>(std::numeric_limits<uint32_t>::min());
			}
		}
		else
		{
			success = ReadImage<float, T_OUT>(in_memory_datatype.fits_datatype, false, data, props);
			is_input_float_type = true;
		}

		if (success && histogram != nullptr)
		{
			T_OUT max_value;
			if (is_input_float_type)
			{
				min_value = data.min();
				max_value = data.max();
			}
			else
			{
				max_value = min_value + static_cast<T_OUT>(in_memory_datatype.size >= 4 ? (uint32_t(1) << 31) : (1 << (8 * in_memory_datatype.size)));
			}

			T_OUT range = max_value - min_value;

			for (int i = 0; i < data.size(); ++i)
			{
				size_t bucket = static_cast<size_t>(static_cast<double>(data[i] - min_value) * (histogram_size - 1) / range);
				++histogram[std::min(std::max(bucket, size_t(0)), histogram_size - 1)];
			}
		}

		return success;
	}

	template bool FITSInfo::ReadImageUnprocessed(std::valarray<uint64_t>& data, FITSImageLoaderParameters props, uint32_t* histogram, size_t histogram_size);
	template bool FITSInfo::ReadImageUnprocessed(std::valarray<uint32_t>& data, FITSImageLoaderParameters props, uint32_t* histogram, size_t histogram_size);
	template bool FITSInfo::ReadImageUnprocessed(std::valarray<uint16_t>& data, FITSImageLoaderParameters props, uint32_t* histogram, size_t histogram_size);
	template bool FITSInfo::ReadImageUnprocessed(std::valarray<uint8_t>& data, FITSImageLoaderParameters props, uint32_t* histogram, size_t histogram_size);
	template bool FITSInfo::ReadImageUnprocessed(std::valarray<float>& data, FITSImageLoaderParameters props, uint32_t* histogram, size_t histogram_size);

	template<typename T>
	T GetSignedToUnsignedConversionOffset(std::true_type)
	{
		return (T)-std::numeric_limits<std::make_signed_t<T>>::min();
	}
	template<typename T>
	T GetSignedToUnsignedConversionOffset(std::false_type)
	{
		return 0;
	}
	template<typename T>
	T GetSignedToUnsignedConversionOffset()
	{
		return GetSignedToUnsignedConversionOffset<T>(std::integral_constant<bool, std::is_integral<T>::value>{});
	}

	template<typename T_IN, typename T_OUT>
	bool FITSInfo::ReadImage(int fits_datatype, bool issigned, unsigned char* data, FITSImageLoaderParameters props)
	{
		std::valarray<T_OUT> imgData(m_attributes.data.out_dim.n);

		if (!ReadImage<T_IN, T_OUT>(fits_datatype, issigned, imgData, props))
		{
			return false;
		}

		ProcessImage<T_OUT>(imgData, nullptr, 0);

		StoreImageBGRA32<T_OUT>(imgData, data, props);

		return true;
	}

	template<typename T_IN, typename T_OUT>
	bool FITSInfo::ReadImage(int fits_datatype, bool issigned, std::valarray<T_OUT>& data, FITSImageLoaderParameters props)
	{
		if (m_fits_file == nullptr)
		{
			return false;
		}

		long fpixel(1);
		T_OUT nulval(0);
		int anynul(0);

		int full_kernel_size = static_cast<int>(ceil(m_kernel_size));

		int const kernel_dim = (1 + full_kernel_size * 2);

		std::valarray<float> weights(kernel_dim * kernel_dim);
		if (weights.size() == 1)
		{
			weights[0] = 1.0f;
		}
		else
		{
			// gaussian kernel stddev
			float sigma = (m_kernelStride - 1) / 6.0f;

			float s = 2.0f * sigma * sigma;
			float sum = 0.0f;

			// generate kernel gaussian
			for (int x = -full_kernel_size; x <= full_kernel_size; x++) {
				for (int y = -full_kernel_size; y <= full_kernel_size; y++) {
					float r = sqrt(static_cast<float>(x * x + y * y));
					float value = exp(-(r * r) / s) / (3.1416f * s);

					weights[x + full_kernel_size + kernel_dim * (y + full_kernel_size)] = value;

					sum += value;
				}
			}

			// normalize kernel
			for (int i = 0; i < kernel_dim; ++i) {
				for (int j = 0; j < kernel_dim; ++j) {
					weights[i + kernel_dim * j] /= sum;
				}
			}
		}

		std::array<float, 12> cfa;
		if (!m_attributes.instrument.cfa.identity())
		{
			cfa = m_attributes.instrument.cfa.values();

			if (abs(m_attributes.instrument.bayer_offset_x) % 2 != 0)
			{
				// flip cfa along X axis
				for (int i = 0; i < 3; i++)
				{
					std::swap(cfa[0 + i * 4], cfa[1 + i * 4]);
					std::swap(cfa[2 + i * 4], cfa[3 + i * 4]);
				}
			}

			if (abs(m_attributes.instrument.bayer_offset_y) % 2 != 0)
			{
				// flip cfa along Y axis
				for (int i = 0; i < 3; i++)
				{
					std::swap(cfa[0 + i * 4], cfa[2 + i * 4]);
					std::swap(cfa[1 + i * 4], cfa[3 + i * 4]);
				}
			}
		}

		// whether the data contains negative values
		bool negative = false;

		Loader::DataOutput<T_IN, T_OUT> output{ &negative, &data[0] };
		Loader::DataKernel<T_IN, T_OUT> kernel;

		kernel.size = full_kernel_size;
		kernel.stride = m_kernelStride;
		kernel.weights = &weights[0];
		kernel.offset = issigned ? static_cast<T_OUT>(GetSignedToUnsignedConversionOffset<T_IN>()) : 0;
		kernel.cfa = !m_attributes.instrument.cfa.identity() ? cfa.data() : nullptr;
		kernel.rgb = m_attributes.data.out_dim.nc == 3;

		int status = 0;
		if (Loader::ReadData(m_fits_file, fits_datatype, m_attributes.data.in_dim.nx, m_attributes.data.in_dim.ny, kernel, output, &status) != 0)
		{
			return false;
		}

		// if the storage type is signed but no
		// negative values have been found then
		// it's likely that the data was originally
		// unsigned and so the offset is removed again
		if (issigned && !negative && kernel.offset != 0)
		{
			data -= kernel.offset;
		}

		return true;
	};


	template<typename T_OUT>
	void FITSInfo::ProcessImage(std::valarray<T_OUT>& data, unsigned char* outData, bool computeStrechParams, FITSImageLoaderParameters props, uint32_t* histogram, size_t histogram_size)
	{
		std::valarray<T_OUT> data_copy(data);
		if (computeStrechParams)
		{
			ProcessImage<T_OUT>(data_copy, histogram, histogram_size);
		}
		else
		{
			Processing::StretchImage(data_copy, m_attributes.data.out_dim, props.stretch_params, histogram, nullptr, nullptr, histogram_size);
		}
		StoreImageBGRA32<T_OUT>(data_copy, outData, props);
	}

	template void FITSInfo::ProcessImage(std::valarray<uint64_t>& data, unsigned char* outData, bool computeStrechParams, FITSImageLoaderParameters props, uint32_t* histogram, size_t histogram_size);
	template void FITSInfo::ProcessImage(std::valarray<uint32_t>& data, unsigned char* outData, bool computeStrechParams, FITSImageLoaderParameters props, uint32_t* histogram, size_t histogram_size);
	template void FITSInfo::ProcessImage(std::valarray<uint16_t>& data, unsigned char* outData, bool computeStrechParams, FITSImageLoaderParameters props, uint32_t* histogram, size_t histogram_size);
	template void FITSInfo::ProcessImage(std::valarray<uint8_t>& data, unsigned char* outData, bool computeStrechParams, FITSImageLoaderParameters props, uint32_t* histogram, size_t histogram_size);
	template void FITSInfo::ProcessImage(std::valarray<float>& data, unsigned char* outData, bool computeStrechParams, FITSImageLoaderParameters props, uint32_t* histogram, size_t histogram_size);

	template<typename T>
	void FITSInfo::ProcessImage(std::valarray<T>& data, uint32_t* histogram, size_t histogram_size)
	{
		Processing::ImageStretchParameters params;
		Processing::ComputeImageStretch(data, m_attributes.data.out_dim, &params);
		Processing::StretchImage(data, m_attributes.data.out_dim, params, histogram, nullptr, nullptr, histogram_size);
	}

	template<typename T>
	void FITSInfo::StoreImageBGRA32(std::valarray<T>& inData, unsigned char* outData, FITSImageLoaderParameters props)
	{
		auto& size = m_attributes.data.out_dim;
		if (size.nc == 1)
		{
			if (props.mono_color_outline && m_attributes.shot.filter_type != FITSFilterType::Other)
			{
				int const border = std::max(2, std::min(size.nx / 50, size.ny / 50));

				switch (m_attributes.shot.filter_type)
				{
				case FITSFilterType::L:
					for (int y = 0; y < size.ny; y++)
					{
						for (int x = 0; x < size.nx; x++)
						{
							int i = (y * size.nx + x);

							unsigned char v = static_cast<unsigned char>(inData[i]);
							if (x <= border || x >= size.nx - 1 - border || y <= border || y >= size.ny - 1 - border)
							{
								outData[i * 4 + 0] = 200;
								outData[i * 4 + 1] = 200;
								outData[i * 4 + 2] = 200;
							}
							else
							{
								outData[i * 4 + 0] = v;
								outData[i * 4 + 1] = v;
								outData[i * 4 + 2] = v;
							}
							outData[i * 4 + 3] = 255;
						}
					}
					break;
				case FITSFilterType::R:
					for (int y = 0; y < size.ny; y++)
					{
						for (int x = 0; x < size.nx; x++)
						{
							int i = (y * size.nx + x);

							unsigned char v = static_cast<unsigned char>(inData[i]);
							if (x <= border || x >= size.nx - 1 - border || y <= border || y >= size.ny - 1 - border)
							{
								outData[i * 4 + 0] = static_cast<unsigned char>(v * 0.2f);
								outData[i * 4 + 1] = static_cast<unsigned char>(v * 0.2f);
								outData[i * 4 + 2] = 200;
							}
							else
							{
								outData[i * 4 + 0] = v;
								outData[i * 4 + 1] = v;
								outData[i * 4 + 2] = v;
							}
							outData[i * 4 + 3] = 255;
						}
					}
					break;
				case FITSFilterType::G:
					for (int y = 0; y < size.ny; y++)
					{
						for (int x = 0; x < size.nx; x++)
						{
							int i = (y * size.nx + x);

							unsigned char v = static_cast<unsigned char>(inData[i]);
							if (x <= border || x >= size.nx - 1 - border || y <= border || y >= size.ny - 1 - border)
							{
								outData[i * 4 + 0] = static_cast<unsigned char>(v * 0.2f);
								outData[i * 4 + 1] = 200;
								outData[i * 4 + 2] = static_cast<unsigned char>(v * 0.2f);
							}
							else
							{
								outData[i * 4 + 0] = v;
								outData[i * 4 + 1] = v;
								outData[i * 4 + 2] = v;
							}
							outData[i * 4 + 3] = 255;
						}
					}
					break;
				case FITSFilterType::B:
					for (int y = 0; y < size.ny; y++)
					{
						for (int x = 0; x < size.nx; x++)
						{
							int i = (y * size.nx + x);

							unsigned char v = static_cast<unsigned char>(inData[i]);
							if (x <= border || x >= size.nx - border || y <= border || y >= size.ny - border)
							{
								outData[i * 4 + 0] = 200;
								outData[i * 4 + 1] = static_cast<unsigned char>(v * 0.2f);
								outData[i * 4 + 2] = static_cast<unsigned char>(v * 0.2f);
							}
							else
							{
								outData[i * 4 + 0] = v;
								outData[i * 4 + 1] = v;
								outData[i * 4 + 2] = v;
							}
							outData[i * 4 + 3] = 255;
						}
					}
					break;
				}
			}
			else
			{
				for (int i = 0; i < size.ny * size.nx; i++)
				{
					unsigned char v = static_cast<unsigned char>(inData[i]);
					outData[i * 4 + 0] = v;
					outData[i * 4 + 1] = v;
					outData[i * 4 + 2] = v;
					outData[i * 4 + 3] = 255;
				}
			}
		}
		else
		{
			int stride = size.nx * size.ny;

			for (int y = 0; y < size.ny; y++)
			{
				for (int x = 0; x < size.nx; x++)
				{
					int in_index = y * size.nx + x;
					int out_index = in_index * 4;

					Processing::RgbColor rgb{
						static_cast<unsigned char>(inData[in_index]),
						static_cast<unsigned char>(inData[in_index + stride]),
						static_cast<unsigned char>(inData[in_index + stride * 2])
					};

					Processing::HsvColor hsv = Processing::RgbToHsv(rgb);

					hsv.s = std::min(static_cast<unsigned char>(props.saturation * hsv.s), static_cast <unsigned char>(255));

					rgb = Processing::HsvToRgb(hsv);

					outData[out_index + 2] = rgb.r;
					outData[out_index + 1] = rgb.g;
					outData[out_index + 0] = rgb.b;
					outData[out_index + 3] = 255;
				}
			}
		}
	}

	int FITSInfo::ReadStringKeyword(const char* key, std::string* str, int* status)
	{
		*str = "";
		char buffer[FLEN_VALUE]{ '\0' };
		*status = 0;
		fits_read_key(m_fits_file, TSTRING, key, buffer, NULL, status);
		if (strlen(buffer) > 0)
		{
			*str = std::string(buffer);
		}
		return *status;
	}

	int FITSInfo::ReadIntKeyword(const char* key, int* value, int* status)
	{
		*value = 0;
		*status = 0;
		fits_read_key(m_fits_file, TINT, key, value, NULL, status);
		return *status;
	}

	int FITSInfo::ReadFloatKeyword(const char* key, float* value, int* status)
	{
		*value = 0;
		*status = 0;
		fits_read_key(m_fits_file, TFLOAT, key, value, NULL, status);
		return *status;
	}

	int FITSInfo::ReadDateKeyword(const char* key, FITSDate* value, int* status)
	{
		value->year = 0;
		value->month = 0;
		value->day = 0;
		value->hour = 0;
		value->minute = 0;
		value->second = 0;
		value->hasTime = false;
		std::string date_str;
		if (ReadStringKeyword(key, &date_str, status) == 0)
		{
			int year = 0, month = 0, day = 0, hour = 0, minute = 0;
			double second = 0;
			if (fits_str2time(const_cast<char*>(date_str.c_str()), &year, &month, &day, &hour, &minute, &second, status) == 0)
			{
				value->year = year;
				value->month = month;
				value->day = day;
				value->hour = hour;
				value->minute = minute;
				value->second = second;
				value->hasTime = true;
			}
			else if (fits_str2date(const_cast<char*>(date_str.c_str()), &year, &month, &day, status) == 0)
			{
				value->year = year;
				value->month = month;
				value->day = day;
				value->hasTime = false;
			}
		}
		return *status;
	}
}
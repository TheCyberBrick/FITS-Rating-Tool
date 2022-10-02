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

#pragma once

#include <valarray>
#include <string>
#include <iostream>
#include <map>
#include <array>

#include "fitsio.h"
#include "fitsdatatype.h"
#include "fitsattributes.h"
#include "stretch.h"

namespace Loader
{
	struct FITSImageLoaderParameters
	{
		bool mono_color_outline;
		float saturation;
		Processing::ImageStretchParameters stretch_params;
	};

	class FITSInfo
	{
	public:
		FITSInfo(std::string& file, size_t max_input_size, int max_input_width, int max_input_height);
		~FITSInfo();

		bool OpenFile();

		void CloseFile();

		int const max_input_width;
		int const max_input_height;
		size_t const max_input_size;

		bool valid() { return m_valid; };

		std::map<std::string, FITSFilterType> filter_map;
		std::map<std::string, std::array<float, 12>> cfa_map;

		FITSStandardAttributes const& attributes() const { return m_attributes; }

		bool const debayer() const { return m_debayer; }

		void ReadHeader();

		bool ReadImage(unsigned char* data, FITSImageLoaderParameters props);

		template<typename T_OUT>
		bool ReadImageUnprocessed(std::valarray<T_OUT>& data, FITSImageLoaderParameters props, uint32_t* histogram, size_t histogram_size);

		template<typename T_OUT>
		void ProcessImage(std::valarray<T_OUT>& data, unsigned char* outData, bool computeStrechParams, FITSImageLoaderParameters props, uint32_t* histogram, size_t histogram_size);
	private:
		std::string m_file;
		fitsfile* m_fits_file;

		bool m_valid;

		FITSStandardAttributes m_attributes;

		bool m_debayer;
		float m_kernel_size;
		int m_kernel_stride;

		int ReadStringKeyword(const char* key, std::string* str, int* status);
		int ReadIntKeyword(const char* key, int* value, int* status);
		int ReadFloatKeyword(const char* key, float* value, int* status);
		int ReadDateKeyword(const char* key, FITSDate* value, int* status);

		template<typename T_IN, typename T_OUT>
		bool ReadImage(int fits_datatype, bool issigned, std::valarray<T_OUT>& data, FITSImageLoaderParameters props);

		template<typename T_IN, typename T_OUT>
		bool ReadImage(int fits_datatype, bool issigned, unsigned char* data, FITSImageLoaderParameters props);

		template<typename T>
		void ProcessImage(std::valarray<T>& data, uint32_t* histogram, size_t histogram_size);

		template<typename T>
		void StoreImageBGRA32(std::valarray<T>& inData, unsigned char* outData, FITSImageLoaderParameters props);
	};
}

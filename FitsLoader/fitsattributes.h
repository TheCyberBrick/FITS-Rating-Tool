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

#include "fitsdatatype.h"

namespace Loader
{

	struct FITSImageDim
	{
		int nx = 0;
		int ny = 0;
		int nc = 0;
		uint32_t n = 0;
	};

	enum class FITSFilterType
	{
		L = 0,
		R = 1,
		G = 2,
		B = 3,
		Other = 4
	};

	struct FITSColorFilterArray
	{
		FITSColorFilterArray(std::array<float, 12> values) : m_values(values), m_identity(false)
		{
		}

		FITSColorFilterArray() : m_values(), m_identity(true)
		{
		}

		FITSColorFilterArray& operator=(FITSColorFilterArray const& other)
		{
			m_identity = other.m_identity;
			m_values = other.m_values;
			return *this;
		}

		bool const identity() const { return m_identity; }

		std::array<float, 12> const& values() const { return m_values; }

	private:
		bool m_identity;
		std::array<float, 12> m_values;
	};

	struct FITSDate
	{
		uint16_t year;
		uint8_t month;
		uint16_t day;
		uint8_t hour;
		uint8_t minute;
		double second;
		bool hasTime;
	};

	struct FITSHeaderRecord
	{
		std::string keyword;
		std::string value;
		std::string comment;
	};

	struct FITSInstrumentAttributes
	{
		float focal_length;
		float aperture;

		std::string bayer_pattern;
		int bayer_offset_x;
		int bayer_offset_y;

		FITSColorFilterArray cfa;
	};

	struct FITSObjectAttributes
	{
		std::string object_dec;
		std::string object_ra;
	};

	struct FITSShotAttributes
	{
		float gain;
		float exposure;

		std::string filter;
		FITSFilterType filter_type;

		FITSDate date;
	};

	struct FITSDataAttributes
	{
		FITSImageDim in_dim;
		FITSImageDim out_dim;

		Loader::FITSDatatype in_file_datatype;
		Loader::FITSDatatype in_memory_datatype;
	};

	struct FITSStandardAttributes
	{
		FITSShotAttributes shot;

		FITSInstrumentAttributes instrument;

		FITSObjectAttributes object;

		FITSDataAttributes data;

		std::vector<FITSHeaderRecord> header;
	};

}
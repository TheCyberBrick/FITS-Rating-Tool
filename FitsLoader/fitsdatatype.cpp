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

#include <map>
#include <tuple>

#include "fitsdatatype.h"
#include "fitsio.h"

namespace Loader
{
	FITSDatatype GetFITSDatatypeFromImgType(int fitsImgType)
	{
		static std::map<size_t, std::pair<int, int>> fitsIntTypes = {
			{ sizeof(char), std::make_pair(TSBYTE, TBYTE) },
			{ sizeof(short), std::make_pair(TSHORT, TUSHORT) },
			{ sizeof(int), std::make_pair(TINT, TUINT) },
			{ sizeof(long), std::make_pair(TLONG, TULONG) },
			{ sizeof(long long), std::make_pair(TLONGLONG, TULONGLONG) }
		};

		auto findFitsIntType = [](size_t size, bool issigned, int& fitsDatatype, size_t& typesize)
		{
			fitsDatatype = issigned ? TLONGLONG : TULONGLONG;
			typesize = sizeof(long long);
			for (auto& entry : fitsIntTypes)
			{
				if (entry.first >= size && entry.first <= typesize)
				{
					fitsDatatype = issigned ? entry.second.first : entry.second.second;
					typesize = entry.first;
				}
			}
		};

		size_t typesize;
		int fitsDatatype;
		switch (fitsImgType)
		{
		case SBYTE_IMG:
			findFitsIntType(1, true, fitsDatatype, typesize);
			return { fitsImgType, fitsDatatype, 8, typesize, true };
		case BYTE_IMG:
			findFitsIntType(1, false, fitsDatatype, typesize);
			return { fitsImgType, fitsDatatype, 8, typesize, false };
		case SHORT_IMG:
			findFitsIntType(2, true, fitsDatatype, typesize);
			return { fitsImgType, fitsDatatype, 16, typesize, true };
		case USHORT_IMG:
			findFitsIntType(2, false, fitsDatatype, typesize);
			return { fitsImgType, fitsDatatype, 16, typesize, false };
		case LONG_IMG:
			findFitsIntType(4, true, fitsDatatype, typesize);
			return { fitsImgType, fitsDatatype, 32, typesize, true };
		case ULONG_IMG:
			findFitsIntType(4, false, fitsDatatype, typesize);
			return { fitsImgType, fitsDatatype, 32, typesize, false };
		case LONGLONG_IMG:
			findFitsIntType(8, true, fitsDatatype, typesize);
			return { fitsImgType, fitsDatatype, 64, typesize, true };
		case ULONGLONG_IMG:
			findFitsIntType(8, false, fitsDatatype, typesize);
			return { fitsImgType, fitsDatatype, 64, typesize, false };
		case FLOAT_IMG:
			return { fitsImgType, TFLOAT, 32, sizeof(float), false };
		case DOUBLE_IMG:
			return { fitsImgType, TDOUBLE, 64, sizeof(double), false };
		default:
			return { FLOAT_IMG, TFLOAT, 32, sizeof(float), false };
		}
	}
}
